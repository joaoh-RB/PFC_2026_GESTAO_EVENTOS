using API_Gestao_Eventos.src.Application.DTO.Auth;
using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Infrastructure.Services.GoogleCalendar;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace API_Gestao_Eventos.Tests.Services
{
    public class EventServiceTests
    {
        private readonly EventService _eventService;
        private readonly Mock<IEventRepository> _eventRepositoryMock;
        private readonly Mock<ICourseRepository> _courseRepositoryMock;
        private readonly Mock<IGoogleCalendarService> _googleCalendarServiceMock;
        private readonly Mock<IAuthService> _authServiceMock;
        public EventServiceTests()
        {
            _eventRepositoryMock = new Mock<IEventRepository>();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _googleCalendarServiceMock = new Mock<IGoogleCalendarService>();
            _authServiceMock = new Mock<IAuthService>();
            _eventService = new EventService(_eventRepositoryMock.Object, _courseRepositoryMock.Object, _googleCalendarServiceMock.Object, _authServiceMock.Object);
        }
        //Cenário 7: Caminho feliz com verificação de interação mockada (Times.Once)
        [Fact]
        public async Task CreateEventAsync_QuandoDadosForemValidos_DeveCadastrarEventoComSucessoEChamarAddAsync()
        {
            var userId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var course = new Course()
            {
                Id = courseId,
                Name = "Curso de Teste"
            };
            var dto = new CreateEventRequestDto
            {
                Name = "Evento de Teste",
                Description = "Descrição do evento de teste",
                AllowDocuments = true,
                AllowedCourses = new List<Guid> { courseId },
                Capacity = 100,
                EventType = src.Domain.Enums.EventType.Palestra,
                InstitutionId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2)
            };
            _authServiceMock
                .Setup(a => a.GetUserInfoAsync(userId))
                .ReturnsAsync(new UserResponseDto
                {
                    Id = userId,
                    Role = "Professor",
                    Email = "professor@teste.com"
                });
            _courseRepositoryMock
                .Setup(r => r.GetByInstitutionAsync(dto.InstitutionId))
                .ReturnsAsync(new List<Course> { course });
            _googleCalendarServiceMock
                .Setup(g => g.CreateEventAsync(It.IsAny<Event>()))
                .ReturnsAsync("google-event-id");

            var savedEvent = new Event { Id = Guid.NewGuid() };
            _eventRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Event>()))
                .ReturnsAsync(savedEvent);
            // Act
            var result = await _eventService.AddAsync(dto, userId);
            // Assert

            Assert.Equal(savedEvent, result);
            _eventRepositoryMock.Verify(r =>
                r.AddAsync(It.Is<Event>(e =>
                    e.Name == dto.Name &&
                    e.Description == dto.Description &&
                    e.InstitutionId == dto.InstitutionId &&
                    e.GoogleCalendarEventId == "google-event-id" &&
                    e.AllowedCourses.Count == 1 &&
                    e.AllowedCourses.First().Id == courseId
                )),
                Times.Once);
        }
        [Fact]
        public async Task CreateEventAsync_QuandoCursoNaoPertencerAInstituicao_DeveLancarInvalidOperationExceptionENaoChamarAddAsync()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var dto = new CreateEventRequestDto
            {
                Name = "Evento de Teste",
                Description = "Descrição do evento de teste",
                AllowDocuments = true,
                AllowedCourses = new List<Guid> { courseId },
                Capacity = 100,
                EventType = src.Domain.Enums.EventType.Palestra,
                InstitutionId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2)
            };
            _authServiceMock
                .Setup(a => a.GetUserInfoAsync(userId))
                .ReturnsAsync(new UserResponseDto
                {
                    Id = userId,
                    Role = "Professor",
                    Email = "professor@teste.com"
                });
            _courseRepositoryMock
                .Setup(r => r.GetByInstitutionAsync(dto.InstitutionId))
                .ReturnsAsync(new List<Course>());
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventService.AddAsync(dto, userId));
            Assert.Equal("Nem todos os cursos cadastrados são permitidos.", exception.Message);
            _eventRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Never);
        }
        //Cenário 9: Caso-limite com verificação de interação mockada (Times.Never)
        [Fact]
        public async Task SetActiveAsync_QuandoDataForPassadaEForDesativar_DeveLancarArgumentExceptionENaoChamarSetActiveAsyncEDeactivateEventAsync()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var existingEvent = new Event
            {
                Id = eventId,
                Name = "Evento de Teste",
                Description = "Descrição do evento de teste",
                AllowDocuments = true,
                AllowedCourses = new List<Course>(),
                Capacity = 100,
                EventType = src.Domain.Enums.EventType.Palestra,
                InstitutionId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
            };
            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(existingEvent);
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventService.SetActiveAsync(eventId, false));

            Assert.Equal("Não é possível inativar um evento que já começou.", exception.Message);
            _eventRepositoryMock.Verify(r => r.SetActiveAsync(It.IsAny<Event>(), It.IsAny<bool>()), Times.Never);
            _googleCalendarServiceMock.Verify(r => r.DeactivateEventAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task JoinEventAsync_QuandoEventoLotadoEInscricaoAnteriorCancelada_DeveLancarExcecaoENaoAtualizarEvento()
        {
            var institutionId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var course = new Course { Id = Guid.NewGuid(), Name = "ADS", InstitutionId = institutionId };
            var evt = new Event
            {
                Id = eventId,
                InstitutionId = institutionId,
                IsActive = true,
                Capacity = 1,
                StartDate = DateTime.UtcNow.AddDays(1),
                AllowedCourses = new List<Course> { course },
                EventStudents = new List<EventStudent>
                {
                    new() { EventId = eventId, StudentId = Guid.NewGuid(), Status = RegistrationStatus.Confirmed, RegisteredAt = DateTime.UtcNow.AddMinutes(-30) },
                    new() { EventId = eventId, StudentId = userId, Status = RegistrationStatus.Canceled, RegisteredAt = DateTime.UtcNow.AddHours(-2), CanceledAt = DateTime.UtcNow.AddHours(-1) }
                }
            };

            _authServiceMock
                .Setup(a => a.GetUserInfoAsync(userId))
                .ReturnsAsync(new UserResponseDto { Id = userId, InstitutionId = institutionId, Email = "aluno@teste.com" });
            _eventRepositoryMock
                .Setup(r => r.GetFilteredAvailableForUserAsync(It.IsAny<EventFilterDto>(), userId))
                .ReturnsAsync((new List<Event> { evt }, 1));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventService.JoinEventAsync(eventId, userId));

            Assert.Equal("Não é possível se inscrever neste evento, pois a capacidade máxima foi atingida.", exception.Message);
            _eventRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task GetRegistrationsAsync_QuandoHouverInscritosConfirmados_DeveRetornarPaginadoComCursoEData()
        {
            var institutionId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var course = new Course { Id = Guid.NewGuid(), Name = "Sistemas de Informação", InstitutionId = institutionId };
            var firstStudentId = Guid.NewGuid();
            var secondStudentId = Guid.NewGuid();
            var firstRegisteredAt = DateTime.UtcNow.AddHours(-3);
            var secondRegisteredAt = DateTime.UtcNow.AddHours(-1);
            var evt = new Event
            {
                Id = eventId,
                InstitutionId = institutionId,
                IsActive = true,
                Capacity = 10,
                StartDate = DateTime.UtcNow.AddDays(1),
                EventStudents = new List<EventStudent>
                {
                    new()
                    {
                        EventId = eventId,
                        StudentId = secondStudentId,
                        Status = RegistrationStatus.Confirmed,
                        RegisteredAt = secondRegisteredAt,
                        Student = new Student { Id = secondStudentId, Name = "Aluno 2", CourseId = course.Id, Course = course }
                    },
                    new()
                    {
                        EventId = eventId,
                        StudentId = firstStudentId,
                        Status = RegistrationStatus.Confirmed,
                        RegisteredAt = firstRegisteredAt,
                        Student = new Student { Id = firstStudentId, Name = "Aluno 1", CourseId = course.Id, Course = course }
                    },
                    new()
                    {
                        EventId = eventId,
                        StudentId = Guid.NewGuid(),
                        Status = RegistrationStatus.Canceled,
                        RegisteredAt = DateTime.UtcNow.AddHours(-5),
                        Student = new Student { Id = Guid.NewGuid(), Name = "Aluno Cancelado", CourseId = course.Id, Course = course }
                    }
                }
            };

            _authServiceMock
                .Setup(a => a.GetUserInfoAsync(userId))
                .ReturnsAsync(new UserResponseDto { Id = userId, InstitutionId = institutionId, Email = "professor@teste.com" });
            _eventRepositoryMock
                .Setup(r => r.GetFilteredAvailableForUserAsync(It.IsAny<EventFilterDto>(), userId))
                .ReturnsAsync((new List<Event> { evt }, 1));

            var result = await _eventService.GetRegistrationsAsync(eventId, userId, new EventRegistrationFilterDto
            {
                PageNumber = 1,
                PageSize = 1
            });

            Assert.Equal(2, result.TotalItems);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(1, result.PageSize);
            Assert.Single(result.Items);

            var item = result.Items.Single();
            Assert.Equal(firstStudentId, item.StudentId);
            Assert.Equal("Aluno 1", item.StudentName);
            Assert.Equal(course.Name, item.CourseName);
            Assert.Equal(firstRegisteredAt, item.RegisteredAt);
        }
    }
}

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
            // Arrange
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
            var result = await _eventService.AddAsync(dto);
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
        // Cenário 8: Violação de regra/erro com teste de exceção (tipo e mensagem) e mock verify (Times.Never)
        [Fact]
        public async Task CreateEventAsync_QuandoCursoNaoPertencerAInstituicao_DeveLancarInvalidOperationExceptionENaoChamarAddAsync()
        {
            // Arrange
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
            _courseRepositoryMock
                .Setup(r => r.GetByInstitutionAsync(dto.InstitutionId))
                .ReturnsAsync(new List<Course>());
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                                                        _eventService.AddAsync(dto));
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

    }
}

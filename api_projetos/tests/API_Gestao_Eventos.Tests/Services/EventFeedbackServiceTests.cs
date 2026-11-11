using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using Moq;
using Xunit;

namespace API_Gestao_Eventos.Tests.Services
{
    public class EventFeedbackServiceTests
    {
        private readonly Mock<IEventFeedbackRepository> _eventFeedbackRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly EventFeedbackService _eventFeedbackService;

        public EventFeedbackServiceTests()
        {
            _eventFeedbackRepositoryMock = new Mock<IEventFeedbackRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _eventFeedbackService = new EventFeedbackService(_eventFeedbackRepositoryMock.Object, _userRepositoryMock.Object);
        }

        private static Event SeedEvent(DateTime? endDate = null, Guid? institutionId = null, Guid? courseId = null) => new()
        {
            Id = Guid.NewGuid(),
            Name = "Evento de Teste",
            InstitutionId = institutionId ?? Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(-2),
            EndDate = endDate ?? DateTime.UtcNow.AddHours(-1),
            AllowedCourses = courseId.HasValue ? new List<Course> { new() { Id = courseId.Value, Name = "ADS" } } : new List<Course>()
        };

        private static EventFeedback SeedFeedback(Guid eventId, Guid studentId) => new()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            StudentId = studentId,
            Rating = 9,
            FeedbackText = "Excelente evento!",
            CreatedAt = DateTime.UtcNow,
            Student = new Student
            {
                Id = studentId,
                Name = "Aluno Teste",
                Course = new Course { Id = Guid.NewGuid(), Name = "Engenharia" }
            }
        };

        [Fact]
        public async Task CreateFeedbackAsync_QuandoEventoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var eventId = Guid.NewGuid();
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(eventId)).ReturnsAsync((Event?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _eventFeedbackService.CreateFeedbackAsync(eventId, Guid.NewGuid(), new CreateEventFeedbackRequestDto { Rating = 8 }));

            Assert.Equal("Evento não encontrado.", exception.Message);
        }

        [Fact]
        public async Task CreateFeedbackAsync_QuandoEventoAindaNaoTerminou_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(endDate: DateTime.UtcNow.AddHours(2));
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventFeedbackService.CreateFeedbackAsync(evt.Id, Guid.NewGuid(), new CreateEventFeedbackRequestDto { Rating = 8 }));

            Assert.Equal("O feedback só pode ser enviado após o término do evento.", exception.Message);
        }

        [Fact]
        public async Task CreateFeedbackAsync_QuandoAlunoNaoEstiverInscrito_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _eventFeedbackRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, studentId)).ReturnsAsync((EventStudent?)null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventFeedbackService.CreateFeedbackAsync(evt.Id, studentId, new CreateEventFeedbackRequestDto { Rating = 8 }));

            Assert.Equal("Você não está inscrito neste evento.", exception.Message);
        }

        [Fact]
        public async Task CreateFeedbackAsync_QuandoAlunoNaoTiverPresencaConfirmada_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            var registration = new EventStudent { EventId = evt.Id, StudentId = studentId, Status = RegistrationStatus.Confirmed, AttendedAt = null };
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _eventFeedbackRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, studentId)).ReturnsAsync(registration);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventFeedbackService.CreateFeedbackAsync(evt.Id, studentId, new CreateEventFeedbackRequestDto { Rating = 8 }));

            Assert.Equal("Você só pode avaliar o evento se tiver presença confirmada.", exception.Message);
        }

        [Fact]
        public async Task CreateFeedbackAsync_QuandoFeedbackJaFoiEnviado_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            var registration = new EventStudent { EventId = evt.Id, StudentId = studentId, Status = RegistrationStatus.Attended };
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _eventFeedbackRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, studentId)).ReturnsAsync(registration);
            _eventFeedbackRepositoryMock.Setup(r => r.GetByEventAndStudentAsync(evt.Id, studentId)).ReturnsAsync(SeedFeedback(evt.Id, studentId));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventFeedbackService.CreateFeedbackAsync(evt.Id, studentId, new CreateEventFeedbackRequestDto { Rating = 8 }));

            Assert.Equal("Você já enviou um feedback para este evento.", exception.Message);
        }

        [Fact]
        public async Task CreateFeedbackAsync_QuandoDadosValidos_DeveSalvarERetornarDto()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            var registration = new EventStudent { EventId = evt.Id, StudentId = studentId, Status = RegistrationStatus.Confirmed, AttendedAt = DateTime.UtcNow };
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _eventFeedbackRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, studentId)).ReturnsAsync(registration);
            _eventFeedbackRepositoryMock.SetupSequence(r => r.GetByEventAndStudentAsync(evt.Id, studentId))
                .ReturnsAsync((EventFeedback?)null)
                .ReturnsAsync(SeedFeedback(evt.Id, studentId));

            var result = await _eventFeedbackService.CreateFeedbackAsync(evt.Id, studentId, new CreateEventFeedbackRequestDto
            {
                Rating = 9,
                FeedbackText = " Excelente evento! "
            });

            Assert.Equal(evt.Id, result.EventId);
            Assert.Equal(studentId, result.StudentId);
            Assert.Equal(9, result.Rating);
            Assert.Equal("Aluno Teste", result.StudentName);
            _eventFeedbackRepositoryMock.Verify(r => r.AddAsync(It.Is<EventFeedback>(f => f.FeedbackText == "Excelente evento!")), Times.Once);
        }

        [Fact]
        public async Task GetMyFeedbackAsync_QuandoEventoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var eventId = Guid.NewGuid();
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(eventId)).ReturnsAsync((Event?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _eventFeedbackService.GetMyFeedbackAsync(eventId, Guid.NewGuid()));

            Assert.Equal("Evento não encontrado.", exception.Message);
        }

        [Fact]
        public async Task GetMyFeedbackAsync_QuandoFeedbackNaoExistir_DeveRetornarNulo()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _eventFeedbackRepositoryMock.Setup(r => r.GetByEventAndStudentAsync(evt.Id, studentId)).ReturnsAsync((EventFeedback?)null);

            var result = await _eventFeedbackService.GetMyFeedbackAsync(evt.Id, studentId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetMyFeedbackAsync_QuandoFeedbackExistir_DeveRetornarDto()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            var feedback = SeedFeedback(evt.Id, studentId);
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _eventFeedbackRepositoryMock.Setup(r => r.GetByEventAndStudentAsync(evt.Id, studentId)).ReturnsAsync(feedback);

            var result = await _eventFeedbackService.GetMyFeedbackAsync(evt.Id, studentId);

            Assert.NotNull(result);
            Assert.Equal(feedback.Id, result.Id);
            Assert.Equal(feedback.Rating, result.Rating);
        }

        [Fact]
        public async Task GetEventFeedbacksAsync_QuandoUsuarioNaoExistir_DeveLancarKeyNotFoundException()
        {
            var evt = SeedEvent();
            var userId = Guid.NewGuid();
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _eventFeedbackService.GetEventFeedbacksAsync(evt.Id, userId));

            Assert.Equal("Usuário inválido.", exception.Message);
        }

        [Fact]
        public async Task GetEventFeedbacksAsync_QuandoUsuarioSemPermissao_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var student = new Student { Id = Guid.NewGuid() };
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(student.Id)).ReturnsAsync(student);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _eventFeedbackService.GetEventFeedbacksAsync(evt.Id, student.Id));

            Assert.Equal("Você não tem permissão para visualizar os feedbacks deste evento.", exception.Message);
        }

        [Fact]
        public async Task GetEventFeedbacksAsync_QuandoProfessorComCursoPermitido_DeveRetornarListaDeFeedbacks()
        {
            var courseId = Guid.NewGuid();
            var evt = SeedEvent(courseId: courseId);
            var teacher = new Teacher { Id = Guid.NewGuid(), Courses = new List<Course> { new() { Id = courseId } } };
            var feedback = SeedFeedback(evt.Id, Guid.NewGuid());
            _eventFeedbackRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(teacher.Id)).ReturnsAsync(teacher);
            _eventFeedbackRepositoryMock.Setup(r => r.GetByEventAsync(evt.Id)).ReturnsAsync(new[] { feedback });

            var result = (await _eventFeedbackService.GetEventFeedbacksAsync(evt.Id, teacher.Id)).ToList();

            Assert.Single(result);
            Assert.Equal(feedback.Id, result[0].Id);
        }
    }
}


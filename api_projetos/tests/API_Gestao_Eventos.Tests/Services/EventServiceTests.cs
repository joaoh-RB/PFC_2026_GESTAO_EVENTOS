using API_Gestao_Eventos.src.Application.DTO.Auth;
using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Infrastructure.Services.GoogleCalendar;
using Moq;
using Xunit;

namespace API_Gestao_Eventos.Tests.Services
{
    public class EventServiceTests
    {
        private readonly EventService _eventService;
        private readonly Mock<IEventRepository> _eventRepositoryMock;
        private readonly Mock<ICourseRepository> _courseRepositoryMock;
        private readonly Mock<IGoogleCalendarService> _googleCalendarServiceMock;
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<IGroupRepository> _groupRepositoryMock;

        public EventServiceTests()
        {
            _eventRepositoryMock = new Mock<IEventRepository>();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _googleCalendarServiceMock = new Mock<IGoogleCalendarService>();
            _authServiceMock = new Mock<IAuthService>();
            _groupRepositoryMock = new Mock<IGroupRepository>();
            _eventService = new EventService(
                _eventRepositoryMock.Object,
                _courseRepositoryMock.Object,
                _googleCalendarServiceMock.Object,
                _authServiceMock.Object,
                _groupRepositoryMock.Object);
        }

        private static CreateEventRequestDto SeedCreateRequest(Guid? institutionId = null, Guid? courseId = null, bool allowDocuments = true) => new()
        {
            Name = "Evento de Teste",
            Description = "Descrição do evento de teste",
            AllowDocuments = allowDocuments,
            MaxGroupSize = 4,
            SubmissionDeadline = DateTimeOffset.UtcNow.AddDays(3),
            AllowedCourses = new List<Guid> { courseId ?? Guid.NewGuid() },
            Capacity = 100,
            EventType = EventType.Palestra,
            InstitutionId = institutionId ?? Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow.AddDays(4),
            EndDate = DateTimeOffset.UtcNow.AddDays(5)
        };

        private static Event SeedEvent(
            Guid? id = null,
            Guid? institutionId = null,
            bool isActive = true,
            EventStatus status = EventStatus.Agendado,
            int capacity = 10,
            string? googleCalendarId = "google-event-id",
            DateTime? startDate = null) => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Evento de Teste",
            Description = "Descrição",
            InstitutionId = institutionId ?? Guid.NewGuid(),
            Institution = new Institution { Id = institutionId ?? Guid.NewGuid(), Name = "Instituição Teste" },
            IsActive = isActive,
            EventStatus = status,
            Capacity = capacity,
            GoogleCalendarEventId = googleCalendarId,
            StartDate = startDate ?? DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2),
            AllowedCourses = new List<Course> { new() { Id = Guid.NewGuid(), Name = "ADS" } },
            EventStudents = new List<EventStudent>()
        };

        private UserResponseDto SeedAuthUser(Guid? userId = null, string role = "Professor", Guid? institutionId = null)
        {
            var dto = new UserResponseDto
            {
                Id = userId ?? Guid.NewGuid(),
                Role = role,
                Email = "usuario@teste.com",
                InstitutionId = institutionId ?? Guid.NewGuid()
            };
            _authServiceMock.Setup(a => a.GetUserInfoAsync(dto.Id)).ReturnsAsync(dto);
            return dto;
        }

        [Fact]
        public async Task AddAsync_QuandoDadosValidosPorProfessor_DeveCadastrarComoAgendadoECriarNoGoogleCalendar()
        {
            var courseId = Guid.NewGuid();
            var dto = SeedCreateRequest(courseId: courseId);
            var user = SeedAuthUser(role: "Professor");
            var savedEvent = SeedEvent();

            _courseRepositoryMock.Setup(r => r.GetByInstitutionAsync(dto.InstitutionId))
                .ReturnsAsync(new List<Course> { new() { Id = courseId, Name = "Curso de Teste" } });
            _googleCalendarServiceMock.Setup(g => g.CreateEventAsync(It.IsAny<Event>())).ReturnsAsync("google-event-id");
            _eventRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>())).ReturnsAsync(savedEvent);

            var result = await _eventService.AddAsync(dto, user.Id);

            Assert.Equal(savedEvent, result);
            _googleCalendarServiceMock.Verify(g => g.CreateEventAsync(It.IsAny<Event>()), Times.Once);
            _eventRepositoryMock.Verify(r => r.AddAsync(It.Is<Event>(e =>
                e.Name == dto.Name &&
                e.EventStatus == EventStatus.Agendado &&
                e.GoogleCalendarEventId == "google-event-id")), Times.Once);
        }

        [Fact]
        public async Task AddAsync_QuandoCriadoPorAluno_DeveCadastrarComoSolicitadoSemGoogleCalendar()
        {
            var courseId = Guid.NewGuid();
            var dto = SeedCreateRequest(courseId: courseId, allowDocuments: false);
            var user = SeedAuthUser(role: nameof(UserRole.Aluno));

            _courseRepositoryMock.Setup(r => r.GetByInstitutionAsync(dto.InstitutionId))
                .ReturnsAsync(new List<Course> { new() { Id = courseId, Name = "Curso" } });
            _eventRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>())).ReturnsAsync((Event e) => e);

            var result = await _eventService.AddAsync(dto, user.Id);

            Assert.Equal(EventStatus.Solicitado, result.EventStatus);
            Assert.Null(result.MaxGroupSize);
            Assert.Null(result.SubmissionDeadline);
            _googleCalendarServiceMock.Verify(g => g.CreateEventAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_QuandoCursoNaoPertencerAInstituicao_DeveLancarInvalidOperationException()
        {
            var dto = SeedCreateRequest();
            var user = SeedAuthUser();
            _courseRepositoryMock.Setup(r => r.GetByInstitutionAsync(dto.InstitutionId)).ReturnsAsync(new List<Course>());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _eventService.AddAsync(dto, user.Id));

            Assert.Equal("Nem todos os cursos cadastrados são permitidos.", exception.Message);
            _eventRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_QuandoCursoNaoPertencerAInstituicao_DeveLancarInvalidOperationException()
        {
            var dto = SeedCreateRequest();
            _courseRepositoryMock.Setup(r => r.GetByInstitutionAsync(dto.InstitutionId)).ReturnsAsync(new List<Course>());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _eventService.UpdateAsync(Guid.NewGuid(), dto));

            Assert.Equal("Nem todos os cursos cadastrados pertencem à instituição.", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_QuandoEventoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var courseId = Guid.NewGuid();
            var dto = SeedCreateRequest(courseId: courseId);
            var eventId = Guid.NewGuid();
            _courseRepositoryMock.Setup(r => r.GetByInstitutionAsync(dto.InstitutionId))
                .ReturnsAsync(new List<Course> { new() { Id = courseId } });
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((Event?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _eventService.UpdateAsync(eventId, dto));

            Assert.Equal("Evento não encontrado.", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_QuandoDadosValidos_DeveAtualizarNoGoogleCalendarENoRepositorio()
        {
            var courseId = Guid.NewGuid();
            var dto = SeedCreateRequest(courseId: courseId);
            var existingEvent = SeedEvent();
            _courseRepositoryMock.Setup(r => r.GetByInstitutionAsync(dto.InstitutionId))
                .ReturnsAsync(new List<Course> { new() { Id = courseId } });
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(existingEvent.Id)).ReturnsAsync(existingEvent);

            await _eventService.UpdateAsync(existingEvent.Id, dto);

            _googleCalendarServiceMock.Verify(g => g.UpdateEventAsync(It.Is<Event>(e => e.Id == existingEvent.Id && e.Name == dto.Name)), Times.Once);
            _eventRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Event>(e => e.Id == existingEvent.Id)), Times.Once);
        }

        [Fact]
        public async Task SetApprovalAsync_QuandoStatusForInvalido_DeveLancarInvalidOperationException()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventService.SetApprovalAsync(Guid.NewGuid(), EventStatus.Solicitado, Guid.NewGuid()));

            Assert.Equal("Use apenas aprovação (Agendado) ou reprovação (Reprovado).", exception.Message);
        }

        [Fact]
        public async Task SetApprovalAsync_QuandoEventoNaoEstiverSolicitado_DeveLancarInvalidOperationException()
        {
            var approver = SeedAuthUser();
            var evt = SeedEvent(status: EventStatus.Agendado);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventService.SetApprovalAsync(evt.Id, EventStatus.Agendado, approver.Id));

            Assert.Equal("Apenas eventos com status 'Solicitado' podem ser aprovados ou reprovados.", exception.Message);
        }

        [Fact]
        public async Task SetApprovalAsync_QuandoAprovarEventoSolicitadoSemGoogleId_DeveCriarNoGoogleCalendarEAprovar()
        {
            var approver = SeedAuthUser();
            var evt = SeedEvent(status: EventStatus.Solicitado, googleCalendarId: null);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _googleCalendarServiceMock.Setup(g => g.CreateEventAsync(evt)).ReturnsAsync("novo-google-id");

            await _eventService.SetApprovalAsync(evt.Id, EventStatus.Agendado, approver.Id);

            _googleCalendarServiceMock.Verify(g => g.CreateEventAsync(evt), Times.Once);
            _eventRepositoryMock.Verify(r => r.SetApprovalStatusAsync(evt, EventStatus.Agendado, "novo-google-id"), Times.Once);
        }

        [Fact]
        public async Task SetActiveAsync_QuandoDataForPassadaEForDesativar_DeveLancarInvalidOperationException()
        {
            var existingEvent = SeedEvent(startDate: DateTime.UtcNow.AddDays(-1));
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(existingEvent.Id)).ReturnsAsync(existingEvent);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventService.SetActiveAsync(existingEvent.Id, false));

            Assert.Equal("Não é possível inativar um evento que já começou.", exception.Message);
            _eventRepositoryMock.Verify(r => r.SetActiveAsync(It.IsAny<Event>(), It.IsAny<bool>()), Times.Never);
            _googleCalendarServiceMock.Verify(r => r.DeactivateEventAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task SetActiveAsync_QuandoEventoFuturo_DeveAtualizarGoogleCalendarERepositorio()
        {
            var existingEvent = SeedEvent(startDate: DateTime.UtcNow.AddDays(2));
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(existingEvent.Id)).ReturnsAsync(existingEvent);

            await _eventService.SetActiveAsync(existingEvent.Id, false);

            _googleCalendarServiceMock.Verify(g => g.DeactivateEventAsync(existingEvent.GoogleCalendarEventId!, false), Times.Once);
            _eventRepositoryMock.Verify(r => r.SetActiveAsync(existingEvent, false), Times.Once);
        }

        [Fact]
        public async Task GetRequestedEventsPagedAsync_QuandoChamado_DeveFiltrarPorStatusSolicitadoEMapear()
        {
            var userId = Guid.NewGuid();
            var evt = SeedEvent(status: EventStatus.Solicitado);
            evt.CreatedByUser = new Student { Id = userId, Name = "Aluno Criador", UniqueIdentifier = "RGM123" };
            evt.EventStudents.Add(new EventStudent { StudentId = userId, Status = RegistrationStatus.Confirmed, AttendedAt = DateTime.UtcNow });

            _eventRepositoryMock.Setup(r => r.GetFilteredAvailableForUserAsync(It.IsAny<EventFilterDto>(), userId))
                .ReturnsAsync((new[] { evt }, 1));
            _eventRepositoryMock.Setup(r => r.GetAllAdditionsToCalendarAsync(userId))
                .ReturnsAsync(new[] { new EventAddedToCalendar { EventId = evt.Id, UserId = userId } });
            _eventRepositoryMock.Setup(r => r.GetEventIdsWithApprovedDocumentsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new HashSet<Guid> { evt.Id });

            var result = await _eventService.GetRequestedEventsPagedAsync(new EventFilterDto { PageNumber = 1, PageSize = 10 }, userId);

            Assert.Equal(1, result.TotalItems);
            var item = result.Items.Single();
            Assert.Equal(evt.Id, item.Id);
            Assert.True(item.HasApprovedDocuments);
            Assert.True(item.IsAddedToCalendar);
            Assert.True(item.IsUserRegistered);
            Assert.True(item.IsUserAttended);
            Assert.Equal("RGM123", item.CreatedByUniqueIdentifier);
        }

        [Fact]
        public async Task AddUserToCalendarAsync_QuandoEventoSemGoogleCalendarId_DeveLancarInvalidOperationException()
        {
            var user = SeedAuthUser();
            var evt = SeedEvent(googleCalendarId: null);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventService.AddUserToCalendarAsync(evt.Id, user.Id));

            Assert.Equal("Este evento não possui integração com Google Calendar.", exception.Message);
        }

        [Fact]
        public async Task AddUserToCalendarAsync_QuandoNaoAdicionadoAntes_DeveAdicionarParticipanteESalvarRegistro()
        {
            var user = SeedAuthUser();
            var evt = SeedEvent();
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _eventRepositoryMock.Setup(r => r.GetEventAddedToCalendarAsync(evt.Id, user.Id)).ReturnsAsync((EventAddedToCalendar?)null);

            await _eventService.AddUserToCalendarAsync(evt.Id, user.Id);

            _googleCalendarServiceMock.Verify(g => g.AddAttendeeAsync(evt.GoogleCalendarEventId!, user.Email), Times.Once);
            _eventRepositoryMock.Verify(r => r.AddEventToCalendarAsync(It.Is<EventAddedToCalendar>(c => c.EventId == evt.Id && c.UserId == user.Id && c.IsActive)), Times.Once);
        }

        [Fact]
        public async Task AddUserToCalendarAsync_QuandoRegistroExistirInativo_DeveReativarRegistro()
        {
            var user = SeedAuthUser();
            var evt = SeedEvent();
            var existingRecord = new EventAddedToCalendar { EventId = evt.Id, UserId = user.Id, IsActive = false };
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _eventRepositoryMock.Setup(r => r.GetEventAddedToCalendarAsync(evt.Id, user.Id)).ReturnsAsync(existingRecord);

            await _eventService.AddUserToCalendarAsync(evt.Id, user.Id);

            Assert.True(existingRecord.IsActive);
            _eventRepositoryMock.Verify(r => r.UpdateEventInCalendarAsync(existingRecord), Times.Once);
        }

        [Fact]
        public async Task RemoveUserFromCalendarAsync_QuandoRegistroExistir_DeveRemoverDoGoogleCalendarEInativarRegistro()
        {
            var user = SeedAuthUser();
            var evt = SeedEvent();
            var existingRecord = new EventAddedToCalendar { EventId = evt.Id, UserId = user.Id, IsActive = true };
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _eventRepositoryMock.Setup(r => r.GetEventAddedToCalendarAsync(evt.Id, user.Id)).ReturnsAsync(existingRecord);

            await _eventService.RemoveUserFromCalendarAsync(evt.Id, user.Id);

            Assert.False(existingRecord.IsActive);
            _googleCalendarServiceMock.Verify(g => g.RemoveAttendeeAsync(evt.GoogleCalendarEventId!, user.Email), Times.Once);
            _eventRepositoryMock.Verify(r => r.UpdateEventInCalendarAsync(existingRecord), Times.Once);
        }

        [Fact]
        public async Task JoinEventAsync_QuandoEventoLotadoEInscricaoAnteriorCancelada_DeveLancarExcecaoENaoAtualizarEvento()
        {
            var user = SeedAuthUser(role: "Aluno");
            var evt = SeedEvent(institutionId: user.InstitutionId, capacity: 1);
            evt.EventStudents.Add(new EventStudent { EventId = evt.Id, StudentId = Guid.NewGuid(), Status = RegistrationStatus.Confirmed });
            evt.EventStudents.Add(new EventStudent { EventId = evt.Id, StudentId = user.Id, Status = RegistrationStatus.Canceled });

            _eventRepositoryMock.Setup(r => r.GetFilteredAvailableForUserAsync(It.IsAny<EventFilterDto>(), user.Id))
                .ReturnsAsync((new List<Event> { evt }, 1));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventService.JoinEventAsync(evt.Id, user.Id));

            Assert.Equal("Não é possível se inscrever neste evento, pois a capacidade máxima foi atingida.", exception.Message);
            _eventRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task JoinEventAsync_QuandoNovaInscricaoValida_DeveAdicionarInscricaoEAtualizarEvento()
        {
            var user = SeedAuthUser(role: "Aluno");
            var evt = SeedEvent(institutionId: user.InstitutionId, capacity: 10);
            _eventRepositoryMock.Setup(r => r.GetFilteredAvailableForUserAsync(It.IsAny<EventFilterDto>(), user.Id))
                .ReturnsAsync((new List<Event> { evt }, 1));

            await _eventService.JoinEventAsync(evt.Id, user.Id);

            Assert.Single(evt.EventStudents);
            Assert.Equal(user.Id, evt.EventStudents.First().StudentId);
            _eventRepositoryMock.Verify(r => r.UpdateAsync(evt), Times.Once);
        }

        [Fact]
        public async Task JoinEventAsync_QuandoReinscricaoDeInscricaoCanceladaComVagas_DeveReativarInscricao()
        {
            var user = SeedAuthUser(role: "Aluno");
            var evt = SeedEvent(institutionId: user.InstitutionId, capacity: 10);
            evt.EventStudents.Add(new EventStudent { EventId = evt.Id, StudentId = user.Id, Status = RegistrationStatus.Canceled, CanceledAt = DateTime.UtcNow });
            _eventRepositoryMock.Setup(r => r.GetFilteredAvailableForUserAsync(It.IsAny<EventFilterDto>(), user.Id))
                .ReturnsAsync((new List<Event> { evt }, 1));

            await _eventService.JoinEventAsync(evt.Id, user.Id);

            Assert.Equal(RegistrationStatus.Confirmed, evt.EventStudents.First().Status);
            Assert.Null(evt.EventStudents.First().CanceledAt);
            _eventRepositoryMock.Verify(r => r.UpdateAsync(evt), Times.Once);
        }

        [Fact]
        public async Task LeaveEventAsync_QuandoAlunoParticiparDeGrupo_DeveLancarInvalidOperationException()
        {
            var user = SeedAuthUser(role: "Aluno");
            var evt = SeedEvent(institutionId: user.InstitutionId);
            evt.EventStudents.Add(new EventStudent { EventId = evt.Id, StudentId = user.Id, Status = RegistrationStatus.Confirmed });
            _eventRepositoryMock.Setup(r => r.GetFilteredAvailableForUserAsync(It.IsAny<EventFilterDto>(), user.Id))
                .ReturnsAsync((new List<Event> { evt }, 1));
            _groupRepositoryMock.Setup(g => g.HasAcceptedGroupAsync(user.Id, evt.Id)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventService.LeaveEventAsync(evt.Id, user.Id));

            Assert.Equal("Saia do grupo antes de cancelar sua inscrição no evento.", exception.Message);
        }

        [Fact]
        public async Task LeaveEventAsync_QuandoInscritoESemGrupo_DeveCancelarInscricaoERemoverDoGoogleCalendar()
        {
            var user = SeedAuthUser(role: "Aluno");
            var evt = SeedEvent(institutionId: user.InstitutionId);
            var registration = new EventStudent { EventId = evt.Id, StudentId = user.Id, Status = RegistrationStatus.Confirmed };
            evt.EventStudents.Add(registration);
            _eventRepositoryMock.Setup(r => r.GetFilteredAvailableForUserAsync(It.IsAny<EventFilterDto>(), user.Id))
                .ReturnsAsync((new List<Event> { evt }, 1));
            _groupRepositoryMock.Setup(g => g.HasAcceptedGroupAsync(user.Id, evt.Id)).ReturnsAsync(false);

            await _eventService.LeaveEventAsync(evt.Id, user.Id);

            Assert.Equal(RegistrationStatus.Canceled, registration.Status);
            Assert.NotNull(registration.CanceledAt);
            _googleCalendarServiceMock.Verify(g => g.RemoveAttendeeAsync(evt.GoogleCalendarEventId!, user.Email), Times.Once);
            _eventRepositoryMock.Verify(r => r.UpdateAsync(evt), Times.Once);
        }

        [Fact]
        public async Task GetRegistrationsAsync_QuandoHouverInscritosConfirmados_DeveRetornarPaginadoComCursoEData()
        {
            var user = SeedAuthUser(role: "Professor");
            var evt = SeedEvent(institutionId: user.InstitutionId);
            var course = new Course { Id = Guid.NewGuid(), Name = "Sistemas de Informação", InstitutionId = user.InstitutionId!.Value };
            var firstStudentId = Guid.NewGuid();
            var secondStudentId = Guid.NewGuid();
            var firstRegisteredAt = DateTime.UtcNow.AddHours(-3);
            var secondRegisteredAt = DateTime.UtcNow.AddHours(-1);

            evt.EventStudents = new List<EventStudent>
            {
                new()
                {
                    EventId = evt.Id,
                    StudentId = secondStudentId,
                    Status = RegistrationStatus.Confirmed,
                    RegisteredAt = secondRegisteredAt,
                    Student = new Student { Id = secondStudentId, Name = "Aluno 2", CourseId = course.Id, Course = course }
                },
                new()
                {
                    EventId = evt.Id,
                    StudentId = firstStudentId,
                    Status = RegistrationStatus.Confirmed,
                    RegisteredAt = firstRegisteredAt,
                    Student = new Student { Id = firstStudentId, Name = "Aluno 1", CourseId = course.Id, Course = course }
                },
                new()
                {
                    EventId = evt.Id,
                    StudentId = Guid.NewGuid(),
                    Status = RegistrationStatus.Canceled,
                    RegisteredAt = DateTime.UtcNow.AddHours(-5),
                    Student = new Student { Id = Guid.NewGuid(), Name = "Aluno Cancelado", CourseId = course.Id, Course = course }
                }
            };

            _eventRepositoryMock.Setup(r => r.GetFilteredAvailableForUserAsync(It.IsAny<EventFilterDto>(), user.Id))
                .ReturnsAsync((new List<Event> { evt }, 1));

            var result = await _eventService.GetRegistrationsAsync(evt.Id, user.Id, new EventRegistrationFilterDto
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

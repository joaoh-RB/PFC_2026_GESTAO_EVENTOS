using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using Moq;

namespace API_Gestao_Eventos.Tests.Services
{
    public class AttendanceServiceTests
    {
        private readonly Mock<IAttendanceRepository> _attendanceRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly AttendanceService _attendanceService;

        public AttendanceServiceTests()
        {
            _attendanceRepositoryMock = new Mock<IAttendanceRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _attendanceService = new AttendanceService(_attendanceRepositoryMock.Object, _userRepositoryMock.Object);
        }

        private static Event SeedEvent(DateTime? startDate = null, DateTime? endDate = null, string? code = "ABC234", Guid? institutionId = null, Guid? courseId = null) => new()
        {
            Id = Guid.NewGuid(),
            Name = "Evento Teste",
            InstitutionId = institutionId ?? Guid.NewGuid(),
            StartDate = startDate ?? DateTime.UtcNow.AddHours(-1),
            EndDate = endDate ?? DateTime.UtcNow.AddHours(2),
            AttendanceCode = code,
            AllowedCourses = courseId.HasValue ? new List<Course> { new() { Id = courseId.Value, Name = "ADS" } } : new List<Course>()
        };

        private static EventStudent SeedRegistration(Guid eventId, Guid? studentId = null, RegistrationStatus status = RegistrationStatus.Confirmed, DateTime? attendedAt = null) => new()
        {
            EventId = eventId,
            StudentId = studentId ?? Guid.NewGuid(),
            Status = status,
            AttendedAt = attendedAt,
            AttendanceMethod = attendedAt.HasValue ? AttendanceMethod.Code : null,
            Student = new Student
            {
                Id = studentId ?? Guid.NewGuid(),
                Name = "Aluno Teste",
                Course = new Course { Id = Guid.NewGuid(), Name = "ADS" }
            }
        };

        private Administrator SeedAdmin()
        {
            var admin = new Administrator { Id = Guid.NewGuid() };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);
            return admin;
        }

        [Fact]
        public async Task GetCodeAsync_QuandoEventoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var eventId = Guid.NewGuid();
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(eventId)).ReturnsAsync((Event?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _attendanceService.GetCodeAsync(eventId, Guid.NewGuid()));

            Assert.Equal("Evento não encontrado.", exception.Message);
        }

        [Fact]
        public async Task GetCodeAsync_QuandoUsuarioNaoExistir_DeveLancarKeyNotFoundException()
        {
            var evt = SeedEvent();
            var userId = Guid.NewGuid();
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _attendanceService.GetCodeAsync(evt.Id, userId));

            Assert.Equal("Usuário inválido.", exception.Message);
        }

        [Fact]
        public async Task GetCodeAsync_QuandoUsuarioSemPermissao_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var student = new Student { Id = Guid.NewGuid() };
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(student.Id)).ReturnsAsync(student);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _attendanceService.GetCodeAsync(evt.Id, student.Id));

            Assert.Equal("Você não tem permissão para gerenciar a presença deste evento.", exception.Message);
        }

        [Fact]
        public async Task GetCodeAsync_QuandoEventoAindaNaoComecou_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(startDate: DateTime.UtcNow.AddHours(2));
            var admin = SeedAdmin();
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _attendanceService.GetCodeAsync(evt.Id, admin.Id));

            Assert.Equal("O código de presença só fica disponível depois do início do evento.", exception.Message);
        }

        [Fact]
        public async Task GetCodeAsync_QuandoCodigoEstiverVazio_DeveGerarESalvarCodigo()
        {
            var courseId = Guid.NewGuid();
            var evt = SeedEvent(code: null, courseId: courseId);
            var teacher = new Teacher { Id = Guid.NewGuid(), Courses = new List<Course> { new() { Id = courseId } } };
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(teacher.Id)).ReturnsAsync(teacher);
            _attendanceRepositoryMock
                .Setup(r => r.SetAttendanceCodeAsync(evt, It.IsAny<string>()))
                .Callback<Event, string>((e, c) => e.AttendanceCode = c)
                .Returns(Task.CompletedTask);

            var code = await _attendanceService.GetCodeAsync(evt.Id, teacher.Id);

            Assert.NotNull(code);
            Assert.Equal(6, code.Length);
            _attendanceRepositoryMock.Verify(r => r.SetAttendanceCodeAsync(evt, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetCodeAsync_QuandoSecretariaDaMesmaInstituicaoECodigoJaExistir_DeveRetornarCodigoExistente()
        {
            var institutionId = Guid.NewGuid();
            var evt = SeedEvent(code: "XYZ789", institutionId: institutionId);
            var secretaria = new AcademicDepartment { Id = Guid.NewGuid(), InstitutionId = institutionId };
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(secretaria.Id)).ReturnsAsync(secretaria);

            var code = await _attendanceService.GetCodeAsync(evt.Id, secretaria.Id);

            Assert.Equal("XYZ789", code);
            _attendanceRepositoryMock.Verify(r => r.SetAttendanceCodeAsync(It.IsAny<Event>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmWithCodeAsync_QuandoEventoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var eventId = Guid.NewGuid();
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(eventId)).ReturnsAsync((Event?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _attendanceService.ConfirmWithCodeAsync(eventId, Guid.NewGuid(), "ABC234"));

            Assert.Equal("Evento não encontrado.", exception.Message);
        }

        [Fact]
        public async Task ConfirmWithCodeAsync_QuandoAlunoNaoEstiverInscrito_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, studentId)).ReturnsAsync((EventStudent?)null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _attendanceService.ConfirmWithCodeAsync(evt.Id, studentId, "ABC234"));

            Assert.Equal("Você não está inscrito neste evento.", exception.Message);
        }

        [Fact]
        public async Task ConfirmWithCodeAsync_QuandoInscricaoEstiverCancelada_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var registration = SeedRegistration(evt.Id, status: RegistrationStatus.Canceled);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _attendanceService.ConfirmWithCodeAsync(evt.Id, registration.StudentId, "ABC234"));

            Assert.Equal("Sua inscrição neste evento foi cancelada.", exception.Message);
        }

        [Fact]
        public async Task ConfirmWithCodeAsync_QuandoPresencaJaRegistrada_DeveRetornarSemAlterar()
        {
            var evt = SeedEvent();
            var registration = SeedRegistration(evt.Id, attendedAt: DateTime.UtcNow.AddMinutes(-10));
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            await _attendanceService.ConfirmWithCodeAsync(evt.Id, registration.StudentId, "ABC234");

            _attendanceRepositoryMock.Verify(r => r.SetAttendanceAsync(It.IsAny<EventStudent>(), It.IsAny<DateTime?>(), It.IsAny<AttendanceMethod?>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmWithCodeAsync_QuandoEventoAindaNaoComecou_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(startDate: DateTime.UtcNow.AddHours(1), endDate: DateTime.UtcNow.AddHours(3));
            var registration = SeedRegistration(evt.Id);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _attendanceService.ConfirmWithCodeAsync(evt.Id, registration.StudentId, "ABC234"));

            Assert.Equal("A confirmação de presença só abre quando o evento começar.", exception.Message);
        }

        [Fact]
        public async Task ConfirmWithCodeAsync_QuandoEventoJaTerminou_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(startDate: DateTime.UtcNow.AddHours(-3), endDate: DateTime.UtcNow.AddHours(-1));
            var registration = SeedRegistration(evt.Id);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _attendanceService.ConfirmWithCodeAsync(evt.Id, registration.StudentId, "ABC234"));

            Assert.Equal("O evento já terminou. Procure a organização para registrar sua presença.", exception.Message);
        }

        [Fact]
        public async Task ConfirmWithCodeAsync_QuandoCodigoAindaNaoLiberado_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(code: null);
            var registration = SeedRegistration(evt.Id);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _attendanceService.ConfirmWithCodeAsync(evt.Id, registration.StudentId, "ABC234"));

            Assert.Equal("O código de presença ainda não foi liberado pela organização.", exception.Message);
        }

        [Fact]
        public async Task ConfirmWithCodeAsync_QuandoCodigoInformadoForIncorreto_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(code: "ABC234");
            var registration = SeedRegistration(evt.Id);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _attendanceService.ConfirmWithCodeAsync(evt.Id, registration.StudentId, "ERRADO"));

            Assert.Equal("Código inválido.", exception.Message);
        }

        [Fact]
        public async Task ConfirmWithCodeAsync_QuandoCodigoForValido_DeveRegistrarPresencaPorCodigo()
        {
            var evt = SeedEvent(code: "ABC234");
            var registration = SeedRegistration(evt.Id);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            await _attendanceService.ConfirmWithCodeAsync(evt.Id, registration.StudentId, " abc234 ");

            _attendanceRepositoryMock.Verify(r => r.SetAttendanceAsync(registration, It.IsNotNull<DateTime?>(), AttendanceMethod.Code), Times.Once);
        }

        [Fact]
        public async Task GetRegistrationsAsync_QuandoChamadoPorGestorAutorizado_DeveRetornarInscricoesMapeadas()
        {
            var evt = SeedEvent();
            var admin = SeedAdmin();
            var registration = SeedRegistration(evt.Id, attendedAt: DateTime.UtcNow);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationsAsync(evt.Id)).ReturnsAsync(new[] { registration });

            var result = (await _attendanceService.GetRegistrationsAsync(evt.Id, admin.Id)).ToList();

            Assert.Single(result);
            Assert.Equal(registration.StudentId, result[0].StudentId);
            Assert.True(result[0].Attended);
            Assert.Equal(AttendanceMethod.Code, result[0].Method);
        }

        [Fact]
        public async Task MarkAttendanceAsync_QuandoEventoNaoComecou_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(startDate: DateTime.UtcNow.AddHours(1));
            var admin = SeedAdmin();
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _attendanceService.MarkAttendanceAsync(evt.Id, Guid.NewGuid(), admin.Id));

            Assert.Equal("A presença só pode ser registrada depois do início do evento.", exception.Message);
        }

        [Fact]
        public async Task MarkAttendanceAsync_QuandoPrazoManualDe15DiasExpirou_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(startDate: DateTime.UtcNow.AddDays(-20), endDate: DateTime.UtcNow.AddDays(-16));
            var admin = SeedAdmin();
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _attendanceService.MarkAttendanceAsync(evt.Id, Guid.NewGuid(), admin.Id));

            Assert.Equal("O prazo de 15 dias após o término do evento para registrar presença acabou.", exception.Message);
        }

        [Fact]
        public async Task MarkAttendanceAsync_QuandoInscricaoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var evt = SeedEvent();
            var admin = SeedAdmin();
            var studentId = Guid.NewGuid();
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, studentId)).ReturnsAsync((EventStudent?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _attendanceService.MarkAttendanceAsync(evt.Id, studentId, admin.Id));

            Assert.Equal("Inscrição não encontrada.", exception.Message);
        }

        [Fact]
        public async Task MarkAttendanceAsync_QuandoInscricaoCancelada_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var admin = SeedAdmin();
            var registration = SeedRegistration(evt.Id, status: RegistrationStatus.Canceled);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _attendanceService.MarkAttendanceAsync(evt.Id, registration.StudentId, admin.Id));

            Assert.Equal("A inscrição deste aluno foi cancelada.", exception.Message);
        }

        [Fact]
        public async Task MarkAttendanceAsync_QuandoPresencaJaRegistrada_DeveRetornarSemAlterar()
        {
            var evt = SeedEvent();
            var admin = SeedAdmin();
            var registration = SeedRegistration(evt.Id, attendedAt: DateTime.UtcNow);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            await _attendanceService.MarkAttendanceAsync(evt.Id, registration.StudentId, admin.Id);

            _attendanceRepositoryMock.Verify(r => r.SetAttendanceAsync(It.IsAny<EventStudent>(), It.IsAny<DateTime?>(), It.IsAny<AttendanceMethod?>()), Times.Never);
        }

        [Fact]
        public async Task MarkAttendanceAsync_QuandoValido_DeveRegistrarPresencaManual()
        {
            var evt = SeedEvent();
            var admin = SeedAdmin();
            var registration = SeedRegistration(evt.Id, attendedAt: null);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            await _attendanceService.MarkAttendanceAsync(evt.Id, registration.StudentId, admin.Id);

            _attendanceRepositoryMock.Verify(r => r.SetAttendanceAsync(registration, It.IsNotNull<DateTime?>(), AttendanceMethod.Manual), Times.Once);
        }

        [Fact]
        public async Task UnmarkAttendanceAsync_QuandoPresencaNaoEstiverRegistrada_DeveRetornarSemAlterar()
        {
            var evt = SeedEvent();
            var admin = SeedAdmin();
            var registration = SeedRegistration(evt.Id, attendedAt: null);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            await _attendanceService.UnmarkAttendanceAsync(evt.Id, registration.StudentId, admin.Id);

            _attendanceRepositoryMock.Verify(r => r.SetAttendanceAsync(It.IsAny<EventStudent>(), It.IsAny<DateTime?>(), It.IsAny<AttendanceMethod?>()), Times.Never);
        }

        [Fact]
        public async Task UnmarkAttendanceAsync_QuandoPresencaEstiverRegistrada_DeveRemoverPresenca()
        {
            var evt = SeedEvent();
            var admin = SeedAdmin();
            var registration = SeedRegistration(evt.Id, attendedAt: DateTime.UtcNow);
            _attendanceRepositoryMock.Setup(r => r.GetEventAsync(evt.Id)).ReturnsAsync(evt);
            _attendanceRepositoryMock.Setup(r => r.GetRegistrationAsync(evt.Id, registration.StudentId)).ReturnsAsync(registration);

            await _attendanceService.UnmarkAttendanceAsync(evt.Id, registration.StudentId, admin.Id);

            _attendanceRepositoryMock.Verify(r => r.SetAttendanceAsync(registration, null, null), Times.Once);
        }
    }
}


using API_Gestao_Eventos.src.Application.DTO.Dashboard;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using Moq;
using Xunit;

namespace API_Gestao_Eventos.Tests.Services
{
    public class DashboardServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IDashboardRepository> _dashboardRepositoryMock;
        private readonly DashboardService _dashboardService;

        public DashboardServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _dashboardRepositoryMock = new Mock<IDashboardRepository>();
            _dashboardService = new DashboardService(_userRepositoryMock.Object, _dashboardRepositoryMock.Object);
        }

        private static DashboardFilterDto SeedFilter(DateTime? from = null, DateTime? to = null) => new()
        {
            From = from ?? DateTime.UtcNow.AddMonths(-1),
            To = to ?? DateTime.UtcNow,
            EventId = Guid.Empty,
            CourseId = Guid.Empty,
            TeacherId = Guid.Empty
        };

        [Fact]
        public async Task GetAsync_QuandoUsuarioNaoExistir_DeveLancarKeyNotFoundException()
        {
            var userId = Guid.NewGuid();
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _dashboardService.GetAsync(userId, SeedFilter()));

            Assert.Equal("Usuário não encontrado.", exception.Message);
        }

        [Fact]
        public async Task GetAsync_QuandoUsuarioForAluno_DeveLancarInvalidOperationException()
        {
            var student = new Student { Id = Guid.NewGuid() };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(student.Id)).ReturnsAsync(student);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _dashboardService.GetAsync(student.Id, SeedFilter()));

            Assert.Equal("O painel analítico não está disponível para alunos.", exception.Message);
        }

        [Fact]
        public async Task GetAsync_QuandoDataFinalForAnteriorADataInicial_DeveLancarInvalidOperationException()
        {
            var admin = new Administrator { Id = Guid.NewGuid() };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);
            var filter = SeedFilter(from: DateTime.UtcNow, to: DateTime.UtcNow.AddDays(-5));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _dashboardService.GetAsync(admin.Id, filter));

            Assert.Equal("A data final deve ser posterior à data inicial.", exception.Message);
        }

        [Fact]
        public async Task GetAsync_QuandoPeriodoExceder10Anos_DeveLancarInvalidOperationException()
        {
            var admin = new Administrator { Id = Guid.NewGuid() };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);
            var filter = SeedFilter(from: DateTime.UtcNow.AddDays(-3700), to: DateTime.UtcNow);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _dashboardService.GetAsync(admin.Id, filter));

            Assert.Equal("O período máximo do painel é de 10 anos.", exception.Message);
        }

        [Fact]
        public async Task GetAsync_QuandoSecretariaSemInstituicao_DeveRetornarPainelVazioComMensagem()
        {
            var secretaria = new AcademicDepartment { Id = Guid.NewGuid(), InstitutionId = null };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(secretaria.Id)).ReturnsAsync(secretaria);

            var result = await _dashboardService.GetAsync(secretaria.Id, SeedFilter());

            Assert.Equal("Sua secretaria ainda não está vinculada a uma instituição.", result.ScopeDescription);
            Assert.False(result.CanFilterByTeacher);
            Assert.Equal(5, result.Feedback.Distribution.Count);
            _dashboardRepositoryMock.Verify(r => r.GetAsync(It.IsAny<DashboardQuery>()), Times.Never);
        }

        [Fact]
        public async Task GetAsync_QuandoProfessorSemInstituicao_DeveRetornarPainelVazioComMensagem()
        {
            var professor = new Teacher { Id = Guid.NewGuid(), InstitutionId = null };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(professor.Id)).ReturnsAsync(professor);

            var result = await _dashboardService.GetAsync(professor.Id, SeedFilter());

            Assert.Equal("Seu usuário ainda não está vinculado a uma instituição.", result.ScopeDescription);
            Assert.False(result.CanFilterByTeacher);
            _dashboardRepositoryMock.Verify(r => r.GetAsync(It.IsAny<DashboardQuery>()), Times.Never);
        }

        [Fact]
        public async Task GetAsync_QuandoAdministrador_DeveConsultarRepositorioComEscopoGlobal()
        {
            var admin = new Administrator { Id = Guid.NewGuid() };
            var filter = new DashboardFilterDto
            {
                From = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local),
                To = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified)
            };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);
            _dashboardRepositoryMock.Setup(r => r.GetAsync(It.IsAny<DashboardQuery>())).ReturnsAsync(new DashboardResponseDto());

            var result = await _dashboardService.GetAsync(admin.Id, filter);

            Assert.True(result.CanFilterByTeacher);
            Assert.Equal("Visão de todas as instituições.", result.ScopeDescription);
            _dashboardRepositoryMock.Verify(r => r.GetAsync(It.Is<DashboardQuery>(q => q.InstitutionId == null && !q.RestrictToOwnEvents)), Times.Once);
        }

        [Fact]
        public async Task GetAsync_QuandoSecretariaComInstituicao_DeveConsultarRepositorioRestringindoInstituicao()
        {
            var institutionId = Guid.NewGuid();
            var secretaria = new AcademicDepartment { Id = Guid.NewGuid(), InstitutionId = institutionId };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(secretaria.Id)).ReturnsAsync(secretaria);
            _dashboardRepositoryMock.Setup(r => r.GetAsync(It.IsAny<DashboardQuery>())).ReturnsAsync(new DashboardResponseDto());

            var result = await _dashboardService.GetAsync(secretaria.Id, new DashboardFilterDto());

            Assert.True(result.CanFilterByTeacher);
            Assert.Equal("Visão da sua instituição.", result.ScopeDescription);
            _dashboardRepositoryMock.Verify(r => r.GetAsync(It.Is<DashboardQuery>(q => q.InstitutionId == institutionId)), Times.Once);
        }

        [Fact]
        public async Task GetAsync_QuandoProfessorComInstituicao_DeveConsultarRepositorioComCursosERestricaoDeEventos()
        {
            var institutionId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var professor = new Teacher
            {
                Id = Guid.NewGuid(),
                InstitutionId = institutionId,
                Courses = new List<Course> { new() { Id = courseId } }
            };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(professor.Id)).ReturnsAsync(professor);
            _dashboardRepositoryMock.Setup(r => r.GetAsync(It.IsAny<DashboardQuery>())).ReturnsAsync(new DashboardResponseDto());

            var result = await _dashboardService.GetAsync(professor.Id, SeedFilter());

            Assert.False(result.CanFilterByTeacher);
            Assert.Equal("Visão dos seus cursos e dos eventos que você criou, na sua instituição.", result.ScopeDescription);
            _dashboardRepositoryMock.Verify(r => r.GetAsync(It.Is<DashboardQuery>(q =>
                q.InstitutionId == institutionId &&
                q.RestrictToOwnEvents &&
                q.CreatorUserId == professor.Id &&
                q.TeacherId == null &&
                q.CourseIds != null &&
                q.CourseIds.Contains(courseId))), Times.Once);
        }
    }
}


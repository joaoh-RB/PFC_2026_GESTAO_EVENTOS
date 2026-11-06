using API_Gestao_Eventos.src.Application.DTO.Course;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using Moq;
using Xunit;

namespace API_Gestao_Eventos.Tests.Services
{
    public class CourseServiceTests
    {
        private readonly Mock<ICourseRepository> _courseRepositoryMock;
        private readonly CourseService _courseService;

        public CourseServiceTests()
        {
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _courseService = new CourseService(_courseRepositoryMock.Object);
        }

        private static Course SeedCourse(Guid? id = null, Guid? institutionId = null, string name = "Engenharia de Software", bool isActive = true) => new()
        {
            Id = id ?? Guid.NewGuid(),
            InstitutionId = institutionId ?? Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        private static CreateCourseDto SeedCreateDto(Guid? institutionId = null, string name = "Engenharia de Software") => new()
        {
            Name = name,
            InstitutionId = institutionId ?? Guid.NewGuid()
        };

        [Fact]
        public async Task GetCoursesForSelectAsync_QuandoInstitutionIdForNuloOuVazio_DeveRetornarListaVazia()
        {
            var resultNull = await _courseService.GetCoursesForSelectAsync(null);
            var resultEmpty = await _courseService.GetCoursesForSelectAsync(Guid.Empty);

            Assert.Empty(resultNull);
            Assert.Empty(resultEmpty);
            _courseRepositoryMock.Verify(r => r.GetByInstitutionAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetCoursesForSelectAsync_QuandoInstitutionIdForValido_DeveRetornarItensMapeados()
        {
            var institutionId = Guid.NewGuid();
            var course = SeedCourse(institutionId: institutionId);
            _courseRepositoryMock.Setup(r => r.GetByInstitutionAsync(institutionId)).ReturnsAsync(new[] { course });

            var result = (await _courseService.GetCoursesForSelectAsync(institutionId)).ToList();

            Assert.Single(result);
            Assert.Equal(course.Id.ToString(), result[0].Value);
            Assert.Equal(course.Name, result[0].Label);
        }

        [Fact]
        public async Task CreateCourseAsync_QuandoDadosValidos_DeveCadastrarERetornarId()
        {
            var dto = SeedCreateDto();
            _courseRepositoryMock.Setup(r => r.ExistsByNameAsync(dto.Name, dto.InstitutionId)).ReturnsAsync(false);

            var result = await _courseService.CreateCourseAsync(dto);

            Assert.NotEqual(Guid.Empty, result);
            _courseRepositoryMock.Verify(r => r.AddAsync(It.Is<Course>(c => c.Name == dto.Name && c.InstitutionId == dto.InstitutionId)), Times.Once);
        }

        [Fact]
        public async Task CreateCourseAsync_QuandoNomeJaExisteNaInstituicao_DeveLancarInvalidOperationException()
        {
            var dto = SeedCreateDto();
            _courseRepositoryMock.Setup(r => r.ExistsByNameAsync(dto.Name, dto.InstitutionId)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _courseService.CreateCourseAsync(dto));

            Assert.Equal("Curso já cadastrado nesta instituição.", exception.Message);
            _courseRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Course>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_QuandoCursoNaoExistir_DeveRetornarNulo()
        {
            var id = Guid.NewGuid();
            _courseRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Course?)null);

            var result = await _courseService.GetByIdAsync(id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_QuandoCursoExistir_DeveRetornarDetalhesDoCurso()
        {
            var course = SeedCourse();
            _courseRepositoryMock.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);

            var result = await _courseService.GetByIdAsync(course.Id);

            Assert.NotNull(result);
            Assert.Equal(course.Id, result.Id);
            Assert.Equal(course.Name, result.Name);
            Assert.Equal(course.InstitutionId, result.InstitutionId);
            Assert.Equal(course.IsActive, result.IsActive);
        }

        [Fact]
        public async Task UpdateCourseAsync_QuandoCursoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var id = Guid.NewGuid();
            _courseRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Course?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _courseService.UpdateCourseAsync(id, new UpdateCourseDto { Name = "Novo Nome" }));

            Assert.Equal("Curso não encontrado.", exception.Message);
        }

        [Fact]
        public async Task UpdateCourseAsync_QuandoOutroCursoComMesmoNomeExistir_DeveLancarInvalidOperationException()
        {
            var course = SeedCourse();
            _courseRepositoryMock.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _courseRepositoryMock.Setup(r => r.ExistsByNameExceptAsync("Duplicado", course.InstitutionId, course.Id)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _courseService.UpdateCourseAsync(course.Id, new UpdateCourseDto { Name = "Duplicado" }));

            Assert.Equal("Já existe outro curso com este nome nesta instituição.", exception.Message);
            _courseRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Course>()), Times.Never);
        }

        [Fact]
        public async Task UpdateCourseAsync_QuandoDadosForemValidos_DeveAtualizarCurso()
        {
            var course = SeedCourse();
            _courseRepositoryMock.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _courseRepositoryMock.Setup(r => r.ExistsByNameExceptAsync("Curso Atualizado", course.InstitutionId, course.Id)).ReturnsAsync(false);

            await _courseService.UpdateCourseAsync(course.Id, new UpdateCourseDto { Name = "Curso Atualizado" });

            Assert.Equal("Curso Atualizado", course.Name);
            _courseRepositoryMock.Verify(r => r.UpdateAsync(course), Times.Once);
        }

        [Fact]
        public async Task GetForManagementAsync_QuandoChamado_DeveRetornarCursosDaInstituicao()
        {
            var institutionId = Guid.NewGuid();
            var course = SeedCourse(institutionId: institutionId);
            _courseRepositoryMock.Setup(r => r.GetByInstitutionForManagementAsync(institutionId)).ReturnsAsync(new[] { course });

            var result = (await _courseService.GetForManagementAsync(institutionId)).ToList();

            Assert.Single(result);
            Assert.Equal(course.Id, result[0].Id);
            Assert.Equal(course.Name, result[0].Name);
            Assert.Equal(course.IsActive, result[0].IsActive);
        }

        [Fact]
        public async Task SetCourseActiveAsync_QuandoCursoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var id = Guid.NewGuid();
            _courseRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Course?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _courseService.SetCourseActiveAsync(id, false));

            Assert.Equal("Curso não encontrado.", exception.Message);
        }

        [Fact]
        public async Task SetCourseActiveAsync_QuandoInativarCursoComDependencias_DeveLancarInvalidOperationException()
        {
            var course = SeedCourse();
            _courseRepositoryMock.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _courseRepositoryMock.Setup(r => r.HasDependenciesAsync(course.Id)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _courseService.SetCourseActiveAsync(course.Id, false));

            Assert.Equal("Não é possível inativar: existem alunos, professores ou eventos vinculados a este curso.", exception.Message);
            _courseRepositoryMock.Verify(r => r.SetActiveAsync(It.IsAny<Course>(), It.IsAny<bool>()), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetCourseActiveAsync_QuandoSemImpedimentos_DeveAlterarStatusAtivo(bool isActive)
        {
            var course = SeedCourse();
            _courseRepositoryMock.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _courseRepositoryMock.Setup(r => r.HasDependenciesAsync(course.Id)).ReturnsAsync(false);

            await _courseService.SetCourseActiveAsync(course.Id, isActive);

            _courseRepositoryMock.Verify(r => r.SetActiveAsync(course, isActive), Times.Once);
        }
    }
}

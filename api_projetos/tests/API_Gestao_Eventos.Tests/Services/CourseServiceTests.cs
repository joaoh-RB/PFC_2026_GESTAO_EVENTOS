using API_Gestao_Eventos.src.Application.DTO.Course;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain;
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

        // Cenário 1: Caminho feliz com verificação de interação mockada (Times.Once)
        [Fact]
        public async Task CreateCourseAsync_QuandoDadosValidos_DeveCadastrarComSucessoEChamarAddAsync()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var dto = new CreateCourseDto
            {
                Name = "Engenharia de Software",
                InstitutionId = institutionId
            };

            _courseRepositoryMock
                .Setup(r => r.ExistsByNameAsync(dto.Name, dto.InstitutionId))
                .ReturnsAsync(false);

            _courseRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Course>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _courseService.CreateCourseAsync(dto);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
            _courseRepositoryMock.Verify(r => r.AddAsync(It.Is<Course>(c =>
                c.Name == dto.Name && c.InstitutionId == dto.InstitutionId)), Times.Once);
        }

        // Cenário 2: Violação de regra/erro com teste de exceção (tipo e mensagem) e mock verify (Times.Never)
        [Fact]
        public async Task CreateCourseAsync_QuandoNomeJaExisteNaInstituicao_DeveLancarInvalidOperationExceptionENaoChamarAddAsync()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var dto = new CreateCourseDto
            {
                Name = "Engenharia de Software",
                InstitutionId = institutionId
            };

            _courseRepositoryMock
                .Setup(r => r.ExistsByNameAsync(dto.Name, dto.InstitutionId))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _courseService.CreateCourseAsync(dto));

            Assert.Equal("Curso já cadastrado nesta instituição.", exception.Message);
            _courseRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Course>()), Times.Never);
        }

        // Cenário 3: Caso-limite com verificação de interação mockada (Times.Never)
        [Fact]
        public async Task GetCoursesForSelectAsync_QuandoInstitutionIdForVazioOuNulo_DeveRetornarListaVaziaSemConsultarRepositorio()
        {
            // Arrange
            Guid? emptyInstitutionId = Guid.Empty;

            // Act
            var result = await _courseService.GetCoursesForSelectAsync(emptyInstitutionId);

            // Assert
            Assert.Empty(result);
            _courseRepositoryMock.Verify(r => r.GetByInstitutionAsync(It.IsAny<Guid>()), Times.Never);
        }
        // Cenário 10: Teste parametrizado cobrindo cálculo e validação
        [Theory]
        [InlineData("Engenharia de Software", true)]
        [InlineData("Matemáica", false)]
        [InlineData("Medicina", true)]
        public async Task CreateCourseAsync_QuandoNomesJaExistiremNaInstituicao_DeveLancarInvalidOperationExceptionENaoChamarAddAsync(string nomeCurso, bool cursoExiste)
        {
            //Arrange
            var institutionId = Guid.NewGuid();
            var dto = new CreateCourseDto() { InstitutionId = institutionId, Name = nomeCurso };
            _courseRepositoryMock.Setup(r => r.ExistsByNameAsync(dto.Name, dto.InstitutionId)).ReturnsAsync(cursoExiste);
            _courseRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Course>())).Returns(Task.CompletedTask);

            // Act
            if (cursoExiste)
            {
                // Assert
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _courseService.CreateCourseAsync(dto));

                Assert.Equal(
                    "Curso já cadastrado nesta instituição.",
                    exception.Message);

                _courseRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<Course>()),
                    Times.Never);
            }
            else
            {
                // Assert
                var result = await _courseService.CreateCourseAsync(dto);

                Assert.NotEqual(Guid.Empty, result);

                _courseRepositoryMock.Verify(
                    r => r.AddAsync(It.Is<Course>(c =>
                        c.Name == dto.Name &&
                        c.InstitutionId == dto.InstitutionId)),
                    Times.Once);
            }
        }
    }
}

using API_Gestao_Eventos.src.Application.DTO.Course;
using API_Gestao_Eventos.src.Application.Validators.Course;
using Xunit;

namespace API_Gestao_Eventos.Tests.Validators
{
    public class CreateCourseValidatorTests
    {
        private readonly CreateCourseValidator _validator;

        public CreateCourseValidatorTests()
        {
            _validator = new CreateCourseValidator();
        }

        // Cenário 4: Caminho feliz
        [Fact]
        public void Validate_QuandoDadosForemValidos_DevePassarNaValidacao()
        {
            // Arrange
            var dto = new CreateCourseDto
            {
                Name = "Ciência da Computação",
                InstitutionId = Guid.NewGuid()
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        // Cenário 5: Violação de regra/erro
        [Fact]
        public void Validate_QuandoNomeForVazio_DeveFalharComMensagemObrigatoria()
        {
            // Arrange
            var dto = new CreateCourseDto
            {
                Name = string.Empty,
                InstitutionId = Guid.NewGuid()
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O nome do curso é obrigatório.");
        }

        // Cenário 6: Caso-limite (comprimento exatamente no limite de 150 caracteres)
        [Fact]
        public void Validate_QuandoNomeTiverExatamente150Caracteres_DevePassarNaValidacao()
        {
            // Arrange
            var dto = new CreateCourseDto
            {
                Name = new string('A', 150),
                InstitutionId = Guid.NewGuid()
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }
    }
}

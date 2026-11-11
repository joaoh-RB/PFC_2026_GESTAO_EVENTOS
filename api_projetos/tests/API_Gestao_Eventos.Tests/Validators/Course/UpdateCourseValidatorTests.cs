using API_Gestao_Eventos.src.Application.DTO.Course;
using API_Gestao_Eventos.src.Application.Validators.Course;
using Xunit;

namespace API_Gestao_Eventos.Tests.Validators.Course
{
    public class UpdateCourseValidatorTests
    {
        private readonly UpdateCourseValidator _validator;

        public UpdateCourseValidatorTests()
        {
            _validator = new UpdateCourseValidator();
        }

        [Fact]
        public void Validate_QuandoDadosForemValidos_DevePassarNaValidacao()
        {
            var dto = new UpdateCourseDto
            {
                Name = "Engenharia da Computação"
            };

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoNomeForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = new UpdateCourseDto
            {
                Name = string.Empty
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O nome do curso é obrigatório.");
        }

        [Fact]
        public void Validate_QuandoNomeTiverExatamente150Caracteres_DevePassarNaValidacao()
        {
            var dto = new UpdateCourseDto
            {
                Name = new string('A', 150)
            };

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoNomeExceder150Caracteres_DeveFalharNaValidacao()
        {
            var dto = new UpdateCourseDto
            {
                Name = new string('A', 151)
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCourseDto.Name));
        }
    }
}

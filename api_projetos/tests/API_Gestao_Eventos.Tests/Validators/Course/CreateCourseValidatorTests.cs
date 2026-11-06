using API_Gestao_Eventos.src.Application.DTO.Course;
using API_Gestao_Eventos.src.Application.Validators.Course;
using Xunit;

namespace API_Gestao_Eventos.Tests.Validators.Course
{
    public class CreateCourseValidatorTests
    {
        private readonly CreateCourseValidator _validator;

        public CreateCourseValidatorTests()
        {
            _validator = new CreateCourseValidator();
        }

        [Fact]
        public void Validate_QuandoDadosForemValidos_DevePassarNaValidacao()
        {
            var dto = new CreateCourseDto
            {
                Name = "Ciência da Computação",
                InstitutionId = Guid.NewGuid()
            };

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoNomeForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = new CreateCourseDto
            {
                Name = string.Empty,
                InstitutionId = Guid.NewGuid()
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O nome do curso é obrigatório.");
        }

        [Fact]
        public void Validate_QuandoNomeTiverExatamente150Caracteres_DevePassarNaValidacao()
        {
            var dto = new CreateCourseDto
            {
                Name = new string('A', 150),
                InstitutionId = Guid.NewGuid()
            };

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoNomeExceder150Caracteres_DeveFalharNaValidacao()
        {
            var dto = new CreateCourseDto
            {
                Name = new string('A', 151),
                InstitutionId = Guid.NewGuid()
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCourseDto.Name));
        }

        [Fact]
        public void Validate_QuandoInstitutionIdForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = new CreateCourseDto
            {
                Name = "Ciência da Computação",
                InstitutionId = Guid.Empty
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A instituição é obrigatória.");
        }
    }
}

using API_Gestao_Eventos.src.Application.DTO.User;
using API_Gestao_Eventos.src.Application.Validators.User;

namespace API_Gestao_Eventos.Tests.Validators.User
{
    public class UpdateStudentValidatorTests
    {
        private readonly UpdateStudentValidator _validator;

        public UpdateStudentValidatorTests()
        {
            _validator = new UpdateStudentValidator();
        }

        private static UpdateStudentRequestDto CreateValidDto() => new()
        {
            Name = "X X Atualizada",
            Email = "x.atualizada@teste.com",
            UniqueIdentifier = "RGM654321",
            InstitutionId = Guid.NewGuid(),
            CourseId = Guid.NewGuid()
        };

        [Fact]
        public void Validate_QuandoDadosForemValidos_DevePassarNaValidacao()
        {
            var dto = CreateValidDto();

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoNomeForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.Name = string.Empty;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O nome é obrigatório.");
        }

        [Fact]
        public void Validate_QuandoEmailForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.Email = string.Empty;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O email é obrigatório.");
        }

        [Fact]
        public void Validate_QuandoEmailForInvalido_DeveFalharComMensagemDeFormato()
        {
            var dto = CreateValidDto();
            dto.Email = "email-invalido";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O email deve ser válido.");
        }

        [Fact]
        public void Validate_QuandoUniqueIdentifierForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.UniqueIdentifier = string.Empty;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O número de matrícula é obrigatório.");
        }

        [Fact]
        public void Validate_QuandoInstitutionIdForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.InstitutionId = Guid.Empty;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A instituição é obrigatória.");
        }

        [Fact]
        public void Validate_QuandoCourseIdForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.CourseId = Guid.Empty;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O curso é obrigatório.");
        }
    }
}

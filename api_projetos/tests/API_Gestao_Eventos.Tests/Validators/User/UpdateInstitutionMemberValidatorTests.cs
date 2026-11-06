using API_Gestao_Eventos.src.Application.DTO.User;
using API_Gestao_Eventos.src.Application.Validators.User;
using Xunit;

namespace API_Gestao_Eventos.Tests.Validators.User
{
    public class UpdateInstitutionMemberValidatorTests
    {
        private readonly UpdateInstitutionMemberValidator _validator;

        public UpdateInstitutionMemberValidatorTests()
        {
            _validator = new UpdateInstitutionMemberValidator();
        }

        private static UpdateInstitutionMemberRequestDto CreateValidDto() => new()
        {
            Name = "X Professor Atualizado",
            Email = "professor.atualizado@teste.com",
            Courses = new List<Guid> { Guid.NewGuid() }
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
    }
}

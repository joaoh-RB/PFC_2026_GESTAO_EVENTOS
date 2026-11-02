using API_Gestao_Eventos.src.Application.DTO.Auth;
using API_Gestao_Eventos.src.Application.Validators.Auth;
using Xunit;

namespace API_Gestao_Eventos.Tests.Validators.Auth
{
    public class RegisterRequestValidatorTests
    {
        private readonly RegisterRequestValidator _validator;

        public RegisterRequestValidatorTests()
        {
            _validator = new RegisterRequestValidator();
        }

        private static RegisterRequestDto CreateValidDto() => new()
        {
            Name = "João da Silva",
            Email = "joao@teste.com",
            Password = "Senha@123",
            InstitutionId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            UniqueIdentifier = "12345678"
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
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O e-mail é obrigatório.");
        }

        [Fact]
        public void Validate_QuandoEmailForInvalido_DeveFalharComMensagemDeFormato()
        {
            var dto = CreateValidDto();
            dto.Email = "email-invalido";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O e-mail deve ser válido.");
        }

        [Fact]
        public void Validate_QuandoSenhaForVazia_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.Password = string.Empty;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A senha é obrigatória.");
        }

        [Fact]
        public void Validate_QuandoSenhaTiverMenosDe8Caracteres_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.Password = "Ab1@abc";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A senha deve ter no mínimo 8 caracteres.");
        }

        [Fact]
        public void Validate_QuandoSenhaTiverExatamente8CaracteresValidos_DevePassarNaValidacao()
        {
            var dto = CreateValidDto();
            dto.Password = "Ab1@abcd";

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoSenhaNaoTiverLetraMaiuscula_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.Password = "senha@123";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A senha deve conter ao menos uma letra maiúscula.");
        }

        [Fact]
        public void Validate_QuandoSenhaNaoTiverNumero_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.Password = "Senha@abc";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A senha deve conter ao menos um número.");
        }

        [Fact]
        public void Validate_QuandoSenhaNaoTiverCaractereEspecial_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.Password = "Senha1234";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A senha deve conter ao menos um caractere especial.");
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

        [Fact]
        public void Validate_QuandoUniqueIdentifierForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.UniqueIdentifier = string.Empty;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O RGM é obrigatório.");
        }
    }
}

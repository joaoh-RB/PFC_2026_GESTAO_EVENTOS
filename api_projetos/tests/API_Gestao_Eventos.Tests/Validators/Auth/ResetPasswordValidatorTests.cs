using API_Gestao_Eventos.src.Application.DTO.Auth;
using API_Gestao_Eventos.src.Application.Validators.Auth;

namespace API_Gestao_Eventos.Tests.Validators.Auth
{
    public class ResetPasswordValidatorTests
    {
        private readonly ResetPasswordValidator _validator;

        public ResetPasswordValidatorTests()
        {
            _validator = new ResetPasswordValidator();
        }

        private static ResetPasswordRequestDto CreateValidDto() => new()
        {
            Email = "usuario@teste.com",
            Token = "token-valido-123",
            NewPassword = "NovaSenha@123",
            ConfirmPassword = "NovaSenha@123"
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
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O e-mail informado não é válido.");
        }

        [Fact]
        public void Validate_QuandoNovaSenhaForVazia_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.NewPassword = string.Empty;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A senha é obrigatória.");
        }

        [Fact]
        public void Validate_QuandoNovaSenhaTiverMenosDe8Caracteres_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.NewPassword = "Ab1@abc";
            dto.ConfirmPassword = "Ab1@abc";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A senha deve ter no mínimo 8 caracteres.");
        }

        [Fact]
        public void Validate_QuandoNovaSenhaTiverExatamente8CaracteresValidos_DevePassarNaValidacao()
        {
            var dto = CreateValidDto();
            dto.NewPassword = "Ab1@abcd";
            dto.ConfirmPassword = "Ab1@abcd";

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoNovaSenhaNaoTiverLetraMaiuscula_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.NewPassword = "novasenha@123";
            dto.ConfirmPassword = "novasenha@123";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A senha deve conter ao menos uma letra maiúscula.");
        }

        [Fact]
        public void Validate_QuandoNovaSenhaNaoTiverNumero_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.NewPassword = "NovaSenha@abc";
            dto.ConfirmPassword = "NovaSenha@abc";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A senha deve conter ao menos um número.");
        }

        [Fact]
        public void Validate_QuandoNovaSenhaNaoTiverCaractereEspecial_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.NewPassword = "NovaSenha123";
            dto.ConfirmPassword = "NovaSenha123";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A senha deve conter ao menos um caractere especial.");
        }

        [Fact]
        public void Validate_QuandoConfirmacaoDeSenhaForVazia_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.ConfirmPassword = string.Empty;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A confirmação da nova senha é obrigatória.");
        }

        [Fact]
        public void Validate_QuandoConfirmacaoDeSenhaForDiferenteDaNovaSenha_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.ConfirmPassword = "OutraSenha@123";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A confirmação da nova senha não confere com a nova senha.");
        }

        [Fact]
        public void Validate_QuandoTokenForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.Token = string.Empty;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O token de redefinição de senha é obrigatório.");
        }
    }
}

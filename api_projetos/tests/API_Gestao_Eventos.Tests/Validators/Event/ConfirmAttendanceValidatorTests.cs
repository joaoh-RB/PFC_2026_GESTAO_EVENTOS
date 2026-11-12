using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.Validators.Event;

namespace API_Gestao_Eventos.Tests.Validators.Event
{
    public class ConfirmAttendanceValidatorTests
    {
        private readonly ConfirmAttendanceValidator _validator;

        public ConfirmAttendanceValidatorTests()
        {
            _validator = new ConfirmAttendanceValidator();
        }

        [Fact]
        public void Validate_QuandoCodigoTiver6Caracteres_DevePassarNaValidacao()
        {
            var dto = new ConfirmAttendanceRequestDto
            {
                Code = "ABC123"
            };

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoCodigoForVazio_DeveFalharApenasComMensagemObrigatoria()
        {
            var dto = new ConfirmAttendanceRequestDto
            {
                Code = string.Empty
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Informe o código de presença.", result.Errors[0].ErrorMessage);
        }

        [Fact]
        public void Validate_QuandoCodigoNaoTiver6Caracteres_DeveFalharComMensagemDeTamanho()
        {
            var dto = new ConfirmAttendanceRequestDto
            {
                Code = "ABC12"
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O código de presença deve ter 6 caracteres.");
        }
    }
}

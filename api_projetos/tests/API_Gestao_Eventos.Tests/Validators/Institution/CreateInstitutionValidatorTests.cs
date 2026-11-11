using API_Gestao_Eventos.src.Application.DTO.Institution;
using API_Gestao_Eventos.src.Application.Validators.Institution;
using Xunit;

namespace API_Gestao_Eventos.Tests.Validators.Institution
{
    public class CreateInstitutionValidatorTests
    {
        private readonly CreateInstitutionValidator _validator;
        private CreateInstitutionDto CreateValidDto()
        {
            return new CreateInstitutionDto
            {
                Name = "Universidade Federal de Teste",
                Cnpj = "11.222.333/0001-81",
                Address = "Rua das Flores, 123",
                Phone = "(11) 99999-9999"
            };
        }
        public CreateInstitutionValidatorTests()
        {
            _validator = new CreateInstitutionValidator();
        }

        [Fact]
        public void Validate_QuandoDadosForemValidosComCnpjValido_DevePassarNaValidacao()
        {
            var dto = CreateValidDto();

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_QuandoCnpjForNuloOuEmBranco_DeveIgnorarRegraDeCnpjEPassarNaValidacao(string? cnpj)
        {

            var dto = CreateValidDto();
            dto.Cnpj = cnpj;

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoNomeForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.Name = "";

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O nome da instituição é obrigatório.");
        }

        [Fact]
        public void Validate_QuandoNomeTiverExatamente150Caracteres_DevePassarNaValidacao()
        {
            var dto = CreateValidDto();
            dto.Name = new string('a', 150);
            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoNomeExceder150Caracteres_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.Name = new string('a', 151);

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateInstitutionDto.Name));
        }

        [Theory]
        [InlineData("11.222.333/0001-00")]
        [InlineData("123456")]
        public void Validate_QuandoCnpjForInvalido_DeveFalharComMensagemDeErro(string invalidCnpj)
        {
            var dto = CreateValidDto();
            dto.Cnpj = invalidCnpj;
            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "CNPJ inválido.");
        }

        [Fact]
        public void Validate_QuandoAddressTiverExatamente250Caracteres_DevePassarNaValidacao()
        {
            var dto = CreateValidDto();
            dto.Address = new string('A', 250);

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoAddressExceder250Caracteres_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.Address = new string('A', 251);

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateInstitutionDto.Address));
        }

        [Fact]
        public void Validate_QuandoPhoneTiverExatamente20Caracteres_DevePassarNaValidacao()
        {
            var dto = CreateValidDto();
            dto.Phone = new string('9', 20);

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoPhoneExceder20Caracteres_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.Phone = new string('9', 21);

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateInstitutionDto.Phone));
        }
    }
}

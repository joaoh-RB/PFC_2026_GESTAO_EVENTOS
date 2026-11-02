using API_Gestao_Eventos.src.Application.DTO.Group;
using API_Gestao_Eventos.src.Application.Validators.Group;
using Xunit;

namespace API_Gestao_Eventos.Tests.Validators.Group
{
    public class CreateGroupValidatorTests
    {
        private readonly CreateGroupValidator _validator;

        public CreateGroupValidatorTests()
        {
            _validator = new CreateGroupValidator();
        }

        [Fact]
        public void Validate_QuandoDadosForemValidos_DevePassarNaValidacao()
        {
            var dto = new CreateGroupRequestDto
            {
                EventId = Guid.NewGuid(),
                StudentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
            };

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoListaDeAlunosEstiverVaziaEEventIdValido_DevePassarNaValidacao()
        {
            var dto = new CreateGroupRequestDto
            {
                EventId = Guid.NewGuid(),
                StudentIds = new List<Guid>()
            };

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoEventIdForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = new CreateGroupRequestDto
            {
                EventId = Guid.Empty,
                StudentIds = new List<Guid> { Guid.NewGuid() }
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O evento é obrigatório.");
        }

        [Fact]
        public void Validate_QuandoStudentIdsForNulo_DeveFalharComMensagemObrigatoria()
        {
            var dto = new CreateGroupRequestDto
            {
                EventId = Guid.NewGuid(),
                StudentIds = null!
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("A lista de alunos é obrigatória.", result.Errors[0].ErrorMessage);
        }

        [Fact]
        public void Validate_QuandoHouverAlunosRepetidosNaLista_DeveFalhar()
        {
            var duplicateStudentId = Guid.NewGuid();
            var dto = new CreateGroupRequestDto
            {
                EventId = Guid.NewGuid(),
                StudentIds = new List<Guid> { duplicateStudentId, duplicateStudentId }
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "Há alunos repetidos na lista de convidados.");
        }
    }
}

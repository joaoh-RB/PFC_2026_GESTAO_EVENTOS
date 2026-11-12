using API_Gestao_Eventos.src.Application.DTO.Group;
using API_Gestao_Eventos.src.Application.Validators.Group;

namespace API_Gestao_Eventos.Tests.Validators.Group
{
    public class InviteGroupMembersValidatorTests
    {
        private readonly InviteGroupMembersValidator _validator;

        public InviteGroupMembersValidatorTests()
        {
            _validator = new InviteGroupMembersValidator();
        }

        [Fact]
        public void Validate_QuandoDadosForemValidos_DevePassarNaValidacao()
        {
            var dto = new InviteGroupMembersRequestDto
            {
                StudentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
            };

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoListaDeAlunosEstiverVazia_DeveFalharComMensagemObrigatoria()
        {
            var dto = new InviteGroupMembersRequestDto
            {
                StudentIds = new List<Guid>()
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Selecione ao menos um aluno para convidar.", result.Errors[0].ErrorMessage);
        }

        [Fact]
        public void Validate_QuandoListaDeAlunosForNula_DeveFalharComMensagemObrigatoria()
        {
            var dto = new InviteGroupMembersRequestDto
            {
                StudentIds = null!
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Selecione ao menos um aluno para convidar.", result.Errors[0].ErrorMessage);
        }

        [Fact]
        public void Validate_QuandoHouverAlunosRepetidosNaLista_DeveFalhar()
        {
            var duplicateStudentId = Guid.NewGuid();
            var dto = new InviteGroupMembersRequestDto
            {
                StudentIds = new List<Guid> { duplicateStudentId, duplicateStudentId }
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "Há alunos repetidos na lista de convidados.");
        }
    }
}

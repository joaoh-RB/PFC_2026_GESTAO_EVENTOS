using API_Gestao_Eventos.src.Application.DTO.User;
using API_Gestao_Eventos.src.Application.Validators.User;
using API_Gestao_Eventos.src.Domain.Enums;
using Xunit;

namespace API_Gestao_Eventos.Tests.Validators.User
{
    public class CreateInstitutionMemberValidatorTests
    {
        private readonly CreateInstitutionMemberValidator _validator;

        public CreateInstitutionMemberValidatorTests()
        {
            _validator = new CreateInstitutionMemberValidator();
        }

        private static CreateInstitutionMemberRequestDto CreateValidDto() => new()
        {
            Name = "Carlos Professor",
            Email = "carlos@teste.com",
            InstitutionId = Guid.NewGuid(),
            UserRole = UserRole.Professor,
            Courses = new List<Guid> { Guid.NewGuid() }
        };

        [Fact]
        public void Validate_QuandoDadosForemValidosParaProfessorComCursos_DevePassarNaValidacao()
        {
            var dto = CreateValidDto();

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData(UserRole.Secretaria)]
        [InlineData(UserRole.Administrador)]
        public void Validate_QuandoCargoNaoForProfessorESemCursos_DevePassarNaValidacao(UserRole role)
        {
            var dto = CreateValidDto();
            dto.UserRole = role;
            dto.Courses = null;

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
        public void Validate_QuandoInstitutionIdForVazio_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.InstitutionId = Guid.Empty;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A instituição é obrigatória.");
        }

        [Fact]
        public void Validate_QuandoUserRoleForInvalido_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.UserRole = (UserRole)999;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O cargo deve ser válido.");
        }

        [Fact]
        public void Validate_QuandoUserRoleForProfessorECursosEstiverVazio_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.UserRole = UserRole.Professor;
            dto.Courses = new List<Guid>();

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "Selecione ao menos um curso.");
        }

        [Fact]
        public void Validate_QuandoUserRoleForProfessorECursosForNulo_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.UserRole = UserRole.Professor;
            dto.Courses = null;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "Selecione ao menos um curso.");
        }
    }
}

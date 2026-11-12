using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.Validators.Event;
using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.Tests.Validators.Event
{
    public class CreateEventValidatorTests
    {
        private readonly CreateEventValidator _validator;

        public CreateEventValidatorTests()
        {
            _validator = new CreateEventValidator();
        }

        private static CreateEventRequestDto CreateValidDto()
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(2);
            return new CreateEventRequestDto
            {
                Name = "Semana de Tecnologia",
                Description = "Evento anual de tecnologia e inovação.",
                InstitutionId = Guid.NewGuid(),
                StartDate = startDate,
                EndDate = startDate.AddDays(1),
                Capacity = 100,
                AllowDocuments = true,
                MaxGroupSize = 5,
                SubmissionDeadline = startDate.AddHours(12),
                EventType = EventType.Palestra,
                AllowedCourses = new List<Guid> { Guid.NewGuid() }
            };
        }

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
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O nome do evento é obrigatório.");
        }

        [Fact]
        public void Validate_QuandoNomeTiverExatamente100Caracteres_DevePassarNaValidacao()
        {
            var dto = CreateValidDto();
            dto.Name = new string('A', 100);

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoNomeExceder100Caracteres_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.Name = new string('A', 101);

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O nome do evento não pode exceder 100 caracteres.");
        }

        [Fact]
        public void Validate_QuandoDescricaoTiverExatamente300Caracteres_DevePassarNaValidacao()
        {
            var dto = CreateValidDto();
            dto.Description = new string('A', 300);

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoDescricaoExceder300Caracteres_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.Description = new string('A', 301);

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A descrição do evento não pode exceder 300 caracteres.");
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
        public void Validate_QuandoStartDateForVazia_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.StartDate = default;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A data de início é obrigatória.");
        }

        [Fact]
        public void Validate_QuandoStartDateForNoPassado_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.StartDate = DateTimeOffset.UtcNow.AddDays(-1);

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A data de início deve ser futura.");
        }

        [Fact]
        public void Validate_QuandoStartDateForMaiorOuIgualAEndDate_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.EndDate = dto.StartDate.AddHours(-1);

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A data de início deve ser anterior à data de término.");
        }

        [Fact]
        public void Validate_QuandoEndDateForVazia_DeveFalharComMensagemObrigatoria()
        {
            var dto = CreateValidDto();
            dto.EndDate = default;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A data de término é obrigatória.");
        }

        [Fact]
        public void Validate_QuandoCapacidadeForZero_DeveFalharComMensagensDeCapacidade()
        {
            var dto = CreateValidDto();
            dto.Capacity = 0;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A capacidade do evento deve ser maior que zero.");
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A capacidade do evento é obrigatória.");
        }

        [Fact]
        public void Validate_QuandoCapacidadeForNegativa_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.Capacity = -10;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A capacidade do evento deve ser maior que zero.");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void Validate_QuandoMaxGroupSizeEstiverNosLimites1E10_DevePassarNaValidacao(int maxGroupSize)
        {
            var dto = CreateValidDto();
            dto.MaxGroupSize = maxGroupSize;

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(11)]
        public void Validate_QuandoMaxGroupSizeEstiverForaDoIntervalo1A10_DeveFalhar(int maxGroupSize)
        {
            var dto = CreateValidDto();
            dto.MaxGroupSize = maxGroupSize;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O limite de alunos por grupo deve estar entre 1 e 10.");
        }

        [Fact]
        public void Validate_QuandoSubmissionDeadlineForAnteriorOuIgualAoStartDateComAllowDocumentsTrue_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.AllowDocuments = true;
            dto.SubmissionDeadline = dto.StartDate;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O prazo de entrega deve ser posterior ao início do evento.");
        }

        [Fact]
        public void Validate_QuandoAllowDocumentsForFalseESubmissionDeadlineAnteriorAoStartDate_DevePassarNaValidacao()
        {
            var dto = CreateValidDto();
            dto.AllowDocuments = false;
            dto.SubmissionDeadline = dto.StartDate.AddDays(-1);

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoEventTypeForInvalido_DeveFalhar()
        {
            var dto = CreateValidDto();
            dto.EventType = (EventType)999;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O tipo de evento é obrigatório.");
        }
    }
}

using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.Validators.Event;

namespace API_Gestao_Eventos.Tests.Validators.Event
{
    public class CreateEventFeedbackValidatorTests
    {
        private readonly CreateEventFeedbackValidator _validator;

        public CreateEventFeedbackValidatorTests()
        {
            _validator = new CreateEventFeedbackValidator();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(10)]
        public void Validate_QuandoDadosForemValidos_DevePassarNaValidacao(int rating)
        {
            var dto = new CreateEventFeedbackRequestDto
            {
                Rating = rating,
                FeedbackText = "Ótimo evento!"
            };

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(11)]
        public void Validate_QuandoRatingEstiverForaDoIntervalo0A10_DeveFalhar(int rating)
        {
            var dto = new CreateEventFeedbackRequestDto
            {
                Rating = rating,
                FeedbackText = "Comentário"
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "A avaliação deve ser uma nota entre 0 e 10.");
        }

        [Fact]
        public void Validate_QuandoFeedbackTextTiverExatamente1000Caracteres_DevePassarNaValidacao()
        {
            var dto = new CreateEventFeedbackRequestDto
            {
                Rating = 9,
                FeedbackText = new string('A', 1000)
            };

            var result = _validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_QuandoFeedbackTextExceder1000Caracteres_DeveFalhar()
        {
            var dto = new CreateEventFeedbackRequestDto
            {
                Rating = 9,
                FeedbackText = new string('A', 1001)
            };

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == "O comentário do feedback não pode exceder 1000 caracteres.");
        }
    }
}

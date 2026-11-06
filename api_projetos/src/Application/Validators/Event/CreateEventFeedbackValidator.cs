using API_Gestao_Eventos.src.Application.DTO.Event;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.Event
{
    public class CreateEventFeedbackValidator : AbstractValidator<CreateEventFeedbackRequestDto>
    {
        public CreateEventFeedbackValidator()
        {
            RuleFor(x => x.Rating)
                .InclusiveBetween(0, 10)
                .WithMessage("A avaliação deve ser uma nota entre 0 e 10.");

            RuleFor(x => x.FeedbackText)
                .MaximumLength(1000)
                .WithMessage("O comentário do feedback não pode exceder 1000 caracteres.");
        }
    }
}


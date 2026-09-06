using API_Gestao_Eventos.src.Application.DTO.Event;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.Event
{
    public class CreateEventRequestValidator : AbstractValidator<CreateEventRequestDto>
    {
        public CreateEventRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome do evento é obrigatório.")
                .MaximumLength(100).WithMessage("O nome do evento não pode exceder 100 caracteres.");
            RuleFor(x => x.Description)
                .MaximumLength(300)
                .WithMessage("A descrição do evento não pode exceder 300 caracteres.");
            RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("A instituição é obrigatória.");
            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("A data de início é obrigatória.")
                .LessThan(x => x.EndDate).WithMessage("A data de início deve ser anterior à data de término.")
                .GreaterThan(x => DateTime.UtcNow).WithMessage("A data de início deve ser futura.");
            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("A data de término é obrigatória.");
            RuleFor(x => x.Capacity)
                .GreaterThan(0)
                .WithMessage("A capacidade do evento deve ser maior que zero.");
            RuleFor(x => x.EventType)
                .IsInEnum()
                .WithMessage("O tipo de evento é obrigatório.");
            RuleFor(x => x.Capacity)
                .NotEmpty()
                .WithMessage("A capacidade do evento é obrigatória.");
        }
    }
}

using API_Gestao_Eventos.src.Application.DTO.Group;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.Group
{
    public class CreateGroupValidator : AbstractValidator<CreateGroupRequestDto>
    {
        public CreateGroupValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("O evento é obrigatório.");
            RuleFor(x => x.StudentIds)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("A lista de alunos é obrigatória.")
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Há alunos repetidos na lista de convidados.");
        }
    }
}
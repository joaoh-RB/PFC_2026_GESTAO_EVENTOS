using API_Gestao_Eventos.src.Application.DTO.Group;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.Group
{
    public class InviteGroupMembersValidator : AbstractValidator<InviteGroupMembersRequestDto>
    {
        public InviteGroupMembersValidator()
        {
            RuleFor(x => x.StudentIds)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Selecione ao menos um aluno para convidar.")
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Há alunos repetidos na lista de convidados.");
        }
    }
}
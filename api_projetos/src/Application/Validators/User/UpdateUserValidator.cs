using API_Gestao_Eventos.src.Application.DTO.User;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.User
{
    public class UpdateStudentValidator : AbstractValidator<UpdateStudentRequestDto>
    {
        public UpdateStudentValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome é obrigatório.");
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O email é obrigatório.")
                .EmailAddress().WithMessage("O email deve ser válido.");
            RuleFor(x => x.UniqueIdentifier)
                .NotEmpty().WithMessage("O número de matrícula é obrigatório.");
            RuleFor(x => x.InstitutionId)
                .NotEmpty().WithMessage("A instituição é obrigatória.");
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("O curso é obrigatório.");
        }
    }
    public class UpdateInstitutionMemberValidator : AbstractValidator<UpdateInstitutionMemberRequestDto>
    {
        public UpdateInstitutionMemberValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome é obrigatório.");
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O email é obrigatório.")
                .EmailAddress().WithMessage("O email deve ser válido.");
        }
    }
}

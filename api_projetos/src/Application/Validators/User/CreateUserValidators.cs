using API_Gestao_Eventos.src.Application.DTO.User;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.User
{
    public class CreateStudentValidator : AbstractValidator<CreateStudentRequestDto>
    {
        public CreateStudentValidator()
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
    public class CreateInstitutionMemberValidator : AbstractValidator<CreateInstitutionMemberRequestDto>
    {
        public CreateInstitutionMemberValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome é obrigatório.");
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O email é obrigatório.")
                .EmailAddress().WithMessage("O email deve ser válido.");
            RuleFor(x => x.InstitutionId)
                .NotEmpty().WithMessage("A instituição é obrigatória.");
            RuleFor(x => x.UserRole)
                .IsInEnum().WithMessage("O cargo deve ser válido.");
            RuleFor(x => x.Courses)
                .NotEmpty()
                .WithMessage("Selecione ao menos um curso.")
                .When(x => x.UserRole == Domain.Enums.UserRole.Professor);
        }
    }
}

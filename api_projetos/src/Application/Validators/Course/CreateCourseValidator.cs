using API_Gestao_Eventos.src.Application.DTO.Course;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.Course
{
    public class CreateCourseValidator : AbstractValidator<CreateCourseDto>
    {
        public CreateCourseValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome do curso é obrigatório.")
                .MaximumLength(150);
            RuleFor(x => x.InstitutionId)
                .NotEmpty().WithMessage("A instituição é obrigatória.");
        }
    }
}

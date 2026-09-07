using API_Gestao_Eventos.src.Application.DTO.Course;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.Course
{
    public class UpdateCourseValidator : AbstractValidator<UpdateCourseDto>
    {
        public UpdateCourseValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome do curso é obrigatório.")
                .MaximumLength(150);
        }
    }
}
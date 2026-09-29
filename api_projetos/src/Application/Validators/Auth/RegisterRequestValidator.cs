using API_Gestao_Eventos.src.Application.DTO.Auth;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.Auth
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("O nome é obrigatório.");
            RuleFor(x => x.Email).NotEmpty().WithMessage("O e-mail é obrigatório.").EmailAddress().WithMessage("O e-mail deve ser válido.");
            RuleFor(x => x.Password).NotEmpty().WithMessage("A senha é obrigatória.").MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
                .Matches("[A-Z]")
                    .WithMessage("A senha deve conter ao menos uma letra maiúscula.")
                .Matches("[0-9]")
                    .WithMessage("A senha deve conter ao menos um número.")
                .Matches(@"[\W_]")
                    .WithMessage("A senha deve conter ao menos um caractere especial.");
            RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("A instituição é obrigatória.");
            RuleFor(x => x.CourseId).NotEmpty().WithMessage("O curso é obrigatório.");
            RuleFor(x => x.UniqueIdentifier).NotEmpty().WithMessage("O RGM é obrigatório.");
        }
    }
}

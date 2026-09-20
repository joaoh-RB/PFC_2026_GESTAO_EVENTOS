using API_Gestao_Eventos.src.Application.DTO.Auth;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.Auth
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequestDto>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O e-mail é obrigatório.")
                .EmailAddress().WithMessage("O e-mail informado não é válido.");
            RuleFor(x => x.NewPassword).NotEmpty().WithMessage("A senha é obrigatória.").MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
                .Matches("[A-Z]")
                    .WithMessage("A senha deve conter ao menos uma letra maiúscula.")
                .Matches("[0-9]")
                    .WithMessage("A senha deve conter ao menos um número.")
                .Matches(@"[\W_]")
                    .WithMessage("A senha deve conter ao menos um caractere especial.");
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("A confirmação da nova senha é obrigatória.")
                .Equal(x => x.NewPassword).WithMessage("A confirmação da nova senha não confere com a nova senha.");
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("O token de redefinição de senha é obrigatório.");
        }
    }
}

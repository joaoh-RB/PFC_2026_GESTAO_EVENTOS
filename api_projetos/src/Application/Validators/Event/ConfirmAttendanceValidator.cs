using API_Gestao_Eventos.src.Application.DTO.Event;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.Event
{
    public class ConfirmAttendanceValidator : AbstractValidator<ConfirmAttendanceRequestDto>
    {
        public ConfirmAttendanceValidator()
        {
            RuleFor(x => x.Code)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Informe o código de presença.")
                .Length(6).WithMessage("O código de presença deve ter 6 caracteres.");
        }
    }
}
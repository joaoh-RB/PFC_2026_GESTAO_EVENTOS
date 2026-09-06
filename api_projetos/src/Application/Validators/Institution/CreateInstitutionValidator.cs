using API_Gestao_Eventos.src.Application.DTO.Institution;
using API_Gestao_Eventos.src.Domain;
using FluentValidation;

namespace API_Gestao_Eventos.src.Application.Validators.Institution
{
    public class CreateInstitutionValidator : AbstractValidator<CreateInstitutionDto>
    {
        public CreateInstitutionValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome da instituição é obrigatório.")
                .MaximumLength(150);
            When(x => !string.IsNullOrWhiteSpace(x.Cnpj), () =>
            {
                RuleFor(x => x.Cnpj!)
                    .Must(Cnpj.IsValid)
                    .WithMessage("CNPJ inválido.");
            });
            RuleFor(x => x.Address).MaximumLength(250);
            RuleFor(x => x.Phone).MaximumLength(20);
        }
    }
}

// Código real, vivo em src/domain/commands/handlers/CriarClienteCommandValidator.cs — este arquivo é cópia de leitura, não edite aqui.
using FluentValidation;

namespace BaseDotnet.Domain.Commands.Handlers
{
    public class CriarClienteCommandValidator : AbstractValidator<CriarClienteCommand>
    {
        public CriarClienteCommandValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatorio.")
                .MaximumLength(100).WithMessage("Nome deve ter no maximo 100 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email é obrigatorio.")
                .EmailAddress().WithMessage("Email invalido.")
                .MaximumLength(200).WithMessage("Email deve ter no maximo 200 caracteres.");
        }
    }
}

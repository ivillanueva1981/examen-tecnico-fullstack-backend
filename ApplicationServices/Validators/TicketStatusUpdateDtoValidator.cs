using ApplicationServices.DTOs;
using FluentValidation;

namespace ApplicationServices.Validators;

public class TicketStatusUpdateDtoValidator : AbstractValidator<TicketStatusUpdateDto>
{
    private readonly string[] _allowedStatuses = ["Abierto", "EnProgreso", "Cerrado"];

    public TicketStatusUpdateDtoValidator()
    {
        RuleFor(t => t.Status)
            //.Transform(status => status?.Trim())
            .NotEmpty().WithMessage("El estado es obligatorio.")
            .Must(status => _allowedStatuses.Contains(status))
            .WithMessage("El estado proporcionado no es válido. Valores permitidos: Abierto, EnProgreso, Cerrado.");
    }
}
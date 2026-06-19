using ApplicationServices.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApplicationServices.Validators;

public class TicketUpdateDtoValidator : AbstractValidator<TicketUpdateDto>
{
    private readonly string[] _allowedStatuses = ["Abierto", "EnProgreso", "Cerrado"];

    public TicketUpdateDtoValidator()
    {
        RuleFor(t => t.Title)
            //.Transform(title => title?.Trim())
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MaximumLength(120).WithMessage("El título no puede superar los 120 caracteres.")
            .Must(NotContainHtml).WithMessage("El título contiene código HTML no permitido.");

        RuleFor(t => t.Description)
           // .Transform(desc => desc?.Trim())
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(2000).WithMessage("La descripción no puede superar los 2000 caracteres.")
            .Must(NotContainHtml).WithMessage("La descripción contiene código HTML no permitido.");

        RuleFor(t => t.Status)
            //.Transform(status => status?.Trim())
            .NotEmpty().WithMessage("El estado es obligatorio.")
            .Must(status => _allowedStatuses.Contains(status))
            .WithMessage("El estado proporcionado no es válido. Valores permitidos: Abierto, EnProgreso, Cerrado.");
    }

    private bool NotContainHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;
        var htmlRegex = new Regex(@"<[^>]*>|javascript:|onerror|onload", RegexOptions.IgnoreCase);
        return !htmlRegex.IsMatch(text);
    }
}
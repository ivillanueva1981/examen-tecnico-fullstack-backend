using ApplicationServices.DTOs;
using FluentValidation;
using System.Text.RegularExpressions;

namespace ApplicationServices.Validators;

public class TicketCreateDtoValidator : AbstractValidator<TicketCreateDto>
{
    public TicketCreateDtoValidator()
    {
        RuleFor(t => t.Title)
           // .Transform(title => title?.Trim()) // Sanitización: elimina espacios huérfanos
            .NotEmpty().WithMessage("El título del ticket es obligatorio.")
            .MaximumLength(120).WithMessage("El título no puede superar los 120 caracteres.") // Límite exacto del script SQL
            .Must(NotContainHtml).WithMessage("El título contiene caracteres o etiquetas HTML no permitidas.");

        RuleFor(t => t.Description)
            //.Transform(desc => desc?.Trim())
            .NotEmpty().WithMessage("La descripción del ticket es obligatoria.")
            .MaximumLength(2000).WithMessage("La descripción no puede superar los 2000 caracteres.") // Límite del script SQL
            .Must(NotContainHtml).WithMessage("La descripción contiene caracteres o etiquetas HTML no permitidas.");

        RuleFor(t => t.Priority)
           // .Transform(priority => priority?.Trim())
            .NotEmpty().WithMessage("La prioridad es obligatoria.")
            .MaximumLength(20).WithMessage("La prioridad no puede superar los 20 caracteres.");
    }

    // 🛡️ Validador contra XSS e inyección de etiquetas HTML peligrosas
    private bool NotContainHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;
        var htmlRegex = new Regex(@"<[^>]*>|javascript:|onerror|onload", RegexOptions.IgnoreCase);
        return !htmlRegex.IsMatch(text);
    }
}
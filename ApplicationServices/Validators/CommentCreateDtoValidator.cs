using ApplicationServices.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApplicationServices.Validators;

public class CommentCreateDtoValidator : AbstractValidator<CommentCreateDto>
{
    public CommentCreateDtoValidator()
    {
        RuleFor(c => c.Text) // Mapeado a la columna [Text] de tu script
            //.Transform(text => text?.Trim())
            .NotEmpty().WithMessage("El texto del comentario es requerido.")
            .Length(2, 2000).WithMessage("El comentario debe tener entre 2 y 2000 caracteres.") // Ajustado a nvarchar(2000)
            .Must(NotContainHtml).WithMessage("El comentario contiene caracteres HTML no permitidos.");
    }

    private bool NotContainHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;
        var htmlRegex = new Regex(@"<[^>]*>|javascript:|onerror|onload", RegexOptions.IgnoreCase);
        return !htmlRegex.IsMatch(text);
    }
}
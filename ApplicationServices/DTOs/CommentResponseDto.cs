using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationServices.DTOs;

public class CommentResponseDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }

    [Required(ErrorMessage = "El texto del comentario es requerido.")]
    [MinLength(2, ErrorMessage = "El contenido debe tener un mínimo de 2 caracteres.")]
    [MaxLength(2000, ErrorMessage = "El contenido no puede superar los 2000 caracteres.")]
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}

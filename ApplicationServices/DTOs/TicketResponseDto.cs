using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationServices.DTOs;

public class TicketResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }

    // 🚨 AÑADE ESTA PROPIEDAD: Permitirá que viajen los comentarios anidados en el JSON
    public ICollection<CommentResponseDto> Comments { get; set; } = new List<CommentResponseDto>();
}
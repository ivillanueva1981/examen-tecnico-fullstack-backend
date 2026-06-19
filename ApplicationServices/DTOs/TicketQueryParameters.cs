using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationServices.DTOs;

public class TicketQueryParameters
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Q { get; set; }
    public string? OrderBy { get; set; } = "CreatedAtDesc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
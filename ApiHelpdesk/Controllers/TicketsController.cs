using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApplicationServices.DTOs;
using ApplicationServices.Services;
using System.Security.Claims;

namespace ApiHelpdesk.Controllers;

[ApiController]
[Route("api/tickets")] // 🎯 Ruta base personalizada e inmutable
[Authorize] // 🔒 Bloqueo de seguridad global para todos los endpoints
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(ITicketService ticketService, ILogger<TicketsController> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }

    // 🎯 GET /api/tickets?status=Abierto&priority=Alta&q=error&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TicketResponseDto>>> GetAll([FromQuery] TicketQueryParameters query)
    {
        var usuarioLogueado = User.Identity?.Name ?? "Anónimo";

        // 🔍 Logging estructurado para indexar parámetros de búsqueda avanzada
        _logger.LogInformation("Consulta masiva de tickets. Filtros -> Q: {SearchTerm}, Status: {Status}, Pagina: {Page}, Por: {Usuario}",
            query.Q, query.Status, query.Page, usuarioLogueado);

        var result = await _ticketService.GetPagedAsync(query);
        return Ok(result);
    }

    // 🎯 GET /api/tickets/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketResponseDto>> GetById(int id)
    {
        var usuarioLogueado = User.Identity?.Name ?? "Anónimo";
        _logger.LogInformation("Consulta de ticket individual iniciada. TicketId: {TicketId}, Por: {Usuario}", id, usuarioLogueado);

        var result = await _ticketService.GetByIdAsync(id);
        return Ok(result);
    }

    // 🎯 POST /api/tickets
    [HttpPost]
    public async Task<ActionResult<TicketResponseDto>> Create([FromBody] TicketCreateDto dto)
    {
        int currentUserId = GetCurrentUserId();

        _logger.LogInformation("Intento de creación de ticket. Titulo: {TicketTitle}, CreadoPorId: {UserId}", dto.Title, currentUserId);

        var result = await _ticketService.CreateAsync(dto, currentUserId);

        // REST estándar: Retorna HTTP 201 y apunta la cabecera 'Location' al endpoint GetById
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // 🎯 PUT /api/tickets/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TicketUpdateDto dto)
    {
        var usuarioLogueado = User.Identity?.Name ?? "Anónimo";
        _logger.LogInformation("Actualización completa de ticket solicitada. TicketId: {TicketId}, Por: {Usuario}", id, usuarioLogueado);

        await _ticketService.UpdateAsync(id, dto);
        return NoContent(); // HTTP 204 No Content
    }

    // 🎯 PATCH /api/tickets/{id}/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] TicketStatusUpdateDto dto)
    {
        var usuarioLogueado = User.Identity?.Name ?? "Anónimo";
        _logger.LogInformation("Transición parcial de estado iniciada. TicketId: {TicketId}, EstadoDestino: {TicketStatus}, Por: {Usuario}",
            id, dto.Status, usuarioLogueado);

        await _ticketService.UpdateStatusAsync(id, dto);
        return NoContent(); // HTTP 204 No Content
    }

    // 🎯 POST /api/tickets/{id}/comments
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<CommentResponseDto>> AddComment(int id, [FromBody] CommentCreateDto dto)
    {
        int currentUserId = GetCurrentUserId();
        _logger.LogInformation("Agregando comentario en ticket. TicketId: {TicketId}, PorUsuarioId: {UserId}", id, currentUserId);

        var result = await _ticketService.AddCommentAsync(id, dto, currentUserId);

        // Retorna HTTP 201 apuntando al recurso del ticket padre
        return CreatedAtAction(nameof(GetById), new { id = result.TicketId }, result);
    }

    // 🎯 GET /api/tickets/{id}/comments
    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<IEnumerable<CommentResponseDto>>> GetComments(int id)
    {
        var usuarioLogueado = User.Identity?.Name ?? "Anónimo";
        _logger.LogInformation("Lectura de comentarios solicitada. TicketId: {TicketId}, Por: {Usuario}", id, usuarioLogueado);

        var result = await _ticketService.GetCommentsByTicketIdAsync(id);
        return Ok(result); // HTTP 200 OK
    }

    /// <summary>
    /// Helper privado para extraer de forma limpia el ID numérico del usuario autenticado
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var parsedId) ? parsedId : 1; // 1 por defecto para pruebas de desarrollo
    }
}


using ApplicationServices.DTOs;
using ApplicationServices.Exceptions;
using Domain;
using FluentValidation;
using Infrastructure;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ApplicationServices.Services;

public interface ITicketService
{
    /// <summary>
    /// Obtiene una lista paginada de tickets aplicando filtros por estado, 
    /// prioridad, ordenamiento y búsqueda por texto (q).
    /// </summary>
    Task<PagedResponse<TicketResponseDto>> GetPagedAsync(TicketQueryParameters query);

    /// <summary>
    /// Recupera los detalles de un ticket específico mediante su identificador numérico.
    /// Lanza NotFoundException (HTTP 404) si el ID no existe.
    /// </summary>
    Task<TicketResponseDto> GetByIdAsync(int id);

    /// <summary>
    /// Crea un nuevo ticket mapeando un DTO seguro, inyectando los valores iniciales 
    /// obligatorios del sistema y asociándolo al ID del usuario creador.
    /// </summary>
    Task<TicketResponseDto> CreateAsync(TicketCreateDto dto, int currentUserId);

    /// <summary>
    /// Actualiza de forma completa las propiedades editables de un ticket existente.
    /// Valida transiciones de estado arrojando ConflictException (HTTP 409) si se viola el flujo.
    /// </summary>
    Task UpdateAsync(int id, TicketUpdateDto dto);

    /// <summary>
    /// Realiza una actualización parcial exclusivamente sobre el estado del ticket,
    /// validando de forma estricta las reglas de la máquina de estados del negocio.
    /// </summary>
    Task UpdateStatusAsync(int id, TicketStatusUpdateDto dto);

    /// <summary>
    /// Agrega de forma segura un nuevo comentario enlazado a un ticket específico, 
    /// inyectando el autor y controlando la sanitización estructural contra XSS.
    /// </summary>
    Task<CommentResponseDto> AddCommentAsync(int ticketId, CommentCreateDto dto, int currentUserId);

    /// <summary>
    /// Obtiene el listado completo de comentarios asociados a un ticket, 
    /// ordenados cronológicamente de forma descendente.
    /// </summary>
    Task<IEnumerable<CommentResponseDto>> GetCommentsByTicketIdAsync(int ticketId);
}

public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<TicketCreateDto> _createValidator;
    private readonly IValidator<TicketUpdateDto> _updateValidator;
    private readonly IValidator<TicketStatusUpdateDto> _statusValidator;
    private readonly IValidator<CommentCreateDto> _commentValidator;

    public TicketService(
        ApplicationDbContext context,
        IValidator<TicketCreateDto> createValidator,
        IValidator<TicketUpdateDto> updateValidator,
        IValidator<TicketStatusUpdateDto> statusValidator,
        IValidator<CommentCreateDto> commentValidator)
    {
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
        _commentValidator = commentValidator;
    }

    public async Task<PagedResponse<TicketResponseDto>> GetPagedAsync(TicketQueryParameters query)
    {
        var queryable = _context.Tickets.AsQueryable();

        // 1. Filtrado exacto
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            queryable = queryable.Where(t => t.Status == query.Status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            queryable = queryable.Where(t => t.Priority == query.Priority.Trim());
        }

        // 2. Búsqueda por texto (q) en Título o Descripción
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var searchTerm = query.Q.Trim().ToLower();
            queryable = queryable.Where(t => t.Title.ToLower().Contains(searchTerm) ||
                                             t.Description.ToLower().Contains(searchTerm));
        }

        // 3. Ordenamiento dinámico
        queryable = query.OrderBy switch
        {
            "TitleAsc" => queryable.OrderBy(t => t.Title),
            "TitleDesc" => queryable.OrderByDescending(t => t.Title),
            "CreatedAtAsc" => queryable.OrderBy(t => t.CreatedAt),
            _ => queryable.OrderByDescending(t => t.CreatedAt) // Por defecto: más recientes primero
        };

        // 4. Conteo total de elementos (para metadatos de paginación)
        var totalItems = await queryable.CountAsync();

        // 5. Paginación en Base de Datos (Skip y Take)
        var tickets = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = tickets.Adapt<IEnumerable<TicketResponseDto>>();
        return new PagedResponse<TicketResponseDto>(dtos, query.Page, query.PageSize, totalItems);
    }

    public async Task<TicketResponseDto> GetByIdAsync(int id)
    {
        // 🚨 CORRECCIÓN: Agregamos .Include(t => t.Comments) para traer la colección asociada
        var ticket = await _context.Tickets
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            throw new NotFoundException($"El ticket con ID {id} no fue encontrado.");
        }

        return ticket.Adapt<TicketResponseDto>();
    }

    public async Task<TicketResponseDto> CreateAsync(TicketCreateDto dto, int currentUserId)
    {
        // Validar estructuralmente y sanitizar contra XSS
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Mapear DTO seguro a Entidad de Dominio
        var ticket = dto.Adapt<Ticket>();

        // Forzar valores obligatorios del sistema y del script SQL
        ticket.Status = "Abierto";
        ticket.CreatedAt = DateTime.UtcNow;
        ticket.CreatedBy = currentUserId;

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        return ticket.Adapt<TicketResponseDto>();
    }

    public async Task UpdateAsync(int id, TicketUpdateDto dto)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var dbTicket = await _context.Tickets.FindAsync(id);
        if (dbTicket == null)
        {
            throw new NotFoundException($"El ticket con ID {id} no existe.");
        }

        // Control de transición de estados inválidos (Conflictos 409)
        if (dbTicket.Status == "Cerrado" && dto.Status != "Cerrado")
        {
            throw new ConflictException($"No se puede reabrir ni modificar el ticket {id} porque ya está en estado 'Cerrado'.");
        }

        // Mapear de forma segura sobre la entidad rastreada (Evita overposting)
        dto.Adapt(dbTicket);

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int id, TicketStatusUpdateDto dto)
    {
        var validationResult = await _statusValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var dbTicket = await _context.Tickets.FindAsync(id);
        if (dbTicket == null)
        {
            throw new NotFoundException($"El ticket con ID {id} no existe.");
        }

        // Reglas de negocio estrictas para cambios parciales de estado
        if (dbTicket.Status == "Cerrado")
        {
            throw new ConflictException($"No se puede cambiar el estado del ticket {id} porque ya está finalizado en estado 'Cerrado'.");
        }

        if (dbTicket.Status == "Abierto" && dto.Status == "Cerrado")
        {
            throw new ConflictException("No se puede cerrar un ticket directo desde 'Abierto'. Debe pasar primero por 'EnProgreso'.");
        }

        dbTicket.Status = dto.Status;
        await _context.SaveChangesAsync();
    }

    public async Task<CommentResponseDto> AddCommentAsync(int ticketId, CommentCreateDto dto, int currentUserId)
    {
        var validationResult = await _commentValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var ticketExists = await _context.Tickets.AnyAsync(t => t.Id == ticketId);
        if (!ticketExists)
        {
            throw new NotFoundException($"No se puede agregar el comentario porque el ticket con ID {ticketId} no existe.");
        }

        var comment = dto.Adapt<Comment>();

        // Autogenerar ID manual para el comentario si la tabla SQL Server no posee IDENTITY
        var maxId = await _context.Comments.MaxAsync(c => (int?)c.Id) ?? 0;
        comment.Id = maxId + 1;

        comment.TicketId = ticketId;
        comment.CreatedBy = currentUserId;
        comment.CreatedAt = DateTime.UtcNow;

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return comment.Adapt<CommentResponseDto>();
    }

    public async Task<IEnumerable<CommentResponseDto>> GetCommentsByTicketIdAsync(int ticketId)
    {
        var ticketExists = await _context.Tickets.AnyAsync(t => t.Id == ticketId);
        if (!ticketExists)
        {
            throw new NotFoundException($"No se pueden listar los comentarios porque el ticket con ID {ticketId} no existe.");
        }

        var comments = await _context.Comments
            .Where(c => c.TicketId == ticketId)
            .OrderByDescending(c => c.CreatedAt) // Comentarios más recientes primero
            .ToListAsync();

        return comments.Adapt<IEnumerable<CommentResponseDto>>();
    }
}

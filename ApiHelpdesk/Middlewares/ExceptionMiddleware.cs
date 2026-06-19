using ApplicationServices.Exceptions;
using System.Net;
using System.Text.Json;
using FluentValidation;

namespace ApiHelpdesk.Middlewares;


public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió un error no controlado en la aplicación.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var errorResponse = new ErrorResponse();

        switch (exception)
        {
            // 🚨 CAPTURA ERRORES DE FLUENTVALIDATION
            case ValidationException valEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Errores de validación en la petición.";
                // Estructura los errores en formato de texto legible o lista
                errorResponse.Details = string.Join(" | ", valEx.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
                break;
            case BadRequestException:   //  - `400` validación
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = exception.Message;
                break;

            case NotFoundException:     //   - `400` validación
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = exception.Message;
                break;

            case ConflictException:     //  - `409` conflictos (ej. transición de estado inválida)
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Message = exception.Message;
                break;

            default:
                // 500` error inesperado
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "Ocurrió un error inesperado en el servidor.";
                // Solo muestra el stack trace detallado si estás en desarrollo
                if (_env.IsDevelopment())
                {
                    errorResponse.Details = exception.StackTrace;
                }
                break;
        }

        errorResponse.StatusCode = context.Response.StatusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(errorResponse, options);

        return context.Response.WriteAsync(json);
    }
}

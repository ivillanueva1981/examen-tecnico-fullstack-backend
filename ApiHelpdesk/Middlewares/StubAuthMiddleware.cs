using System.Security.Claims;

namespace ApiHelpdesk.Middlewares;

public class StubAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public StubAuthMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Opcional: Activar el stub solo en entornos de Desarrollo/Pruebas
        // Dentro de StubAuthMiddleware.cs...
        if (_env.IsDevelopment() && context.Request.Headers.TryGetValue("X-User-Id", out var userIdStr))
        {
            // Leer roles desde otro header opcional, o asignar uno por defecto
            context.Request.Headers.TryGetValue("X-Roles", out var rolesHeader);
            var roles = !string.IsNullOrEmpty(rolesHeader)
                ? rolesHeader.ToString().Split(',')
                : ["Admin", "User"]; // Roles por defecto para pruebas

            // 1. Crear las declaraciones (Claims) del usuario simulado
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userIdStr.ToString()), // Contendrá el "1" (ID numérico del User)
                new(ClaimTypes.Name, "Usuario Simulador")
            };
            // ... resto del código del middleware igual
            // Añadir los roles configurados
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.Trim())));

            // 2. Crear la identidad e inyectarla en el contexto HTTP
            var identity = new ClaimsIdentity(claims, "StubAuth");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}

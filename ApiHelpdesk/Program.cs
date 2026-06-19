
using ApiHelpdesk.Auth;
using ApiHelpdesk.Middlewares;
using ApplicationServices.Services;
using ApplicationServices.Validators;
using FluentValidation;
using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Serilog;


// 1. CONFIGURACIÓN INICIAL DE SERILOG
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();

try
{
    Log.Information("Iniciando el servidor web de la API de Soporte...");

    var builder = WebApplication.CreateBuilder(args);

    // 2. FORZAR PUERTOS DE ESCUCHA (Evita el conflicto del puerto 7008)
    builder.WebHost.UseUrls("http://localhost:5050", "https://localhost:7070");

    // 3. REEMPLAZAR EL LOGGING POR DEFECTO POR SERILOG
    builder.Host.UseSerilog();

    // 🚨 4. ---- AÑADE ESTO PARA DARLE PERMISO A ANGULAR ----
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("PermitirAngular", policy =>
        {
            policy.WithOrigins("http://localhost:4200") // URL de tu Angular
                  .AllowAnyMethod()
                  .AllowAnyHeader(); // Permite que Angular mande X-User y X-User-Id
        });
    });

    // 5. CONFIGURACIÓN DE CONEXIÓN A MICROSOFT SQL SERVER
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, b => b.MigrationsAssembly("MiApi.WebApi")));

    // 6. REGISTRO DE SERVICIOS DE LA CAPA DE NEGOCIO (DI)
    builder.Services.AddScoped<ITicketService, TicketService>();
 

    // 7. INYECCIÓN MASIVA AUTOMÁTICA DE FLUENTVALIDATION
    builder.Services.AddValidatorsFromAssemblyContaining<TicketCreateDtoValidator>();

    // 8. CONFIGURACIÓN DEL ESQUEMA STUB AUTH (Bypass para desarrollo)
    builder.Services.AddAuthentication("StubAuthScheme")
        .AddScheme<AuthenticationSchemeOptions, StubAuthHandler>("StubAuthScheme", null);
    builder.Services.AddAuthorization();

    // Configuración básica de Controladores y Swagger
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "API Soporte .NET 8", Version = "v1" });
        options.CustomSchemaIds(type => type.FullName);
    });

    var app = builder.Build();

    // 9. MIDDLEWARE DE REGISTRO DE PETICIONES HTTP DE SERILOG
    app.UseSerilogRequestLogging();

    // 10. MIDDLEWARE DE MANEJO DE EXCEPCIONES GLOBAL
    app.UseMiddleware<ExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Soporte v1"));
    }

    app.UseHttpsRedirection();

    // 🚨 11. ACTIVAR LA POLÍTICA CORS DE ANGULAR
    // Importante: Debe ejecutarse antes de la autenticación, autorización y el mapeo de controladores.
    app.UseCors("PermitirAngular");

    // 12. ORDEN ESTRICTO DE MIDDLEWARES DE AUTENTICACIÓN Y AUTORIZACIÓN
    app.UseMiddleware<StubAuthMiddleware>(); // Inyecta Claims desde headers X-User-Id / X-User
    app.UseAuthorization();                 // Valida los permisos de los atributos [Authorize]

    app.MapControllers();

    // 13. INYECCIÓN AUTOMÁTICA DE DATOS DE PRUEBA AL ARRANCAR (DataSeeder)
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await DataSeeder.SeedAsync(context);
            Log.Information("Base de datos sincronizada y datos de prueba cargados con éxito.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ocurrió un error al intentar poblar la base de datos.");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "El servidor web terminó inesperadamente debido a un fallo crítico.");
}
finally
{
    Log.CloseAndFlush();
}

using Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure;

 

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 1. Asegurar que las tablas estén creadas y actualizadas en SQL Server
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }

        // 2. POBLAR TABLA [User] (Requerido para evitar fallos de Clave Foránea)
        if (!await context.Users.AnyAsync())
        {
            var testUsers = new List<User>
            {
                new() { Id = 1, Email = "admin@soporte.com", DisplayName = "Administrador del Sistema" },
                new() { Id = 2, Email = "ingeniero@soporte.com", DisplayName = "Ingeniero Full Stack" }
            };

            await context.Users.AddRangeAsync(testUsers);
            await context.SaveChangesAsync();
        }

        // 3. POBLAR TABLA [Ticket] (Soportará tus pruebas de filtrado y búsqueda)
        if (!await context.Tickets.AnyAsync())
        {
            var testTickets = new List<Ticket>
            {
                new()
                {
                    Title = "Error crítico en pasarela de pagos",
                    Description = "El botón de pagar no responde y arroja un timeout al procesar tarjetas de crédito.",
                    Priority = "Alta",
                    Status = "Abierto",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    CreatedBy = 2 // Creado por el Ingeniero
                },
                new()
                {
                    Title = "Fallo de conexión en la VPN corporativa",
                    Description = "Los usuarios de remoto reportan intermitencia al conectar con el servidor de desarrollo.",
                    Priority = "Alta",
                    Status = "EnProgreso",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    CreatedBy = 1 // Creado por el Admin
                },
                new()
                {
                    Title = "Actualizar licencias de IDEs de desarrollo",
                    Description = "Se requiere renovar las suscripciones anuales del equipo de ingeniería informática.",
                    Priority = "Baja",
                    Status = "Abierto",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedBy = 1
                },
                new()
                {
                    Title = "Configuración de contenedor Docker corrupta",
                    Description = "El entorno local tira error al levantar la imagen base de SQL Server en microservicios.",
                    Priority = "Media",
                    Status = "Cerrado",
                    CreatedAt = DateTime.UtcNow.AddHours(-5),
                    CreatedBy = 2
                }
            };

            await context.Tickets.AddRangeAsync(testTickets);
            await context.SaveChangesAsync(); // Guardamos para que SQL Server genere los IDs autoincrementales
        }

        // 4. POBLAR TABLA [Comment]
        if (!await context.Comments.AnyAsync())
        {
            // Recuperamos los IDs de los tickets recién creados para enlazarlos correctamente
            var ticketsDb = await context.Tickets.ToListAsync();
            var ticketPagos = ticketsDb.FirstOrDefault(t => t.Title.Contains("pagos"));
            var ticketVpn = ticketsDb.FirstOrDefault(t => t.Title.Contains("VPN"));

            var testComments = new List<Comment>();

            if (ticketPagos != null)
            {
                testComments.Add(new Comment
                {
                    Id = 1, // Manual: Recuerda que en tu script [Comment].[Id] no es IDENTITY
                    TicketId = ticketPagos.Id,
                    Text = "He revisado los logs de Serilog y el proveedor externo está rechazando las peticiones.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    CreatedBy = 1 // Comentado por el Admin
                });
                testComments.Add(new Comment
                {
                    Id = 2,
                    TicketId = ticketPagos.Id,
                    Text = "Entendido, procederé a mockear la respuesta para avanzar con las pruebas locales.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedBy = 2 // Comentado por el Ingeniero
                });
            }

            if (ticketVpn != null)
            {
                testComments.Add(new Comment
                {
                    Id = 3,
                    TicketId = ticketVpn.Id,
                    Text = "Se identificó un bloqueo en el puerto de escucha del firewall corporativo.",
                    CreatedAt = DateTime.UtcNow.AddHours(-12),
                    CreatedBy = 2
                });
            }

            if (testComments.Any())
            {
                await context.Comments.AddRangeAsync(testComments);
                await context.SaveChangesAsync();
            }
        }
    }
}

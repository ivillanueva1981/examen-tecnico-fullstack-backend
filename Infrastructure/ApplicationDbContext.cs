using Domain;
using Microsoft.EntityFrameworkCore;
 
namespace Infrastructure;
 
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<User> Users => Set<User>();
 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mapeo Tabla USER
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(u => u.Id).HasName("PK_User");
            entity.Property(u => u.Id).ValueGeneratedNever(); // No es Identity
            entity.Property(u => u.Email).HasMaxLength(50).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();
        });

        // Mapeo Tabla TICKET
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Ticket");
            entity.HasKey(t => t.Id).HasName("PK_Ticket");
            entity.Property(t => t.Id).UseIdentityColumn(); // IDENTITY(1,1)
            entity.Property(t => t.Title).HasMaxLength(120).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(2000).IsRequired();
            entity.Property(t => t.Priority).HasMaxLength(20).IsRequired();
            entity.Property(t => t.Status).HasMaxLength(20).IsRequired();
            entity.Property(t => t.CreatedAt).HasColumnType("datetime").IsRequired();

            // Relación FK_Ticket_User
            entity.HasOne(t => t.Creator)
                  .WithMany(u => u.Tickets)
                  .HasForeignKey(t => t.CreatedBy)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Ticket_User");
        });

        // Mapeo Tabla COMMENT
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comment");
            entity.HasKey(c => c.Id).HasName("PK_Comment");
            entity.Property(c => c.Id).ValueGeneratedNever(); // NO es Identity según tu script
            entity.Property(c => c.Text).HasMaxLength(2000).IsRequired();
            entity.Property(c => c.CreatedAt).HasColumnType("datetime").IsRequired();

            // Relación FK_Comment_Ticket
            entity.HasOne(c => c.Ticket)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(c => c.TicketId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Comment_Ticket");

            // Relación FK_Comment_User
            entity.HasOne(c => c.Creator)
                  .WithMany(u => u.Comments)
                  .HasForeignKey(c => c.CreatedBy)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Comment_User");
        });
 
    }
}

using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Models;

namespace SISReservas.Api.Data;

// Extensión del módulo: el Core se construye aunque este archivo no esté presente.
public partial class SISReservasDbContext
{
    public DbSet<Espacio> Espacios => Set<Espacio>();
    public DbSet<Reserva> Reservas => Set<Reserva>();

    partial void ConfigurarReservas(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Espacio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();

            entity.ToTable("Espacios", table =>
            {
                table.HasCheckConstraint("CK_Espacios_Nombre", "LEN(LTRIM(RTRIM([Nombre]))) > 0");
                table.HasCheckConstraint("CK_Espacios_Tipo", "[Tipo] IN ('Salon', 'Cancha', 'Equipo')");
            });
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Estado).HasConversion<string>().HasMaxLength(30).IsRequired();

            entity.HasOne(r => r.Usuario)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Espacio)
                .WithMany(e => e.Reservas)
                .HasForeignKey(r => r.EspacioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => new { r.EspacioId, r.Estado, r.FechaInicio, r.FechaFin });
            entity.HasIndex(r => r.Estado);
            entity.HasIndex(r => r.FechaInicio);
            entity.HasIndex(r => r.FechaFin);

            entity.ToTable("Reservas", table =>
            {
                table.HasCheckConstraint("CK_Reservas_Fechas", "[FechaFin] > [FechaInicio]");
                table.HasCheckConstraint("CK_Reservas_Estado", "[Estado] IN ('Pendiente', 'Confirmada', 'Cancelada', 'Completada')");
            });
        });

    }
}

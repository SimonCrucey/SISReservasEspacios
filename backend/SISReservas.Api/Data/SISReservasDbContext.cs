using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Models;

namespace SISReservas.Api.Data;

public class SISReservasDbContext : DbContext
{
    public SISReservasDbContext(DbContextOptions<SISReservasDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TokenAcceso> TokensAcceso => Set<TokenAcceso>();
    public DbSet<CorreoEnCola> CorreosEnCola => Set<CorreoEnCola>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();
    public DbSet<SolicitudPermiso> SolicitudesPermiso => Set<SolicitudPermiso>();
    public DbSet<UsuarioPermiso> PermisosUsuario => Set<UsuarioPermiso>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(320)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.Rol)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
        });

        modelBuilder.Entity<TokenAcceso>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            entity.HasIndex(t => t.TokenHash)
                .IsUnique();

            entity.Property(t => t.Tipo)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.HasOne(t => t.Usuario)
                .WithMany(u => u.TokensAcceso)
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CorreoEnCola>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Destinatario)
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(c => c.Asunto)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(c => c.Cuerpo)
                .IsRequired();

            entity.Property(c => c.Estado)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasOne(c => c.Usuario)
                .WithMany(u => u.Correos)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sesion>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            entity.HasIndex(s => s.TokenHash)
                .IsUnique();

            entity.HasOne(s => s.Usuario)
                .WithMany(u => u.Sesiones)
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SolicitudPermiso>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Permiso)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(s => s.Estado)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(s => s.MotivoRechazo)
                .HasMaxLength(500);

            entity.Property(s => s.RowVersion)
                .IsRowVersion();

            entity.HasOne(s => s.Solicitante)
                .WithMany(u => u.SolicitudesPermiso)
                .HasForeignKey(s => s.SolicitanteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.ResueltaPor)
                .WithMany(u => u.SolicitudesResueltas)
                .HasForeignKey(s => s.ResueltaPorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => s.Estado);

            entity.HasIndex(s => s.SolicitanteId);
        });

        modelBuilder.Entity<UsuarioPermiso>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Permiso)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.HasOne(p => p.Usuario)
                .WithMany(u => u.Permisos)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => new { p.UsuarioId, p.Permiso })
                .IsUnique();
        });
    }
}
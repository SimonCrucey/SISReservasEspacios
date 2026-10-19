using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using SISReservas.Api.Data;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

internal sealed class TestDatabase : IDisposable
{
    private readonly string _databaseName =
        $"SISReservasTests_{Guid.NewGuid():N}";

    private readonly InMemoryDatabaseRoot _root = new();
    private readonly List<SISReservasDbContext> _contexts = [];

    public SISReservasDbContext CreateContext(
        params IInterceptor[] interceptors)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<SISReservasDbContext>()
                .UseInMemoryDatabase(_databaseName, _root);

        if (interceptors.Length > 0)
            optionsBuilder.AddInterceptors(interceptors);

        var context =
            new SISReservasDbContext(optionsBuilder.Options);

        _contexts.Add(context);

        return context;
    }

    public void Dispose()
    {
        foreach (var context in _contexts)
            context.Dispose();
    }
}

internal static class TestData
{
    public static SolicitudPermisoService CrearSolicitudService(
        SISReservasDbContext db)
    {
        return new SolicitudPermisoService(
            db,
            new PermisoService(db));
    }

    public static async Task<Usuario> AgregarUsuarioAsync(
        SISReservasDbContext db,
        Rol rol = Rol.Estandar,
        bool activo = true,
        string? nombre = null)
    {
        var usuario = new Usuario
        {
            Nombre = nombre ?? $"Usuario {Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash",
            Rol = rol,
            Activo = activo
        };

        db.Usuarios.Add(usuario);

        await db.SaveChangesAsync();

        return usuario;
    }

    public static async Task<SolicitudPermiso> AgregarSolicitudAsync(
        SISReservasDbContext db,
        Usuario solicitante,
        Permiso permiso = Permiso.ConsultarUsuarios,
        EstadoSolicitudPermiso estado =
            EstadoSolicitudPermiso.Pendiente,
        DateTime? creadaEn = null)
    {
        var solicitud = new SolicitudPermiso
        {
            SolicitanteId = solicitante.Id,
            Permiso = permiso,
            Estado = estado,
            CreadaEn = creadaEn ?? DateTime.UtcNow
        };

        db.SolicitudesPermiso.Add(solicitud);

        await db.SaveChangesAsync();

        return solicitud;
    }

    public static async Task AgregarPermisoAsync(
        SISReservasDbContext db,
        Usuario usuario,
        Permiso permiso)
    {
        db.PermisosUsuario.Add(new UsuarioPermiso
        {
            UsuarioId = usuario.Id,
            Permiso = permiso
        });

        await db.SaveChangesAsync();
    }
}

internal sealed class FailingSaveChangesInterceptor
    : SaveChangesInterceptor
{
    public bool Fail { get; set; }

    public override ValueTask<InterceptionResult<int>>
        SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
    {
        if (Fail)
        {
            throw new DbUpdateException(
                "Fallo de persistencia simulado.");
        }

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }
}

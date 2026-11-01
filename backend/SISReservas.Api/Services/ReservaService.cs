using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using SISReservas.Api.Data;
using SISReservas.Api.DTOs.Reservas;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public enum MotivoResultadoReserva
{
    Exito, DatosInvalidos, NoAutorizado, Prohibido, NoEncontrado, Conflicto, ErrorPersistencia
}

public sealed record ResultadoReserva<T>(MotivoResultadoReserva Motivo, string Mensaje, T? Valor)
{
    public bool Exito => Motivo == MotivoResultadoReserva.Exito;
    public static ResultadoReserva<T> Ok(T valor) => new(MotivoResultadoReserva.Exito, "", valor);
    public static ResultadoReserva<T> Falla(MotivoResultadoReserva motivo, string mensaje) => new(motivo, mensaje, default);
}

public class ReservaService(SISReservasDbContext db, PermisoService permisos, TimeProvider reloj)
{
    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;

    private async Task<Usuario?> UsuarioValidoAsync(Guid id) =>
        await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && u.Activo);

    private static bool RolValido(Usuario usuario) =>
        usuario.Rol is Rol.Administrador or Rol.Estandar;

    // Los contratos de fechas utilizan UTC para evitar ambigüedad entre clientes.
    private static bool IntervaloValido(DateTime inicio, DateTime fin) =>
        inicio.Kind == DateTimeKind.Utc && fin.Kind == DateTimeKind.Utc && inicio < fin;

    private Task<bool> HayConflictoAsync(int espacioId, DateTime inicio, DateTime fin, int excluirId = 0) =>
        db.Reservas.AnyAsync(r => r.EspacioId == espacioId && r.Id != excluirId
            && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada)
            && inicio < r.FechaFin && fin > r.FechaInicio);

    public async Task<ResultadoReserva<List<EspacioResponseDto>>> ListarEspaciosAsync(Guid usuarioId)
    {
        var usuario = await UsuarioValidoAsync(usuarioId);
        if (usuario is null)
            return ResultadoReserva<List<EspacioResponseDto>>.Falla(MotivoResultadoReserva.NoAutorizado, "Usuario no disponible.");
        if (!RolValido(usuario))
            return ResultadoReserva<List<EspacioResponseDto>>.Falla(MotivoResultadoReserva.Prohibido, "Rol no permitido.");
        var espacios = await db.Espacios.AsNoTracking().Where(e => e.Activo).OrderBy(e => e.Id)
            .Select(e => new EspacioResponseDto(e.Id, e.Nombre, e.Tipo)).ToListAsync();
        return ResultadoReserva<List<EspacioResponseDto>>.Ok(espacios);
    }

    public async Task<ResultadoReserva<DisponibilidadResponseDto>> ConsultarDisponibilidadAsync(
        Guid usuarioId, int espacioId, DateTime inicio, DateTime fin)
    {
        var usuario = await UsuarioValidoAsync(usuarioId);
        if (usuario is null)
            return ResultadoReserva<DisponibilidadResponseDto>.Falla(MotivoResultadoReserva.NoAutorizado, "Usuario no disponible.");
        if (!RolValido(usuario))
            return ResultadoReserva<DisponibilidadResponseDto>.Falla(MotivoResultadoReserva.Prohibido, "Rol no permitido.");
        if (!IntervaloValido(inicio, fin) || inicio < Ahora)
            return ResultadoReserva<DisponibilidadResponseDto>.Falla(MotivoResultadoReserva.DatosInvalidos, "Use un intervalo futuro válido en UTC.");
        if (!await db.Espacios.AnyAsync(e => e.Id == espacioId && e.Activo))
            return ResultadoReserva<DisponibilidadResponseDto>.Falla(MotivoResultadoReserva.NoEncontrado, "Espacio inexistente o inactivo.");
        return ResultadoReserva<DisponibilidadResponseDto>.Ok(new(espacioId, !await HayConflictoAsync(espacioId, inicio, fin)));
    }

    public async Task<ResultadoReserva<List<ReservaResponseDto>>> ObtenerMiasAsync(Guid usuarioId)
    {
        var usuario = await UsuarioValidoAsync(usuarioId);
        if (usuario is null)
            return ResultadoReserva<List<ReservaResponseDto>>.Falla(MotivoResultadoReserva.NoAutorizado, "Usuario no disponible.");
        if (!RolValido(usuario))
            return ResultadoReserva<List<ReservaResponseDto>>.Falla(MotivoResultadoReserva.Prohibido, "Rol no permitido.");
        var reservas = await db.Reservas.AsNoTracking().Where(r => r.UsuarioId == usuarioId)
            .OrderBy(r => r.FechaInicio).Select(r => new ReservaResponseDto(
                r.Id, r.EspacioId, r.FechaInicio, r.FechaFin, r.Estado)).ToListAsync();
        return ResultadoReserva<List<ReservaResponseDto>>.Ok(reservas);
    }

    public async Task<ResultadoReserva<ReservaResponseDto>> CrearAsync(Guid usuarioId, CrearReservaDto? request)
    {
        var usuario = await UsuarioValidoAsync(usuarioId);
        if (usuario is null) return Falla(MotivoResultadoReserva.NoAutorizado, "Usuario no disponible.");
        if (!RolValido(usuario)) return Falla(MotivoResultadoReserva.Prohibido, "Rol no permitido.");
        // Consulta aislada: la autorización procede del módulo real de Gestión de permisos.
        if (request is null || !IntervaloValido(request.FechaInicio, request.FechaFin) || request.FechaInicio < Ahora)
            return Falla(MotivoResultadoReserva.DatosInvalidos, "Use un intervalo futuro válido en UTC.");

        return await PersistirAsync(async () =>
        {
            if (!await db.Espacios.AnyAsync(e => e.Id == request.EspacioId && e.Activo))
                return Falla(MotivoResultadoReserva.NoEncontrado, "Espacio inexistente o inactivo.");
            if (await HayConflictoAsync(request.EspacioId, request.FechaInicio, request.FechaFin))
                return Falla(MotivoResultadoReserva.Conflicto, "El intervalo ya está ocupado.");
            var reserva = new Reserva
            {
                UsuarioId = usuarioId, EspacioId = request.EspacioId,
                FechaInicio = request.FechaInicio, FechaFin = request.FechaFin
            };
            db.Reservas.Add(reserva);
            await db.SaveChangesAsync();
            return ResultadoReserva<ReservaResponseDto>.Ok(ToDto(reserva));
        });
    }

    public async Task<ResultadoReserva<ReservaResponseDto>> ConfirmarAsync(Guid usuarioId, int id)
    {
        var usuario = await UsuarioValidoAsync(usuarioId);
        if (usuario is null) return Falla(MotivoResultadoReserva.NoAutorizado, "Usuario no disponible.");
        if (usuario.Rol != Rol.Administrador) return Falla(MotivoResultadoReserva.Prohibido, "Solo un Administrador puede confirmar.");
        return await PersistirAsync(async () =>
        {
            var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id);
            if (reserva is null) return Falla(MotivoResultadoReserva.NoEncontrado, "Reserva no encontrada.");
            // Condición de confirmación identificable para el defecto 3.
            if (!await CumpleCondicionesConfirmacionAsync(reserva))
                return Falla(MotivoResultadoReserva.Conflicto, "La reserva no cumple las condiciones de confirmación.");
            return await CambiarEstadoAsync(reserva, EstadoReserva.Confirmada);
        });
    }

    private async Task<bool> CumpleCondicionesConfirmacionAsync(Reserva reserva) =>
        reserva.FechaInicio < reserva.FechaFin && reserva.FechaInicio >= Ahora
        && await db.Espacios.AnyAsync(e => e.Id == reserva.EspacioId && e.Activo)
        && !await HayConflictoAsync(reserva.EspacioId, reserva.FechaInicio, reserva.FechaFin, reserva.Id);

    public async Task<ResultadoReserva<ReservaResponseDto>> CancelarAsync(Guid usuarioId, int id)
    {
        var usuario = await UsuarioValidoAsync(usuarioId);
        if (usuario is null) return Falla(MotivoResultadoReserva.NoAutorizado, "Usuario no disponible.");
        if (!RolValido(usuario)) return Falla(MotivoResultadoReserva.Prohibido, "Rol no permitido.");
        return await PersistirAsync(async () =>
        {
            var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id);
            if (reserva is null || (usuario.Rol != Rol.Administrador && reserva.UsuarioId != usuarioId))
                return Falla(MotivoResultadoReserva.NoEncontrado, "Reserva no encontrada.");
            return await CambiarEstadoAsync(reserva, EstadoReserva.Cancelada);
        });
    }

    // Operación interna, deliberadamente sin controlador ni endpoint.
    internal async Task<ResultadoReserva<ReservaResponseDto>> CompletarAsync(int id)
    {
        return await PersistirAsync(async () =>
        {
            var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id);
            if (reserva is null) return Falla(MotivoResultadoReserva.NoEncontrado, "Reserva no encontrada.");
            if (reserva.FechaFin > Ahora)
                return Falla(MotivoResultadoReserva.Conflicto, "El intervalo todavía no ha finalizado.");
            return await CambiarEstadoAsync(reserva, EstadoReserva.Completada);
        });
    }

    private async Task<ResultadoReserva<ReservaResponseDto>> CambiarEstadoAsync(Reserva reserva, EstadoReserva destino)
    {
        try { TransicionesReserva.AsegurarPermitida(reserva.Estado, destino); }
        catch (InvalidOperationException) { return Falla(MotivoResultadoReserva.Conflicto, "Transición no permitida."); }
        reserva.Estado = destino;
        await db.SaveChangesAsync();
        return ResultadoReserva<ReservaResponseDto>.Ok(ToDto(reserva));
    }

    private async Task<ResultadoReserva<ReservaResponseDto>> PersistirAsync(
        Func<Task<ResultadoReserva<ReservaResponseDto>>> operacion)
    {
        // SERIALIZABLE mantiene la comprobación de solapamiento y escritura atómicas en SQL Server.
        await using IDbContextTransaction? transaccion = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        try
        {
            var resultado = await operacion();
            if (resultado.Exito && transaccion is not null) await transaccion.CommitAsync();
            return resultado;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
        {
            if (transaccion is not null) await transaccion.RollbackAsync();
            foreach (var entry in db.ChangeTracker.Entries<Reserva>().ToList())
            {
                if (entry.State == EntityState.Added) entry.State = EntityState.Detached;
                else if (entry.State == EntityState.Modified)
                {
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entry.State = EntityState.Unchanged;
                }
            }
            return Falla(MotivoResultadoReserva.ErrorPersistencia, "No se pudo guardar la reserva; reintente la operación.");
        }
    }

    private static ReservaResponseDto ToDto(Reserva r) => new(r.Id, r.EspacioId, r.FechaInicio, r.FechaFin, r.Estado);
    private static ResultadoReserva<ReservaResponseDto> Falla(MotivoResultadoReserva motivo, string mensaje) =>
        ResultadoReserva<ReservaResponseDto>.Falla(motivo, mensaje);
}

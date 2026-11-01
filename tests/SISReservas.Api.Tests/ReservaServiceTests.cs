using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.DTOs.Reservas;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class ReservaServiceTests
{
    private sealed class RelojFijo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class Escenario : IDisposable
    {
        private readonly TestDatabase database = new();
        public SISReservasDbContext Db { get; }
        public ReservaService Servicio { get; }
        public Usuario Usuario { get; private set; } = null!;
        public Usuario Admin { get; private set; } = null!;
        public Espacio Espacio { get; private set; } = null!;
        public DateTime Inicio => new(2030, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        public Escenario()
        {
            Db = database.CreateContext();
            Servicio = new(Db, new PermisoService(Db), new RelojFijo());
        }
        public async Task PrepararAsync(bool permiso = true)
        {
            Usuario = await TestData.AgregarUsuarioAsync(Db);
            Admin = await TestData.AgregarUsuarioAsync(Db, Rol.Administrador);
            Espacio = new() { Nombre = "Salón A", Tipo = TipoEspacio.Salon };
            Db.Espacios.Add(Espacio);
            await Db.SaveChangesAsync();
            if (permiso) await TestData.AgregarPermisoAsync(Db, Usuario, Permiso.ReservarEspacios);
        }
        public CrearReservaDto Solicitud(int desplazamiento = 0) => new()
        {
            EspacioId = Espacio.Id, FechaInicio = Inicio.AddHours(desplazamiento), FechaFin = Inicio.AddHours(desplazamiento + 1)
        };
        public async Task<Reserva> AgregarAsync(EstadoReserva estado = EstadoReserva.Pendiente, bool pasada = false)
        {
            var reserva = new Reserva { UsuarioId = Usuario.Id, EspacioId = Espacio.Id,
                FechaInicio = pasada ? Inicio.AddDays(-3) : Inicio,
                FechaFin = pasada ? Inicio.AddDays(-3).AddHours(1) : Inicio.AddHours(1), Estado = estado };
            Db.Reservas.Add(reserva);
            await Db.SaveChangesAsync();
            return reserva;
        }
        public void Dispose() => database.Dispose();
    }

    [Fact]
    public async Task Crear_IntervaloValido_PersistePendiente()
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync();
        // Act
        var resultado = await e.Servicio.CrearAsync(e.Usuario.Id, e.Solicitud());
        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoReserva.Pendiente, (await e.Db.Reservas.SingleAsync()).Estado);
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(-48)] [InlineData(999)]
    public async Task Crear_FechasInvalidas_RechazaSinPersistir(int caso)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync();
        var request = e.Solicitud();
        if (caso == 999) request.FechaInicio = DateTime.SpecifyKind(request.FechaInicio, DateTimeKind.Unspecified);
        else if (caso == -48) { request.FechaInicio = e.Inicio.AddHours(caso); request.FechaFin = request.FechaInicio.AddHours(1); }
        else request.FechaFin = request.FechaInicio.AddHours(caso);
        // Act
        var resultado = await e.Servicio.CrearAsync(e.Usuario.Id, request);
        // Assert
        Assert.Equal(MotivoResultadoReserva.DatosInvalidos, resultado.Motivo);
        Assert.Empty(e.Db.Reservas);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Crear_EspacioInactivoOInexistente_Rechaza(bool inexistente)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync();
        e.Espacio.Activo = false; await e.Db.SaveChangesAsync();
        var request = e.Solicitud(); if (inexistente) request.EspacioId = 999;
        // Act
        var resultado = await e.Servicio.CrearAsync(e.Usuario.Id, request);
        // Assert
        Assert.Equal(MotivoResultadoReserva.NoEncontrado, resultado.Motivo);
        Assert.Empty(e.Db.Reservas);
    }

    [Theory]
    [InlineData(EstadoReserva.Pendiente)] [InlineData(EstadoReserva.Confirmada)]
    public async Task Crear_Solapamiento_Rechaza(EstadoReserva estado)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); await e.AgregarAsync(estado);
        var request = e.Solicitud(); request.FechaInicio = request.FechaInicio.AddMinutes(30);
        // Act
        var resultado = await e.Servicio.CrearAsync(e.Usuario.Id, request);
        // Assert
        Assert.Equal(MotivoResultadoReserva.Conflicto, resultado.Motivo);
        Assert.Single(e.Db.Reservas);
    }

    [Theory]
    [InlineData(-1)] [InlineData(1)]
    public async Task Crear_IntervaloAdyacente_Permite(int desplazamiento)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); await e.AgregarAsync();
        // Act
        var resultado = await e.Servicio.CrearAsync(e.Usuario.Id, e.Solicitud(desplazamiento));
        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(2, await e.Db.Reservas.CountAsync());
    }

    [Theory]
    [InlineData(EstadoReserva.Cancelada)] [InlineData(EstadoReserva.Completada)]
    public async Task Crear_ReservaTerminal_NoBloqueaHorario(EstadoReserva estado)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); await e.AgregarAsync(estado);
        // Act
        var resultado = await e.Servicio.CrearAsync(e.Usuario.Id, e.Solicitud());
        // Assert
        Assert.True(resultado.Exito);
    }

    [Fact]
    public async Task Crear_EstandarSinPermiso_Rechaza()
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(false);
        // Act
        var resultado = await e.Servicio.CrearAsync(e.Usuario.Id, e.Solicitud());
        // Assert
        Assert.Equal(MotivoResultadoReserva.Prohibido, resultado.Motivo);
        Assert.Empty(e.Db.Reservas);
    }

    [Fact]
    public async Task Crear_AdministradorSinConcesion_Permite()
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(false);
        // Act
        var resultado = await e.Servicio.CrearAsync(e.Admin.Id, e.Solicitud());
        // Assert
        Assert.True(resultado.Exito);
    }

    [Fact]
    public async Task Confirmar_AdministradorYCondicionesValidas_ConfirmaExcluyendoPropiaReserva()
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); var r = await e.AgregarAsync();
        // Act
        var resultado = await e.Servicio.ConfirmarAsync(e.Admin.Id, r.Id);
        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoReserva.Confirmada, r.Estado);
    }

    [Theory]
    [InlineData("inactivo")] [InlineData("fechas")] [InlineData("conflicto")] [InlineData("pasado")]
    public async Task Confirmar_CondicionIncumplida_RechazaSinCambiarEstado(string condicion)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync();
        var r = await e.AgregarAsync(pasada: condicion == "pasado");
        if (condicion == "inactivo") e.Espacio.Activo = false;
        if (condicion == "fechas") r.FechaFin = r.FechaInicio;
        if (condicion == "conflicto") await e.AgregarAsync();
        await e.Db.SaveChangesAsync();
        // Act
        var resultado = await e.Servicio.ConfirmarAsync(e.Admin.Id, r.Id);
        // Assert
        Assert.Equal(MotivoResultadoReserva.Conflicto, resultado.Motivo);
        Assert.Equal(EstadoReserva.Pendiente, r.Estado);
        Assert.False(e.Db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task Confirmar_Estandar_RechazaAunqueSeaPropietario()
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); var r = await e.AgregarAsync();
        // Act
        var resultado = await e.Servicio.ConfirmarAsync(e.Usuario.Id, r.Id);
        // Assert
        Assert.Equal(MotivoResultadoReserva.Prohibido, resultado.Motivo);
        Assert.Equal(EstadoReserva.Pendiente, r.Estado);
    }

    [Theory]
    [InlineData(EstadoReserva.Cancelada, true)] [InlineData(EstadoReserva.Completada, true)]
    [InlineData(EstadoReserva.Cancelada, false)] [InlineData(EstadoReserva.Completada, false)]
    public async Task Transicion_DesdeTerminal_RechazaConEstadoPersistidoIntacto(EstadoReserva estado, bool confirmar)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); var r = await e.AgregarAsync(estado);
        // Act
        var resultado = confirmar ? await e.Servicio.ConfirmarAsync(e.Admin.Id, r.Id)
            : await e.Servicio.CancelarAsync(e.Usuario.Id, r.Id);
        // Assert
        Assert.Equal(MotivoResultadoReserva.Conflicto, resultado.Motivo);
        await e.Db.Entry(r).ReloadAsync();
        Assert.Equal(estado, r.Estado);
    }

    [Theory]
    [InlineData(EstadoReserva.Pendiente, false)] [InlineData(EstadoReserva.Confirmada, false)]
    [InlineData(EstadoReserva.Pendiente, true)] [InlineData(EstadoReserva.Confirmada, true)]
    public async Task Cancelar_PropietarioOAdministrador_Cancela(EstadoReserva estado, bool admin)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); var r = await e.AgregarAsync(estado);
        // Act
        var resultado = await e.Servicio.CancelarAsync(admin ? e.Admin.Id : e.Usuario.Id, r.Id);
        // Assert
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoReserva.Cancelada, r.Estado);
    }

    [Fact]
    public async Task Cancelar_OtroEstandar_NoRevelaNiModificaReserva()
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); var r = await e.AgregarAsync();
        var otro = await TestData.AgregarUsuarioAsync(e.Db);
        // Act
        var resultado = await e.Servicio.CancelarAsync(otro.Id, r.Id);
        // Assert
        Assert.Equal(MotivoResultadoReserva.NoEncontrado, resultado.Motivo);
        Assert.Equal(EstadoReserva.Pendiente, r.Estado);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Completar_SoloPermiteIntervaloFinalizado(bool pasada)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); var r = await e.AgregarAsync(EstadoReserva.Confirmada, pasada);
        // Act
        var resultado = await e.Servicio.CompletarAsync(r.Id);
        // Assert
        Assert.Equal(pasada, resultado.Exito);
        Assert.Equal(pasada ? EstadoReserva.Completada : EstadoReserva.Confirmada, r.Estado);
    }

    [Theory]
    [InlineData(EstadoReserva.Pendiente)] [InlineData(EstadoReserva.Cancelada)] [InlineData(EstadoReserva.Completada)]
    public async Task Completar_EstadoNoPermitido_NoModifica(EstadoReserva estado)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); var r = await e.AgregarAsync(estado, true);
        // Act
        var resultado = await e.Servicio.CompletarAsync(r.Id);
        // Assert
        Assert.Equal(MotivoResultadoReserva.Conflicto, resultado.Motivo);
        Assert.Equal(estado, r.Estado);
    }

    [Fact]
    public async Task ObtenerMias_NoIncluyeReservasAjenas()
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); var propia = await e.AgregarAsync();
        var ajena = await e.AgregarAsync(); ajena.UsuarioId = e.Admin.Id; await e.Db.SaveChangesAsync();
        // Act
        var resultado = await e.Servicio.ObtenerMiasAsync(e.Usuario.Id);
        // Assert
        Assert.Equal(propia.Id, Assert.Single(resultado.Valor!).Id);
    }

    [Fact]
    public async Task ListarEspacios_SoloDevuelveActivos()
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync();
        e.Db.Espacios.Add(new() { Nombre = "Inactivo", Tipo = TipoEspacio.Equipo, Activo = false }); await e.Db.SaveChangesAsync();
        // Act
        var resultado = await e.Servicio.ListarEspaciosAsync(e.Usuario.Id);
        // Assert
        Assert.Equal(e.Espacio.Id, Assert.Single(resultado.Valor!).Id);
    }

    [Theory]
    [InlineData(0, false)] [InlineData(1, true)]
    public async Task Disponibilidad_ConsultaIntervaloSinRevelarDatosPersonales(int desplazamiento, bool disponible)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync(); await e.AgregarAsync(); var request = e.Solicitud(desplazamiento);
        // Act
        var resultado = await e.Servicio.ConsultarDisponibilidadAsync(e.Usuario.Id, e.Espacio.Id, request.FechaInicio, request.FechaFin);
        // Assert
        Assert.Equal(disponible, resultado.Valor!.Disponible);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Operaciones_UsuarioInactivoORolInvalido_Rechazan(bool rolInvalido)
    {
        // Arrange
        using var e = new Escenario(); await e.PrepararAsync();
        if (rolInvalido) e.Usuario.Rol = (Rol)999; else e.Usuario.Activo = false;
        await e.Db.SaveChangesAsync(); var request = e.Solicitud();
        var esperado = rolInvalido ? MotivoResultadoReserva.Prohibido : MotivoResultadoReserva.NoAutorizado;
        // Act
        var resultados = new[] {
            (await e.Servicio.ListarEspaciosAsync(e.Usuario.Id)).Motivo,
            (await e.Servicio.ObtenerMiasAsync(e.Usuario.Id)).Motivo,
            (await e.Servicio.ConsultarDisponibilidadAsync(e.Usuario.Id, e.Espacio.Id, request.FechaInicio, request.FechaFin)).Motivo,
            (await e.Servicio.CrearAsync(e.Usuario.Id, request)).Motivo,
            (await e.Servicio.ConfirmarAsync(e.Usuario.Id, 1)).Motivo,
            (await e.Servicio.CancelarAsync(e.Usuario.Id, 1)).Motivo };
        // Assert
        Assert.All(resultados, motivo => Assert.Equal(esperado, motivo));
    }
}

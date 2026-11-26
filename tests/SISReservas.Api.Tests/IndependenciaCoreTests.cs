using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using System.Diagnostics;

namespace SISReservas.Api.Tests;

public class IndependenciaCoreTests
{
    [Fact]
    public void H03_SeparacionDeReservas_NoCambiaModeloDeMigraciones()
    {
        // Arrange: proveedor SQL real para comparar metadatos; no se abre conexión.
        var opciones = new DbContextOptionsBuilder<SISReservasDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=ModeloSinConexion;Integrated Security=True;TrustServerCertificate=True;")
            .Options;
        using var db = new SISReservasDbContext(opciones);
        // Act
        var cambios = db.Database.HasPendingModelChanges();
        // Assert
        Assert.False(cambios);
    }

    [Fact]
    public async Task H03_CoreOnly_CompilaHostRealSinReservasConMigraciones()
    {
        // Arrange: fuentes públicas; se mantiene Program y todas las migraciones reales.
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz is not null && !File.Exists(Path.Combine(raiz.FullName, "SISReservasEspacios.slnx"))) raiz = raiz.Parent;
        Assert.NotNull(raiz);
        var origen = Path.Combine(raiz.FullName, "backend", "SISReservas.Api");
        var destino = Path.Combine(Path.GetTempPath(), "SISReservas-H03-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destino);
        foreach (var archivo in Directory.EnumerateFiles(origen, "*", SearchOption.AllDirectories))
        {
            var relativo = Path.GetRelativePath(origen, archivo);
            var partes = relativo.Split(Path.DirectorySeparatorChar);
            if (partes.Contains("bin") || partes.Contains("obj")) continue;
            if (Path.GetExtension(archivo) is not (".cs" or ".csproj")) continue;
            var copia = Path.Combine(destino, relativo);
            Directory.CreateDirectory(Path.GetDirectoryName(copia)!);
            File.Copy(archivo, copia);
        }
        var publicado = Path.Combine(destino, "publicado");
        var inicio = new ProcessStartInfo("dotnet") { WorkingDirectory = destino, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argumento in new[] { "publish", "SISReservas.Api.csproj", "-c", "CoreOnly", "-o", publicado, "--verbosity", "quiet", "--property:NuGetAudit=false" }) inicio.ArgumentList.Add(argumento);

        // Act
        using var proceso = Process.Start(inicio)!;
        var stdout = proceso.StandardOutput.ReadToEndAsync();
        var stderr = proceso.StandardError.ReadToEndAsync();
        try { await proceso.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(2)); }
        catch { if (!proceso.HasExited) proceso.Kill(entireProcessTree: true); throw; }
        var salida = await stdout + await stderr;
        File.WriteAllText(Path.Combine(destino, "resultado.log"), salida);

        // Assert: el ejecutable real conserva Core y migraciones, pero ningún tipo de Reservas.
        // La ejecución HTTP/SQL/SMTP se verifica con Validar-Practica5.ps1 -CoreOnly.
        Assert.True(proceso.ExitCode == 0, $"Core no independiente. Evidencia: {destino}{Environment.NewLine}{salida}");
        using var assemblyStream = File.OpenRead(Path.Combine(publicado, "SISReservas.Api.dll"));
        using var pe = new System.Reflection.PortableExecutable.PEReader(assemblyStream);
        var metadata = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(pe);
        var tipos = metadata.TypeDefinitions.Select(h => metadata.GetString(metadata.GetTypeDefinition(h).Name)).ToArray();
        Assert.DoesNotContain("Reserva", tipos);
        Assert.DoesNotContain("Espacio", tipos);
        Assert.DoesNotContain("ReservaService", tipos);
        Assert.DoesNotContain("PlanificacionService", tipos);
        foreach (var tipo in new[]{"Program","AuthService","SolicitudPermisoService","DocumentoService","NotificacionService","ReporteService","SISReservasDbContextModelSnapshot"})
            Assert.Contains(tipo, tipos);
    }
}

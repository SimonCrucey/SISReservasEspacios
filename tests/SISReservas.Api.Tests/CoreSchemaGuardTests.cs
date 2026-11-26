using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SISReservas.Api.Data;
namespace SISReservas.Api.Tests;
public class CoreSchemaGuardTests
{
    [Fact]
    public void H03_Infraestructura_SoloAdmiteOmitirLasDosTablasDeNegocio()
    {
        // Arrange
        MigrationOperation[] cambios=[new DropTableOperation{Name="Espacios"},new DropTableOperation{Name="Reservas"}];
        // Act
        var error=Record.Exception(()=>CoreSchemaMigration.ValidateCoreView(cambios));
        // Assert
        Assert.Null(error);
    }
    [Fact]
    public void H03_Infraestructura_RechazaUnaDiferenciaInesperadaEnCore()
    {
        // Arrange
        MigrationOperation[] cambios=[new DropTableOperation{Name="Espacios"},new DropTableOperation{Name="Reservas"},new AlterColumnOperation{Name="Nombre",Table="Usuarios"}];
        // Act
        var error=Record.Exception(()=>CoreSchemaMigration.ValidateCoreView(cambios));
        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }
}

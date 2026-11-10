using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using SISReservas.Api.Data;
using SISReservas.Api.Security;
using SISReservas.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
    .AddAuthentication("Session")
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
        "Session",
        _ => { });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Session Token",
        In = ParameterLocation.Header,
        Description = "Escribe tu token de sesión."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});
builder.Services.AddDbContext<SISReservasDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailQueueService>();
builder.Services.AddScoped<EmailSenderService>();
builder.Services.AddHostedService<EmailDispatcherService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<UserAdministrationService>();
builder.Services.AddScoped<PasswordRecoveryService>();
builder.Services.AddScoped<PasswordPolicyService>();
builder.Services.AddScoped<PermisoService>();
builder.Services.AddScoped<SolicitudPermisoService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<DocumentoService>();
builder.Services.AddSingleton(new ClimaOptions(
    Environment.GetEnvironmentVariable("WEATHER_API_KEY") ?? "",
    Environment.GetEnvironmentVariable("WEATHER_LOCATION") ?? "18.4861,-69.9312"));
builder.Services.AddHttpClient<PlanificacionService>(client =>
{
    client.BaseAddress = new Uri("https://api.weatherapi.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(10);
}).RemoveAllLoggers();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<SISReservasDbContext>();
    var cancellationToken = app.Lifetime.ApplicationStopping;

    try
    {
        await database.Database.MigrateAsync(cancellationToken);
    }
    catch (SqlException error) when (error.Number == 1801)
    {
        // EF comprueba y crea el catálogo antes de adquirir su bloqueo de migraciones.
        // Otro proceso puede haberlo creado entre ambas operaciones.
        if (!await database.Database.CanConnectAsync(cancellationToken))
            throw new InvalidOperationException(
                "SQL Server informa que la base configurada existe, pero la API no puede abrirla. " +
                "Comprueba el estado del catálogo y los permisos de la conexión.", error);

        app.Logger.LogWarning("La base configurada ya existe y admite conexiones. Se reintentan las migraciones una vez.");
        await database.Database.MigrateAsync(cancellationToken);
    }
}

var adminArgument = Array.IndexOf(args, "--administrador-inicial");
if (adminArgument >= 0)
{
    if (adminArgument + 1 >= args.Length) throw new ArgumentException("Falta el correo del usuario activado.");
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<SISReservasDbContext>();
    var email = args[adminArgument + 1].Trim().ToLowerInvariant();
    await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
    var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Email == email && u.Activo);
    if (usuario is null) throw new InvalidOperationException("Primero registra y activa la cuenta por correo.");
    if (await db.Usuarios.AnyAsync(u => u.Rol == SISReservas.Api.Models.Enums.Rol.Administrador))
        throw new InvalidOperationException("Ya existe un Administrador. Usa la administración de usuarios.");
    usuario.Rol = SISReservas.Api.Models.Enums.Rol.Administrador;
    if (!await db.Espacios.AnyAsync())
    {
        db.Espacios.Add(new SISReservas.Api.Models.Espacio
        {
            Nombre = "Cancha de evaluación", Tipo = SISReservas.Api.Models.Enums.TipoEspacio.Cancha, Activo = true
        });
    }
    await scope.ServiceProvider.GetRequiredService<SessionService>().RevokeAllForUserAsync(usuario.Id);
    await db.SaveChangesAsync();
    await transaction.CommitAsync();
    app.Logger.LogInformation("Administrador inicial preparado.");
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    if (app.Configuration.GetValue<bool>("PanelLocal:Enabled"))
    {
        var panel = Path.Combine(app.Environment.ContentRootPath, "App_Data", "panel");
        if (Directory.Exists(panel))
        {
            app.MapGet("/panel", () => Results.Redirect("/panel/index.html"));
            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = "/panel",
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(panel)
            });
        }
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

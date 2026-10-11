using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asistencias.Api.Alumnos;
using Asistencias.Api.Autenticacion;
using Asistencias.Api.Catalogos;
using Asistencias.Api.Clases;
using Asistencias.Api.Comun;
using Asistencias.Api.Cursos;
using Asistencias.Api.Datos;
using Asistencias.Api.Laboratorios;
using Asistencias.Api.Usuarios;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AsistenciasDbContext>((sp, options) =>
{
    var cadena = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Falta la cadena de conexión 'ConnectionStrings:DefaultConnection'. " +
            "Configúrala con 'dotnet user-secrets' o con la variable de entorno ConnectionStrings__DefaultConnection.");
    options.UseMySql(cadena, new MariaDbServerVersion(new Version(11, 8))).UseSnakeCaseNamingConvention();
});

builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => Sesion.Configurar(options, builder.Environment.IsDevelopment()));
builder.Services.AddAuthorization(Politicas.Registrar);
builder.Services.AddHttpContextAccessor();

// Frena el adivinar contraseñas por fuerza bruta
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AutenticacionEndpoints.LimiteLogin, http =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = http.RequestServices.GetRequiredService<IConfiguration>().GetValue("Login:IntentosPorMinuto", 10),
                Window = TimeSpan.FromMinutes(1),
            }));
    // El registro de entrada no pide sesión; el límite frena el probar números de control al azar
    options.AddPolicy(ClasesEndpoints.LimiteRegistro, http =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = http.RequestServices.GetRequiredService<IConfiguration>().GetValue("Registro:IntentosPorMinuto", 120),
                Window = TimeSpan.FromMinutes(1),
            }));
});

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));
// Una petición mal formada responde 400 también en desarrollo, igual que en producción
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/api/salud", () => Results.Ok(new { estado = "ok" })).WithTags("Sistema");
app.MapAutenticacion();
app.MapUsuarios();
app.MapLaboratorios();
app.MapEquipos();
app.MapCarreras();
app.MapPeriodos();
app.MapMaterias();
app.MapAlumnos();
app.MapCursos();
app.MapPracticas();
app.MapClases();

await app.CrearAdminInicialAsync();

app.Run();

public partial class Program;

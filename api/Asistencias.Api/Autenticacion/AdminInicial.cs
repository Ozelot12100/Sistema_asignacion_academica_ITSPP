using Asistencias.Api.Datos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Autenticacion;

public static class AdminInicial
{
    public const int LongitudMinimaPassword = 12;

    // Crea el primer administrador a partir de la configuración. Si ya hay uno, no hace nada.
    public static async Task CrearAdminInicialAsync(this WebApplication app)
    {
        var email = app.Configuration["AdminInicial:Email"];
        var password = app.Configuration["AdminInicial:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AsistenciasDbContext>();
        if (await db.Usuarios.AnyAsync(u => u.Rol == Rol.Admin))
            return;

        if (password.Length < LongitudMinimaPassword)
            throw new InvalidOperationException(
                $"AdminInicial:Password debe tener al menos {LongitudMinimaPassword} caracteres.");

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var admin = new Usuario
        {
            Rol = Rol.Admin,
            Nombre = app.Configuration["AdminInicial:Nombre"] ?? "Administrador",
            Apellido = app.Configuration["AdminInicial:Apellido"] ?? "General",
            Email = email.Trim(),
        };
        admin.PasswordHash = hasher.HashPassword(admin, password);
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();

        app.Logger.LogInformation("Se creó el administrador inicial {Email}.", admin.Email);
    }
}

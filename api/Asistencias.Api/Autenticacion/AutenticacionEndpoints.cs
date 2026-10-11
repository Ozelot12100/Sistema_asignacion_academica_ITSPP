using System.Security.Claims;
using Asistencias.Api.Comun;
using Asistencias.Api.Datos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Autenticacion;

public record CredencialesDto(string? Email, string? Password);

public record CambioPasswordDto(string? Actual, string? Nueva);

public record UsuarioActualDto(int Id, string Nombre, string Apellido, string Email, Rol Rol);

public static class AutenticacionEndpoints
{
    public const string LimiteLogin = "login";

    public static void MapAutenticacion(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/auth").WithTags("Autenticación");

        grupo.MapPost("/login", Login).RequireRateLimiting(LimiteLogin).Produces<UsuarioActualDto>();

        grupo.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        grupo.MapGet("/yo", async (ClaimsPrincipal principal, AsistenciasDbContext db) =>
        {
            var usuario = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == principal.IdUsuario());
            return Results.Ok(ADto(usuario));
        }).RequireAuthorization().Produces<UsuarioActualDto>();

        grupo.MapPut("/password", async (
            CambioPasswordDto dto, ClaimsPrincipal principal, AsistenciasDbContext db, IPasswordHasher<Usuario> hasher) =>
        {
            var usuario = await db.Usuarios.SingleAsync(u => u.Id == principal.IdUsuario());

            var v = new Validador();
            v.Si(string.IsNullOrEmpty(dto.Actual)
                || hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Actual) == PasswordVerificationResult.Failed,
                "actual", "La contraseña actual no es correcta.");
            ReglasPassword.Validar(v, "nueva", dto.Nueva);
            if (v.Error is { } error) return error;

            usuario.PasswordHash = hasher.HashPassword(usuario, dto.Nueva!);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting(LimiteLogin);
    }

    private static async Task<IResult> Login(
        CredencialesDto dto, AsistenciasDbContext db, IPasswordHasher<Usuario> hasher, HttpContext http)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrEmpty(dto.Password))
            return Results.Problem(statusCode: 400, title: "Correo y contraseña son obligatorios.");

        var email = dto.Email.Trim();
        var usuario = await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email);

        // Misma respuesta si el correo no existe, la cuenta está desactivada o la contraseña es incorrecta,
        // para no revelar qué correos están registrados
        if (usuario is null || !usuario.Activo
            || hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            return Results.Problem(statusCode: 401, title: "Correo o contraseña incorrectos.");

        await http.SignInAsync(Sesion.CrearPrincipal(usuario));
        return Results.Ok(ADto(usuario));
    }

    private static UsuarioActualDto ADto(Usuario u) => new(u.Id, u.Nombre, u.Apellido, u.Email, u.Rol);
}

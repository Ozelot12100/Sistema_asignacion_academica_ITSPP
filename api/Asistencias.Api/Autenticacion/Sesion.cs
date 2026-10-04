using System.Security.Claims;
using Asistencias.Api.Datos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Autenticacion;

public static class Sesion
{
    public static ClaimsPrincipal CrearPrincipal(Usuario usuario) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Role, NombreRol(usuario.Rol)),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme));

    public static string NombreRol(Rol rol) => rol.ToString().ToLowerInvariant();

    public static int IdUsuario(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static void Configurar(CookieAuthenticationOptions options, bool esDesarrollo)
    {
        options.Cookie.Name = "asistencias_sesion";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = esDesarrollo ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Es una API: sin sesión responde 401 y sin permiso 403, en lugar de redirigir a una página
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = ValidarQueSigueVigente;
    }

    // Si la cuenta se desactivó o cambió de rol, la sesión abierta deja de valer en la siguiente petición
    private static async Task ValidarQueSigueVigente(CookieValidatePrincipalContext ctx)
    {
        var db = ctx.HttpContext.RequestServices.GetRequiredService<AsistenciasDbContext>();
        var id = ctx.Principal!.IdUsuario();
        var usuario = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.Activo, u.Rol })
            .SingleOrDefaultAsync();

        if (usuario is null || !usuario.Activo || !ctx.Principal!.IsInRole(NombreRol(usuario.Rol)))
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync();
        }
    }
}

using System.Linq.Expressions;
using System.Security.Claims;
using Asistencias.Api.Autenticacion;
using Asistencias.Api.Comun;
using Asistencias.Api.Datos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Usuarios;

public record UsuarioDto(
    int Id, Rol Rol, string? NumeroEmpleado, string Nombre, string Apellido, string Email,
    string? Departamento, bool Activo, DateTime CreadoEn);

public record UsuarioEntrada(
    Rol? Rol, string? Nombre, string? Apellido, string? Email, string? NumeroEmpleado,
    string? Departamento, bool? Activo, string? Password);

public record PasswordEntrada(string? Password);

public static class UsuariosEndpoints
{
    private static readonly Expression<Func<Usuario, UsuarioDto>> ADto = u =>
        new UsuarioDto(u.Id, u.Rol, u.NumeroEmpleado, u.Nombre, u.Apellido, u.Email, u.Departamento, u.Activo, u.CreadoEn);

    public static void MapUsuarios(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/usuarios").WithTags("Usuarios").RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapGet("/", async (string? rol, bool? activo, AsistenciasDbContext db) =>
        {
            // El rol llega en minúsculas, como en el JSON; el enlace automático de enums distingue mayúsculas
            Rol? filtro = null;
            if (rol is not null)
            {
                if (!Enum.TryParse<Rol>(rol, ignoreCase: true, out var valor) || !Enum.IsDefined(valor))
                    return Results.Problem(statusCode: 400, title: "El rol indicado no existe.");
                filtro = valor;
            }

            return Results.Ok(await db.Usuarios.AsNoTracking()
                .Where(u => (filtro == null || u.Rol == filtro) && (activo == null || u.Activo == activo))
                .OrderBy(u => u.Apellido).ThenBy(u => u.Nombre)
                .Select(ADto)
                .ToListAsync());
        });

        grupo.MapPost("/", async (UsuarioEntrada entrada, AsistenciasDbContext db, IPasswordHasher<Usuario> hasher) =>
        {
            var v = new Validador();
            var usuario = new Usuario { Nombre = "", Apellido = "", Email = "" };
            Aplicar(usuario, entrada, v);
            ReglasPassword.Validar(v, "password", entrada.Password);
            if (v.Error is { } error) return error;

            usuario.PasswordHash = hasher.HashPassword(usuario, entrada.Password!);
            db.Usuarios.Add(usuario);
            if (await db.GuardarAsync() is { } conflicto) return conflicto;

            return Results.Created($"/api/usuarios/{usuario.Id}", ADto.Compile()(usuario));
        });

        grupo.MapPut("/{id:int}", async (int id, UsuarioEntrada entrada, AsistenciasDbContext db) =>
        {
            var usuario = await db.Usuarios.FindAsync(id);
            if (usuario is null) return Persistencia.NoExiste("El usuario");

            var eraAdminActivo = usuario is { Rol: Rol.Admin, Activo: true };
            var v = new Validador();
            Aplicar(usuario, entrada, v);
            if (v.Error is { } error) return error;

            // Sin esta regla el sistema podría quedarse sin nadie que administre las cuentas
            if (eraAdminActivo && usuario is not { Rol: Rol.Admin, Activo: true }
                && !await db.Usuarios.AnyAsync(u => u.Id != id && u.Rol == Rol.Admin && u.Activo))
                return Results.Problem(statusCode: 409,
                    title: "No se puede desactivar ni cambiar de rol al único administrador activo.");

            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Ok(ADto.Compile()(usuario));
        });

        grupo.MapPut("/{id:int}/password", async (
            int id, PasswordEntrada entrada, AsistenciasDbContext db, IPasswordHasher<Usuario> hasher) =>
        {
            var usuario = await db.Usuarios.FindAsync(id);
            if (usuario is null) return Persistencia.NoExiste("El usuario");

            var v = new Validador();
            ReglasPassword.Validar(v, "password", entrada.Password);
            if (v.Error is { } error) return error;

            usuario.PasswordHash = hasher.HashPassword(usuario, entrada.Password!);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static void Aplicar(Usuario usuario, UsuarioEntrada entrada, Validador v)
    {
        v.Si(entrada.Rol is null, "rol", "Es obligatorio.");
        usuario.Rol = entrada.Rol ?? usuario.Rol;
        usuario.Nombre = v.Texto("nombre", entrada.Nombre, 100);
        usuario.Apellido = v.Texto("apellido", entrada.Apellido, 100);
        usuario.Email = v.Email("email", entrada.Email);
        usuario.NumeroEmpleado = v.Opcional("numeroEmpleado", entrada.NumeroEmpleado, 20);
        usuario.Departamento = v.Opcional("departamento", entrada.Departamento, 100);
        usuario.Activo = entrada.Activo ?? usuario.Activo;
    }
}

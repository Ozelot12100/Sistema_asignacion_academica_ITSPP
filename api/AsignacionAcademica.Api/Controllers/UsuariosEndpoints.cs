using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Dapper;
using System.Text.RegularExpressions;

namespace AsignacionAcademica.Api.Controllers
{
    public static class UsuariosEndpoints
    {
        private static readonly Regex EmailRegex = new(@"^[\w\-.]+@([\w\-]+\.)+[\w\-]{2,}$");

        public static void MapUsuarios(this WebApplication app)
        {
            app.MapGet("/usuarios", async (ConexionDb conexion) =>
            {
                using var db = conexion.Abrir();
                return Results.Ok(await db.QueryAsync("SELECT id,nombre,username,correo,rol FROM Usuarios"));
            });

            app.MapPost("/usuarios", async (UsuarioDto dto, ConexionDb conexion) =>
            {
                if (!EsValido(dto))
                    return Results.BadRequest("Datos de usuario inválidos.");

                using var db = conexion.Abrir();
                var id = await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO Usuarios (nombre,username,correo,contrasena,rol)
                    VALUES (@nombre,@username,@correo,@contrasena,@rol);
                    SELECT LAST_INSERT_ID();", dto);
                return Results.Created($"/usuarios/{id}", new { id });
            });

            app.MapPut("/usuarios/{id:int}", async (int id, UsuarioDto dto, ConexionDb conexion) =>
            {
                if (!EsValido(dto))
                    return Results.BadRequest("Datos de usuario inválidos.");

                using var db = conexion.Abrir();
                await db.ExecuteAsync(@"
                    UPDATE Usuarios
                    SET nombre=@nombre,username=@username,
                        correo=@correo,contrasena=@contrasena,rol=@rol
                    WHERE id=@Id",
                    new { dto.nombre, dto.username, dto.correo, dto.contrasena, dto.rol, Id = id });
                return Results.Ok("Usuario actualizado");
            });

            app.MapDelete("/usuarios/{id:int}", async (int id, ConexionDb conexion) =>
            {
                using var db = conexion.Abrir();
                await db.ExecuteAsync("DELETE FROM Usuarios WHERE id=@Id", new { Id = id });
                return Results.Ok("Usuario eliminado");
            });
        }

        private static bool EsValido(UsuarioDto dto) =>
            !string.IsNullOrWhiteSpace(dto.nombre)
            && !string.IsNullOrWhiteSpace(dto.username)
            && dto.correo is not null && EmailRegex.IsMatch(dto.correo)
            && !string.IsNullOrWhiteSpace(dto.contrasena) && dto.contrasena.Length >= 6
            && !string.IsNullOrWhiteSpace(dto.rol);
    }
}

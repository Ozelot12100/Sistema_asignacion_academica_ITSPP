using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Dapper;

namespace AsignacionAcademica.Api.Controllers
{
    public static class UsuariosEndpoints
    {
        public static void MapUsuarios(this WebApplication app)
        {
            app.MapGet("/usuarios", async () =>
            {
                using var db = Database.GetConnection();
                return Results.Ok(await db.QueryAsync("SELECT * FROM Usuarios"));
            });

            app.MapPost("/usuarios", async (UsuarioDto dto) =>
            {
                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[\\w\\-.]+@([\\w\\-]+\\.)+[\\w\\-]{2,}$");
                if (string.IsNullOrWhiteSpace(dto.nombre)
                    || string.IsNullOrWhiteSpace(dto.username)
                    || !emailRegex.IsMatch(dto.correo)
                    || string.IsNullOrWhiteSpace(dto.contrasena) || dto.contrasena.Length < 6
                    || string.IsNullOrWhiteSpace(dto.rol))
                    return Results.BadRequest("Datos de usuario inválidos.");

                using var db = Database.GetConnection();
                var id = await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO Usuarios (nombre,username,correo,contrasena,rol)
                    VALUES (@nombre,@username,@correo,@contrasena,@rol);
                    SELECT LAST_INSERT_ID();", dto);
                return Results.Created($"/usuarios/{id}", new { id });
            });

            app.MapPut("/usuarios/{id:int}", async (int id, UsuarioDto dto) =>
            {
                using var db = Database.GetConnection();
                await db.ExecuteAsync(@"
                    UPDATE Usuarios
                    SET nombre=@nombre,username=@username,
                        correo=@correo,contrasena=@contrasena,rol=@rol
                    WHERE id=@Id",
                    new { dto.nombre, dto.username, dto.correo, dto.contrasena, dto.rol, Id = id });
                return Results.Ok("Usuario actualizado");
            });

            app.MapDelete("/usuarios/{id:int}", async (int id) =>
            {
                using var db = Database.GetConnection();
                await db.ExecuteAsync("DELETE FROM Usuarios WHERE id=@Id", new { Id = id });
                return Results.Ok("Usuario eliminado");
            });
        }
    }
}

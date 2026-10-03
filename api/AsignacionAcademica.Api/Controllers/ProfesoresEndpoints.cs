using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Dapper;

namespace AsignacionAcademica.Api.Controllers
{
    public static class ProfesoresEndpoints
    {
        public static void MapProfesores(this WebApplication app)
        {
            app.MapGet("/profesores", async (ConexionDb conexion) =>
            {
                using var db = conexion.Abrir();
                var rows = await db.QueryAsync("SELECT * FROM Profesores");
                return Results.Ok(rows);
            });

            app.MapPost("/profesores", async (ProfesorDto dto, ConexionDb conexion) =>
            {
                if (string.IsNullOrWhiteSpace(dto.nombre)
                    || string.IsNullOrWhiteSpace(dto.especialidad)
                    || string.IsNullOrWhiteSpace(dto.titulo))
                    return Results.BadRequest("Todos los campos son obligatorios y deben ser texto.");

                using var db = conexion.Abrir();
                var sql = @"
                    INSERT INTO Profesores (nombre, especialidad, titulo)
                    VALUES (@nombre, @especialidad, @titulo);
                    SELECT LAST_INSERT_ID();";
                var id = await db.ExecuteScalarAsync<int>(sql, dto);
                return Results.Created($"/profesores/{id}", new { id, dto.nombre, dto.especialidad, dto.titulo });
            });

            app.MapPut("/profesores/{id:int}", async (int id, ProfesorDto dto, ConexionDb conexion) =>
            {
                if (string.IsNullOrWhiteSpace(dto.nombre)
                    || string.IsNullOrWhiteSpace(dto.especialidad)
                    || string.IsNullOrWhiteSpace(dto.titulo))
                    return Results.BadRequest("Todos los campos son obligatorios y deben ser texto.");

                using var db = conexion.Abrir();
                var sql = @"
                    UPDATE Profesores
                    SET nombre=@nombre, especialidad=@especialidad, titulo=@titulo
                    WHERE id=@Id;";
                await db.ExecuteAsync(sql, new { dto.nombre, dto.especialidad, dto.titulo, Id = id });
                return Results.Ok("Profesor actualizado");
            });

            app.MapDelete("/profesores/{id:int}", async (int id, ConexionDb conexion) =>
            {
                using var db = conexion.Abrir();
                var clases = await db.QueryAsync<int>(
                    "SELECT id FROM Clases WHERE profesorId=@Id", new { Id = id });
                if (clases.AsList().Count > 0)
                    return Results.BadRequest("No se puede eliminar el profesor porque está asignado a clases.");

                await db.ExecuteAsync("DELETE FROM ProfesorDisponibilidad WHERE profesorId=@Id", new { Id = id });
                await db.ExecuteAsync("DELETE FROM Profesores WHERE id=@Id", new { Id = id });
                return Results.Ok("Profesor eliminado");
            });
        }
    }
}

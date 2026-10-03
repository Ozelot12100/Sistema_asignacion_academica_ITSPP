using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Dapper;

namespace AsignacionAcademica.Api.Controllers
{
    public static class AulasEndpoints
    {
        public static void MapAulas(this WebApplication app)
        {
            app.MapGet("/aulas", async (ConexionDb conexion) =>
            {
                using var db = conexion.Abrir();
                return Results.Ok(await db.QueryAsync("SELECT * FROM Aulas"));
            });

            app.MapPost("/aulas", async (AulaDto dto, ConexionDb conexion) =>
            {
                if (string.IsNullOrWhiteSpace(dto.nombre)
                    || dto.capacidad <= 0
                    || string.IsNullOrWhiteSpace(dto.tipo))
                    return Results.BadRequest("Nombre, tipo válidos y capacidad > 0.");

                using var db = conexion.Abrir();
                var id = await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO Aulas(nombre,capacidad,tipo)
                    VALUES(@nombre,@capacidad,@tipo);
                    SELECT LAST_INSERT_ID();", dto);
                return Results.Created($"/aulas/{id}", new { id });
            });

            app.MapPut("/aulas/{id:int}", async (int id, AulaDto dto, ConexionDb conexion) =>
            {
                if (string.IsNullOrWhiteSpace(dto.nombre)
                    || dto.capacidad <= 0
                    || string.IsNullOrWhiteSpace(dto.tipo))
                    return Results.BadRequest("Nombre, tipo válidos y capacidad > 0.");

                using var db = conexion.Abrir();
                await db.ExecuteAsync(@"
                    UPDATE Aulas
                    SET nombre=@nombre, capacidad=@capacidad, tipo=@tipo
                    WHERE id=@Id",
                    new { dto.nombre, dto.capacidad, dto.tipo, Id = id });
                return Results.Ok("Aula actualizada");
            });

            app.MapDelete("/aulas/{id:int}", async (int id, ConexionDb conexion) =>
            {
                using var db = conexion.Abrir();
                var usada = await db.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM Clases WHERE aulaId=@Id", new { Id = id });
                if (usada != null)
                    return Results.BadRequest("No se puede eliminar: aula asignada a clases.");

                await db.ExecuteAsync("DELETE FROM Aulas WHERE id=@Id", new { Id = id });
                return Results.Ok("Aula eliminada");
            });
        }
    }
}

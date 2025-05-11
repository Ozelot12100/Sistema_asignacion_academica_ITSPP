using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Dapper;

namespace AsignacionAcademica.Api.Controllers
{
    public static class AsignaturasEndpoints
    {
        public static void MapAsignaturas(this WebApplication app)
        {
            app.MapGet("/asignaturas", async () =>
            {
                using var db = Database.GetConnection();
                return Results.Ok(await db.QueryAsync("SELECT * FROM Asignaturas"));
            });

            app.MapPost("/asignaturas", async (AsignaturaDto dto) =>
            {
                if (string.IsNullOrWhiteSpace(dto.nombre)
                    || dto.semestre < 1 || dto.semestre > 8
                    || dto.creditos <= 0)
                    return Results.BadRequest("Nombre texto, semestre 1–8, créditos > 0.");

                using var db = Database.GetConnection();
                var id = await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO Asignaturas(nombre,semestre,creditos)
                    VALUES(@nombre,@semestre,@creditos);
                    SELECT LAST_INSERT_ID();", dto);
                return Results.Created($"/asignaturas/{id}", new { id });
            });

            app.MapPut("/asignaturas/{id:int}", async (int id, AsignaturaDto dto) =>
            {
                if (string.IsNullOrWhiteSpace(dto.nombre)
                    || dto.semestre < 1 || dto.semestre > 8
                    || dto.creditos <= 0)
                    return Results.BadRequest("Nombre texto, semestre 1–8, créditos > 0.");

                using var db = Database.GetConnection();
                await db.ExecuteAsync(@"
                    UPDATE Asignaturas
                    SET nombre=@nombre, semestre=@semestre, creditos=@creditos
                    WHERE id=@Id",
                    new { dto.nombre, dto.semestre, dto.creditos, Id = id });
                return Results.Ok("Asignatura actualizada");
            });

            app.MapDelete("/asignaturas/{id:int}", async (int id) =>
            {
                using var db = Database.GetConnection();
                var usada = await db.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM Clases WHERE asignaturaId=@Id", new { Id = id });
                if (usada != null)
                    return Results.BadRequest("No se puede eliminar: asignatura en uso.");

                await db.ExecuteAsync("DELETE FROM Asignaturas WHERE id=@Id", new { Id = id });
                return Results.Ok("Asignatura eliminada");
            });
        }
    }
}

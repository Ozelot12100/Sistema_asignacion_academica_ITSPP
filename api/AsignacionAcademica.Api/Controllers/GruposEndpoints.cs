using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Dapper;

namespace AsignacionAcademica.Api.Controllers
{
    public static class GruposEndpoints
    {
        public static void MapGrupos(this WebApplication app)
        {
            app.MapGet("/grupos", async (ConexionDb conexion) =>
            {
                using var db = conexion.Abrir();
                return Results.Ok(await db.QueryAsync("SELECT * FROM Grupos"));
            });

            app.MapPost("/grupos", async (GrupoDto dto, ConexionDb conexion) =>
            {
                if (string.IsNullOrWhiteSpace(dto.nombre)
                    || dto.numero_alumnos <= 0
                    || string.IsNullOrWhiteSpace(dto.curso)
                    || string.IsNullOrWhiteSpace(dto.turno)
                    || dto.semestre < 1 || dto.semestre > 8)
                    return Results.BadRequest("Todos los campos válidos y número de alumnos > 0.");

                using var db = conexion.Abrir();
                var id = await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO Grupos(nombre,numero_alumnos,curso,turno,semestre)
                    VALUES(@nombre,@numero_alumnos,@curso,@turno,@semestre);
                    SELECT LAST_INSERT_ID();", dto);
                return Results.Created($"/grupos/{id}", new { id });
            });

            app.MapPut("/grupos/{id:int}", async (int id, GrupoDto dto, ConexionDb conexion) =>
            {
                if (string.IsNullOrWhiteSpace(dto.nombre)
                    || dto.numero_alumnos <= 0
                    || string.IsNullOrWhiteSpace(dto.curso)
                    || string.IsNullOrWhiteSpace(dto.turno)
                    || dto.semestre < 1 || dto.semestre > 8)
                    return Results.BadRequest("Todos los campos válidos y número de alumnos > 0.");

                using var db = conexion.Abrir();
                await db.ExecuteAsync(@"
                    UPDATE Grupos
                    SET nombre=@nombre, numero_alumnos=@numero_alumnos,
                        curso=@curso, turno=@turno, semestre=@semestre
                    WHERE id=@Id",
                    new { dto.nombre, dto.numero_alumnos, dto.curso, dto.turno, dto.semestre, Id = id });
                return Results.Ok("Grupo actualizado");
            });

            app.MapDelete("/grupos/{id:int}", async (int id, ConexionDb conexion) =>
            {
                using var db = conexion.Abrir();
                var usado = await db.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM Clases WHERE grupoId=@Id", new { Id = id });
                if (usado != null)
                    return Results.BadRequest("No se puede eliminar: grupo con clases asignadas.");

                await db.ExecuteAsync("DELETE FROM Grupos WHERE id=@Id", new { Id = id });
                return Results.Ok("Grupo eliminado");
            });
        }
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Dapper;
using System.Text.RegularExpressions;

namespace AsignacionAcademica.Api.Controllers
{
    public static class ClasesEndpoints
    {
        public static void MapClases(this WebApplication app)
        {
            var horaRegex = new Regex(@"^([01]\d|2[0-3]):[0-5]\d$");

            app.MapGet("/clases", async () =>
            {
                using var db = Database.GetConnection();
                return Results.Ok(await db.QueryAsync("SELECT * FROM Clases"));
            });

            app.MapPost("/clases", async (ClaseDto dto) =>
            {
                var error = await ValidarClase(dto, horaRegex);
                if (error is not null) return error;

                using var db = Database.GetConnection();
                var id = await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO Clases(asignaturaId,profesorId,aulaId,grupoId,dia,horaInicio,horaFin)
                    VALUES(@asignaturaId,@profesorId,@aulaId,@grupoId,@dia,@horaInicio,@horaFin);
                    SELECT LAST_INSERT_ID();", dto);
                return Results.Created($"/clases/{id}", new { id });
            });

            app.MapPut("/clases/{id:int}", async (int id, ClaseDto dto) =>
            {
                var error = await ValidarClase(dto, horaRegex, id);
                if (error is not null) return error;

                using var db = Database.GetConnection();
                await db.ExecuteAsync(@"
                    UPDATE Clases
                    SET asignaturaId=@asignaturaId, profesorId=@profesorId, aulaId=@aulaId,
                        grupoId=@grupoId, dia=@dia, horaInicio=@horaInicio, horaFin=@horaFin
                    WHERE id=@id", new { dto.asignaturaId, dto.profesorId, dto.aulaId, dto.grupoId, dto.dia, dto.horaInicio, dto.horaFin, id });
                return Results.Ok("Clase actualizada");
            });

            app.MapDelete("/clases/{id:int}", async (int id) =>
            {
                using var db = Database.GetConnection();
                await db.ExecuteAsync("DELETE FROM Clases WHERE id=@Id", new { Id = id });
                return Results.Ok("Clase eliminada");
            });
        }

        private static async Task<IResult?> ValidarClase(ClaseDto dto, Regex horaRegex, int? id = null)
        {
            if (dto.asignaturaId <= 0 || dto.profesorId <= 0 || dto.aulaId <= 0
                || dto.grupoId <= 0 || string.IsNullOrWhiteSpace(dto.dia)
                || !horaRegex.IsMatch(dto.horaInicio) || !horaRegex.IsMatch(dto.horaFin))
                return Results.BadRequest("Campos inválidos o formato de hora incorrecto.");

            var hi = int.Parse(dto.horaInicio.Split(':')[0]);
            var hf = int.Parse(dto.horaFin.Split(':')[0]);
            if (hf <= hi || hi < 7 || hf > 20)
                return Results.BadRequest("Rango de horas permitido: 07:00–20:00 y horaFin > horaInicio.");

            using var db = Database.GetConnection();

            var check = async (string sql) => await db.QueryFirstOrDefaultAsync<int?>(sql, dto) is not null;

            if (!await check("SELECT id FROM Profesores WHERE id=@profesorId"))
                return Results.BadRequest("Profesor no existe.");
            if (!await check("SELECT id FROM Grupos WHERE id=@grupoId"))
                return Results.BadRequest("Grupo no existe.");
            if (!await check("SELECT id FROM Aulas WHERE id=@aulaId"))
                return Results.BadRequest("Aula no existe.");
            if (!await check("SELECT id FROM Asignaturas WHERE id=@asignaturaId"))
                return Results.BadRequest("Asignatura no existe.");

            var traslape = await db.QueryFirstOrDefaultAsync<int?>(@"
                SELECT id FROM Clases
                WHERE (@id IS NULL OR id != @id)
                  AND dia=@dia AND horaInicio=@horaInicio AND horaFin=@horaFin
                  AND (profesorId=@profesorId OR aulaId=@aulaId OR grupoId=@grupoId)",
                new { dto.dia, dto.horaInicio, dto.horaFin, dto.profesorId, dto.aulaId, dto.grupoId, id });
            if (traslape is not null)
                return Results.BadRequest("Traslape de horario con profesor, aula o grupo.");

            var numAlu = await db.QuerySingleAsync<int>("SELECT numero_alumnos FROM Grupos WHERE id=@grupoId", dto);
            var cap = await db.QuerySingleAsync<int>("SELECT capacidad FROM Aulas WHERE id=@aulaId", dto);
            if (numAlu > cap)
                return Results.BadRequest("El grupo tiene más alumnos que la capacidad del aula.");

            var disp = await db.QueryFirstOrDefaultAsync<bool?>(@"
                SELECT disponible FROM ProfesorDisponibilidad
                WHERE profesorId=@profesorId AND dia=@dia
                AND horaInicio=@horaInicio AND horaFin=@horaFin", dto);
            if (disp is not true)
                return Results.BadRequest("El profesor no está disponible en ese horario.");

            return null;
        }
    }
}

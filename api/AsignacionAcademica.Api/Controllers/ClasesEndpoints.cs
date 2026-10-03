using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Dapper;
using AsignacionAcademica.Api.Horarios;

namespace AsignacionAcademica.Api.Controllers
{
    public static class ClasesEndpoints
    {
        public static void MapClases(this WebApplication app)
        {
            app.MapGet("/clases", async (ConexionDb conexion) =>
            {
                using var db = conexion.Abrir();
                return Results.Ok(await db.QueryAsync("SELECT * FROM Clases"));
            });

            app.MapPost("/clases", async (ClaseDto dto, ConexionDb conexion) =>
            {
                var error = await ValidarClase(dto, conexion);
                if (error is not null) return error;

                using var db = conexion.Abrir();
                var id = await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO Clases(asignaturaId,profesorId,aulaId,grupoId,dia,horaInicio,horaFin)
                    VALUES(@asignaturaId,@profesorId,@aulaId,@grupoId,@dia,@horaInicio,@horaFin);
                    SELECT LAST_INSERT_ID();", dto);
                return Results.Created($"/clases/{id}", new { id });
            });

            app.MapPut("/clases/{id:int}", async (int id, ClaseDto dto, ConexionDb conexion) =>
            {
                var error = await ValidarClase(dto, conexion, id);
                if (error is not null) return error;

                using var db = conexion.Abrir();
                await db.ExecuteAsync(@"
                    UPDATE Clases
                    SET asignaturaId=@asignaturaId, profesorId=@profesorId, aulaId=@aulaId,
                        grupoId=@grupoId, dia=@dia, horaInicio=@horaInicio, horaFin=@horaFin
                    WHERE id=@id", new { dto.asignaturaId, dto.profesorId, dto.aulaId, dto.grupoId, dto.dia, dto.horaInicio, dto.horaFin, id });
                return Results.Ok("Clase actualizada");
            });

            app.MapDelete("/clases/{id:int}", async (int id, ConexionDb conexion) =>
            {
                using var db = conexion.Abrir();
                await db.ExecuteAsync("DELETE FROM Clases WHERE id=@Id", new { Id = id });
                return Results.Ok("Clase eliminada");
            });
        }

        private sealed class FilaFranja
        {
            public string? inicio { get; set; }
            public string? fin { get; set; }
        }

        private static async Task<IResult?> ValidarClase(ClaseDto dto, ConexionDb conexion, int? id = null)
        {
            if (dto.asignaturaId <= 0 || dto.profesorId <= 0 || dto.aulaId <= 0
                || dto.grupoId <= 0 || string.IsNullOrWhiteSpace(dto.dia)
                || !ReglasHorario.TryCrearFranja(dto.horaInicio, dto.horaFin, out var franja))
                return Results.BadRequest("Campos inválidos, formato de hora incorrecto (HH:mm) u horaFin no posterior a horaInicio.");

            if (!ReglasHorario.DentroDeJornada(franja))
                return Results.BadRequest("Rango de horas permitido: 07:00–20:00.");

            using var db = conexion.Abrir();

            var check = async (string sql) => await db.QueryFirstOrDefaultAsync<int?>(sql, dto) is not null;

            if (!await check("SELECT id FROM Profesores WHERE id=@profesorId"))
                return Results.BadRequest("Profesor no existe.");
            if (!await check("SELECT id FROM Grupos WHERE id=@grupoId"))
                return Results.BadRequest("Grupo no existe.");
            if (!await check("SELECT id FROM Aulas WHERE id=@aulaId"))
                return Results.BadRequest("Aula no existe.");
            if (!await check("SELECT id FROM Asignaturas WHERE id=@asignaturaId"))
                return Results.BadRequest("Asignatura no existe.");

            //TIME_FORMAT normaliza a HH:mm tanto si la columna es TIME como si es texto, y así la comparación de cadenas es válida
            var traslape = await db.QueryFirstOrDefaultAsync<int?>(@"
                SELECT id FROM Clases
                WHERE (@id IS NULL OR id != @id)
                  AND dia=@dia
                  AND TIME_FORMAT(horaInicio,'%H:%i') < @horaFin
                  AND TIME_FORMAT(horaFin,'%H:%i') > @horaInicio
                  AND (profesorId=@profesorId OR aulaId=@aulaId OR grupoId=@grupoId)",
                new { dto.dia, dto.horaInicio, dto.horaFin, dto.profesorId, dto.aulaId, dto.grupoId, id });
            if (traslape is not null)
                return Results.BadRequest("Traslape de horario con profesor, aula o grupo.");

            var numAlu = await db.QuerySingleAsync<int>("SELECT numero_alumnos FROM Grupos WHERE id=@grupoId", dto);
            var cap = await db.QuerySingleAsync<int>("SELECT capacidad FROM Aulas WHERE id=@aulaId", dto);
            if (numAlu > cap)
                return Results.BadRequest("El grupo tiene más alumnos que la capacidad del aula.");

            var filas = await db.QueryAsync<FilaFranja>(@"
                SELECT TIME_FORMAT(horaInicio,'%H:%i') AS inicio, TIME_FORMAT(horaFin,'%H:%i') AS fin
                FROM ProfesorDisponibilidad
                WHERE profesorId=@profesorId AND dia=@dia AND disponible", dto);
            var disponibles = new List<Franja>();
            foreach (var f in filas)
                if (ReglasHorario.TryCrearFranja(f.inicio, f.fin, out var d))
                    disponibles.Add(d);
            if (!ReglasHorario.EstaCubierta(franja, disponibles))
                return Results.BadRequest("El profesor no está disponible en ese horario.");

            return null;
        }
    }
}

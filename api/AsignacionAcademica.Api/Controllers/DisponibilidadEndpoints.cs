using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Dapper;
using AsignacionAcademica.Api.Horarios;

namespace AsignacionAcademica.Api.Controllers
{
    public static class DisponibilidadEndpoints
    {
        public static void MapDisponibilidad(this WebApplication app)
        {
            app.MapGet("/profesores/{id:int}/disponibilidad", async (int id, ConexionDb conexion) =>
            {
                using var db = conexion.Abrir();
                var rows = await db.QueryAsync(@"
                    SELECT id, profesorId AS profesor_id, dia,
                           horaInicio AS hora_inicio, horaFin AS hora_fin, disponible
                    FROM ProfesorDisponibilidad
                    WHERE profesorId = @Id", new { Id = id });

                return Results.Ok(rows);
            });

            app.MapPost("/profesores/{id:int}/disponibilidad", async (int id, List<DisponibilidadDto> disponibilidad, ConexionDb conexion) =>
            {
                if (disponibilidad.Any(d => string.IsNullOrWhiteSpace(d.dia)
                        || !ReglasHorario.TryCrearFranja(d.hora_inicio, d.hora_fin, out _)))
                    return Results.BadRequest("Formato de disponibilidad incorrecto.");

                using var db = conexion.Abrir();

                var existe = await db.QueryFirstOrDefaultAsync<int?>("SELECT id FROM Profesores WHERE id=@Id", new { Id = id });
                if (existe is null)
                    return Results.NotFound("Profesor no existe.");

                using var tx = db.BeginTransaction();
                await db.ExecuteAsync("DELETE FROM ProfesorDisponibilidad WHERE profesorId = @Id", new { Id = id }, tx);

                foreach (var d in disponibilidad)
                {
                    var sql = @"
                        INSERT INTO ProfesorDisponibilidad (profesorId, dia, horaInicio, horaFin, disponible)
                        VALUES (@Id, @dia, @hora_inicio, @hora_fin, @disponible)";
                    await db.ExecuteAsync(sql, new
                    {
                        Id = id,
                        d.dia,
                        hora_inicio = d.hora_inicio,
                        hora_fin = d.hora_fin,
                        d.disponible
                    }, tx);
                }

                tx.Commit();
                return Results.Created($"/profesores/{id}/disponibilidad", null);
            });
        }
    }
}

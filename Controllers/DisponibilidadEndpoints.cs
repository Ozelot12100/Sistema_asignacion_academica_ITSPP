using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Dapper;
using System.Text.RegularExpressions;

namespace AsignacionAcademica.Api.Controllers
{
    public static class DisponibilidadEndpoints
    {
        public static void MapDisponibilidad(this WebApplication app)
        {
            var horaRegex = new Regex(@"^([01]\d|2[0-3]):[0-5]\d$");

            app.MapGet("/profesores/{id:int}/disponibilidad", async (int id) =>
            {
                using var db = Database.GetConnection();
                var rows = await db.QueryAsync(@"
                    SELECT id, profesorId AS profesor_id, dia,
                           horaInicio AS hora_inicio, horaFin AS hora_fin, disponible
                    FROM ProfesorDisponibilidad
                    WHERE profesorId = @Id", new { Id = id });

                return Results.Ok(rows);
            });

            app.MapPost("/profesores/{id:int}/disponibilidad", async (int id, List<DisponibilidadDto> disponibilidad) =>
            {
                using var db = Database.GetConnection();
                using var tx = db.BeginTransaction();
                try
                {
                    await db.ExecuteAsync("DELETE FROM ProfesorDisponibilidad WHERE profesorId = @Id", new { Id = id }, tx);

                    foreach (var d in disponibilidad)
                    {
                        if (string.IsNullOrWhiteSpace(d.dia)
                            || !horaRegex.IsMatch(d.hora_inicio)
                            || !horaRegex.IsMatch(d.hora_fin))
                            return Results.BadRequest("Formato de disponibilidad incorrecto.");

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
                }
                catch
                {
                    tx.Rollback();
                    return Results.StatusCode(500);
                }
            });
        }
    }
}

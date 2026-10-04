using Asistencias.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Laboratorios;

public record LaboratorioDto(
    int Id, string Clave, string Nombre, string? Ubicacion,
    int TotalEquipos, int EquiposEnUso, int EquiposFueraDeServicio);

public record EquipoDto(int Id, string Etiqueta, EstadoEquipo Estado, string? Notas);

public static class LaboratoriosEndpoints
{
    public static void MapLaboratorios(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/laboratorios").WithTags("Laboratorios").RequireAuthorization();

        grupo.MapGet("/", async (AsistenciasDbContext db) =>
            await db.Laboratorios.AsNoTracking()
                .Where(l => l.Activo)
                .OrderBy(l => l.Nombre)
                .Select(l => new LaboratorioDto(
                    l.Id, l.Clave, l.Nombre, l.Ubicacion,
                    l.Equipos.Count(),
                    l.Equipos.Count(e => !e.FueraDeServicio
                        && db.Asistencias.Any(a => a.EquipoId == e.Id && a.HoraSalida == null)),
                    l.Equipos.Count(e => e.FueraDeServicio)))
                .ToListAsync());

        grupo.MapGet("/{id:int}/equipos", async (int id, AsistenciasDbContext db) =>
        {
            if (!await db.Laboratorios.AnyAsync(l => l.Id == id))
                return Results.Problem(statusCode: 404, title: "El laboratorio no existe.");

            var equipos = await db.Equipos.AsNoTracking()
                .Where(e => e.LaboratorioId == id)
                .Select(e => new
                {
                    e.Id, e.Etiqueta, e.FueraDeServicio, e.Notas,
                    Ocupado = db.Asistencias.Any(a => a.EquipoId == e.Id && a.HoraSalida == null),
                })
                .ToListAsync();

            return Results.Ok(equipos
                .OrdenarPorEtiqueta(e => e.Etiqueta)
                .Select(e => new EquipoDto(e.Id, e.Etiqueta, ReglasEquipo.Estado(e.FueraDeServicio, e.Ocupado), e.Notas)));
        });
    }
}

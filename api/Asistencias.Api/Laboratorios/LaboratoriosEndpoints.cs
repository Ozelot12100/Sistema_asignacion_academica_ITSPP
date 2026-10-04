using Asistencias.Api.Comun;
using Asistencias.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Laboratorios;

public record LaboratorioDto(
    int Id, string Clave, string Nombre, string? Ubicacion, bool Activo,
    int TotalEquipos, int EquiposEnUso, int EquiposFueraDeServicio);

public record LaboratorioEntrada(string? Clave, string? Nombre, string? Ubicacion, bool? Activo);

public record EquipoDto(int Id, int LaboratorioId, string Etiqueta, EstadoEquipo Estado, string? Notas);

public record EquipoEntrada(string? Etiqueta, bool? FueraDeServicio, string? Notas);

public record ServicioEquipoEntrada(bool? FueraDeServicio, string? Notas);

public static class LaboratoriosEndpoints
{
    public static void MapLaboratorios(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/laboratorios").WithTags("Laboratorios").RequireAuthorization();

        // Los desactivados solo salen si se piden: la operación diaria no los necesita
        grupo.MapGet("/", async (bool? incluirInactivos, AsistenciasDbContext db) =>
            await ADto(db, db.Laboratorios
                    .Where(l => incluirInactivos == true || l.Activo)
                    .OrderBy(l => l.Nombre))
                .ToListAsync());

        grupo.MapGet("/{id:int}/equipos", async (int id, AsistenciasDbContext db) =>
        {
            if (!await db.Laboratorios.AnyAsync(l => l.Id == id))
                return Persistencia.NoExiste("El laboratorio");

            var equipos = await db.Equipos.AsNoTracking()
                .Where(e => e.LaboratorioId == id)
                .Select(e => new
                {
                    Equipo = e,
                    Ocupado = db.Asistencias.Any(a => a.EquipoId == e.Id && a.HoraSalida == null),
                })
                .ToListAsync();

            return Results.Ok(equipos
                .OrdenarPorEtiqueta(e => e.Equipo.Etiqueta)
                .Select(e => EquiposEndpoints.ADto(e.Equipo, e.Ocupado)));
        });

        grupo.MapPost("/", async (LaboratorioEntrada entrada, AsistenciasDbContext db) =>
        {
            var v = new Validador();
            var laboratorio = new Laboratorio { Clave = "", Nombre = "" };
            Aplicar(laboratorio, entrada, v);
            if (v.Error is { } error) return error;

            db.Laboratorios.Add(laboratorio);
            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Created($"/api/laboratorios/{laboratorio.Id}", await ADto(db, db.Laboratorios.Where(l => l.Id == laboratorio.Id)).SingleAsync());
        }).RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapPut("/{id:int}", async (int id, LaboratorioEntrada entrada, AsistenciasDbContext db) =>
        {
            var laboratorio = await db.Laboratorios.FindAsync(id);
            if (laboratorio is null) return Persistencia.NoExiste("El laboratorio");

            var v = new Validador();
            Aplicar(laboratorio, entrada, v);
            if (v.Error is { } error) return error;

            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Ok(await ADto(db, db.Laboratorios.Where(l => l.Id == id)).SingleAsync());
        }).RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapDelete("/{id:int}", async (int id, AsistenciasDbContext db) =>
        {
            var laboratorio = await db.Laboratorios.FindAsync(id);
            if (laboratorio is null) return Persistencia.NoExiste("El laboratorio");

            db.Laboratorios.Remove(laboratorio);
            return await db.GuardarAsync() ?? Results.NoContent();
        }).RequireAuthorization(Politicas.SoloAdmin);
    }

    private static IQueryable<LaboratorioDto> ADto(AsistenciasDbContext db, IQueryable<Laboratorio> laboratorios) =>
        laboratorios.AsNoTracking().Select(l => new LaboratorioDto(
            l.Id, l.Clave, l.Nombre, l.Ubicacion, l.Activo,
            l.Equipos.Count(),
            l.Equipos.Count(e => !e.FueraDeServicio
                && db.Asistencias.Any(a => a.EquipoId == e.Id && a.HoraSalida == null)),
            l.Equipos.Count(e => e.FueraDeServicio)));

    private static void Aplicar(Laboratorio laboratorio, LaboratorioEntrada entrada, Validador v)
    {
        laboratorio.Clave = v.Texto("clave", entrada.Clave, 50);
        laboratorio.Nombre = v.Texto("nombre", entrada.Nombre, 100);
        laboratorio.Ubicacion = v.Opcional("ubicacion", entrada.Ubicacion, 100);
        laboratorio.Activo = entrada.Activo ?? laboratorio.Activo;
    }
}

public static class EquiposEndpoints
{
    public static EquipoDto ADto(Equipo e, bool ocupado) =>
        new(e.Id, e.LaboratorioId, e.Etiqueta, ReglasEquipo.Estado(e.FueraDeServicio, ocupado), e.Notas);

    public static void MapEquipos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api").WithTags("Equipos").RequireAuthorization();

        grupo.MapPost("/laboratorios/{idLaboratorio:int}/equipos", async (
            int idLaboratorio, EquipoEntrada entrada, AsistenciasDbContext db) =>
        {
            if (!await db.Laboratorios.AnyAsync(l => l.Id == idLaboratorio))
                return Persistencia.NoExiste("El laboratorio");

            var v = new Validador();
            var equipo = new Equipo { LaboratorioId = idLaboratorio, Etiqueta = "" };
            Aplicar(equipo, entrada, v);
            if (v.Error is { } error) return error;

            db.Equipos.Add(equipo);
            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Created($"/api/equipos/{equipo.Id}", ADto(equipo, ocupado: false));
        }).RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapPut("/equipos/{id:int}", async (int id, EquipoEntrada entrada, AsistenciasDbContext db) =>
        {
            var equipo = await db.Equipos.FindAsync(id);
            if (equipo is null) return Persistencia.NoExiste("El equipo");

            var v = new Validador();
            Aplicar(equipo, entrada, v);
            if (v.Error is { } error) return error;

            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Ok(ADto(equipo, await EstaOcupado(db, id)));
        }).RequireAuthorization(Politicas.SoloAdmin);

        // Lo único que el laboratorista cambia de un equipo: si sirve o no, y por qué
        grupo.MapPut("/equipos/{id:int}/servicio", async (int id, ServicioEquipoEntrada entrada, AsistenciasDbContext db) =>
        {
            var equipo = await db.Equipos.FindAsync(id);
            if (equipo is null) return Persistencia.NoExiste("El equipo");

            var v = new Validador();
            v.Si(entrada.FueraDeServicio is null, "fueraDeServicio", "Es obligatorio.");
            var notas = v.Opcional("notas", entrada.Notas, 2000);
            if (v.Error is { } error) return error;

            equipo.FueraDeServicio = entrada.FueraDeServicio!.Value;
            equipo.Notas = notas;
            await db.SaveChangesAsync();
            return Results.Ok(ADto(equipo, await EstaOcupado(db, id)));
        }).RequireAuthorization(Politicas.Personal);

        grupo.MapDelete("/equipos/{id:int}", async (int id, AsistenciasDbContext db) =>
        {
            var equipo = await db.Equipos.FindAsync(id);
            if (equipo is null) return Persistencia.NoExiste("El equipo");

            db.Equipos.Remove(equipo);
            return await db.GuardarAsync() ?? Results.NoContent();
        }).RequireAuthorization(Politicas.SoloAdmin);
    }

    private static Task<bool> EstaOcupado(AsistenciasDbContext db, int idEquipo) =>
        db.Asistencias.AnyAsync(a => a.EquipoId == idEquipo && a.HoraSalida == null);

    private static void Aplicar(Equipo equipo, EquipoEntrada entrada, Validador v)
    {
        equipo.Etiqueta = v.Texto("etiqueta", entrada.Etiqueta, 50);
        equipo.FueraDeServicio = entrada.FueraDeServicio ?? equipo.FueraDeServicio;
        equipo.Notas = v.Opcional("notas", entrada.Notas, 2000);
    }
}

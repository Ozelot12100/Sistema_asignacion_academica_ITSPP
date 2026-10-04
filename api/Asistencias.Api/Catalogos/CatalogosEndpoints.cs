using Asistencias.Api.Comun;
using Asistencias.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Catalogos;

// Catálogos sencillos: cualquiera con sesión los consulta; solo el administrador los modifica.

public record CarreraDto(int Id, string Clave, string Nombre, bool Activa);
public record CarreraEntrada(string? Clave, string? Nombre, bool? Activa);

public record PeriodoDto(int Id, string Nombre, DateOnly FechaInicio, DateOnly FechaFin);
public record PeriodoEntrada(string? Nombre, DateOnly? FechaInicio, DateOnly? FechaFin);

public record MateriaDto(int Id, string Clave, string Nombre, int? CarreraId, string? Carrera, byte? Semestre);
public record MateriaEntrada(string? Clave, string? Nombre, int? CarreraId, byte? Semestre);

public static class CatalogosEndpoints
{
    public const byte SemestreMaximo = 15;

    public static void MapCarreras(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/carreras").WithTags("Carreras").RequireAuthorization();

        grupo.MapGet("/", async (bool? incluirInactivas, AsistenciasDbContext db) =>
            await db.Carreras.AsNoTracking()
                .Where(c => incluirInactivas == true || c.Activa)
                .OrderBy(c => c.Nombre)
                .Select(c => new CarreraDto(c.Id, c.Clave, c.Nombre, c.Activa))
                .ToListAsync());

        grupo.MapPost("/", async (CarreraEntrada entrada, AsistenciasDbContext db) =>
        {
            var v = new Validador();
            var carrera = new Carrera { Clave = "", Nombre = "" };
            Aplicar(carrera, entrada, v);
            if (v.Error is { } error) return error;

            db.Carreras.Add(carrera);
            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Created($"/api/carreras/{carrera.Id}", ADto(carrera));
        }).RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapPut("/{id:int}", async (int id, CarreraEntrada entrada, AsistenciasDbContext db) =>
        {
            var carrera = await db.Carreras.FindAsync(id);
            if (carrera is null) return Persistencia.NoExiste("La carrera");

            var v = new Validador();
            Aplicar(carrera, entrada, v);
            if (v.Error is { } error) return error;

            return await db.GuardarAsync() ?? Results.Ok(ADto(carrera));
        }).RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapDelete("/{id:int}", async (int id, AsistenciasDbContext db) =>
        {
            var carrera = await db.Carreras.FindAsync(id);
            if (carrera is null) return Persistencia.NoExiste("La carrera");

            db.Carreras.Remove(carrera);
            return await db.GuardarAsync() ?? Results.NoContent();
        }).RequireAuthorization(Politicas.SoloAdmin);

        static CarreraDto ADto(Carrera c) => new(c.Id, c.Clave, c.Nombre, c.Activa);

        static void Aplicar(Carrera carrera, CarreraEntrada entrada, Validador v)
        {
            carrera.Clave = v.Texto("clave", entrada.Clave, 20);
            carrera.Nombre = v.Texto("nombre", entrada.Nombre, 100);
            carrera.Activa = entrada.Activa ?? carrera.Activa;
        }
    }

    public static void MapPeriodos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/periodos").WithTags("Periodos").RequireAuthorization();

        grupo.MapGet("/", async (AsistenciasDbContext db) =>
            await db.Periodos.AsNoTracking()
                .OrderByDescending(p => p.FechaInicio)
                .Select(p => new PeriodoDto(p.Id, p.Nombre, p.FechaInicio, p.FechaFin))
                .ToListAsync());

        grupo.MapPost("/", async (PeriodoEntrada entrada, AsistenciasDbContext db) =>
        {
            var v = new Validador();
            var periodo = new Periodo { Nombre = "" };
            Aplicar(periodo, entrada, v);
            if (v.Error is { } error) return error;

            db.Periodos.Add(periodo);
            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Created($"/api/periodos/{periodo.Id}", ADto(periodo));
        }).RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapPut("/{id:int}", async (int id, PeriodoEntrada entrada, AsistenciasDbContext db) =>
        {
            var periodo = await db.Periodos.FindAsync(id);
            if (periodo is null) return Persistencia.NoExiste("El periodo");

            var v = new Validador();
            Aplicar(periodo, entrada, v);
            if (v.Error is { } error) return error;

            return await db.GuardarAsync() ?? Results.Ok(ADto(periodo));
        }).RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapDelete("/{id:int}", async (int id, AsistenciasDbContext db) =>
        {
            var periodo = await db.Periodos.FindAsync(id);
            if (periodo is null) return Persistencia.NoExiste("El periodo");

            db.Periodos.Remove(periodo);
            return await db.GuardarAsync() ?? Results.NoContent();
        }).RequireAuthorization(Politicas.SoloAdmin);

        static PeriodoDto ADto(Periodo p) => new(p.Id, p.Nombre, p.FechaInicio, p.FechaFin);

        static void Aplicar(Periodo periodo, PeriodoEntrada entrada, Validador v)
        {
            periodo.Nombre = v.Texto("nombre", entrada.Nombre, 50);
            v.Si(entrada.FechaInicio is null, "fechaInicio", "Es obligatoria.");
            v.Si(entrada.FechaFin is null, "fechaFin", "Es obligatoria.");
            v.Si(entrada.FechaFin <= entrada.FechaInicio, "fechaFin", "Debe ser posterior a la fecha de inicio.");
            periodo.FechaInicio = entrada.FechaInicio ?? periodo.FechaInicio;
            periodo.FechaFin = entrada.FechaFin ?? periodo.FechaFin;
        }
    }

    public static void MapMaterias(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/materias").WithTags("Materias").RequireAuthorization();

        grupo.MapGet("/", async (int? carreraId, AsistenciasDbContext db) =>
            await ADto(db.Materias
                    .Where(m => carreraId == null || m.CarreraId == carreraId)
                    .OrderBy(m => m.Nombre))
                .ToListAsync());

        grupo.MapPost("/", async (MateriaEntrada entrada, AsistenciasDbContext db) =>
        {
            var v = new Validador();
            var materia = new Materia { Clave = "", Nombre = "" };
            Aplicar(materia, entrada, v);
            if (v.Error is { } error) return error;

            db.Materias.Add(materia);
            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Created($"/api/materias/{materia.Id}", await ADto(db.Materias.Where(m => m.Id == materia.Id)).SingleAsync());
        }).RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapPut("/{id:int}", async (int id, MateriaEntrada entrada, AsistenciasDbContext db) =>
        {
            var materia = await db.Materias.FindAsync(id);
            if (materia is null) return Persistencia.NoExiste("La materia");

            var v = new Validador();
            Aplicar(materia, entrada, v);
            if (v.Error is { } error) return error;

            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Ok(await ADto(db.Materias.Where(m => m.Id == id)).SingleAsync());
        }).RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapDelete("/{id:int}", async (int id, AsistenciasDbContext db) =>
        {
            var materia = await db.Materias.FindAsync(id);
            if (materia is null) return Persistencia.NoExiste("La materia");

            db.Materias.Remove(materia);
            return await db.GuardarAsync() ?? Results.NoContent();
        }).RequireAuthorization(Politicas.SoloAdmin);

        static IQueryable<MateriaDto> ADto(IQueryable<Materia> materias) =>
            materias.AsNoTracking().Select(m =>
                new MateriaDto(m.Id, m.Clave, m.Nombre, m.CarreraId, m.Carrera!.Nombre, m.Semestre));

        static void Aplicar(Materia materia, MateriaEntrada entrada, Validador v)
        {
            materia.Clave = v.Texto("clave", entrada.Clave, 50);
            materia.Nombre = v.Texto("nombre", entrada.Nombre, 150);
            materia.CarreraId = entrada.CarreraId;
            v.Si(entrada.Semestre is 0 or > SemestreMaximo, "semestre", $"Debe estar entre 1 y {SemestreMaximo}.");
            materia.Semestre = entrada.Semestre;
        }
    }
}

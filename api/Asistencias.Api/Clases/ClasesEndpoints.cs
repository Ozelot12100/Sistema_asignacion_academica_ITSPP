using System.Security.Claims;
using Asistencias.Api.Autenticacion;
using Asistencias.Api.Comun;
using Asistencias.Api.Datos;
using Asistencias.Api.Laboratorios;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Clases;

public record ClaseDto(
    int Id, int CursoId, string Materia, string Grupo, int LaboratorioId, string Laboratorio,
    int? PracticaId, string? Practica, int AbiertaPorId, string AbiertaPor,
    DateTime AbiertaEn, DateTime? CerradaEn, int Registrados);

public record ClaseEntrada(int? CursoId, int? LaboratorioId, int? PracticaId);

public record AsistenteDto(
    long AsistenciaId, int AlumnoId, string Matricula, string Nombre, string Apellido,
    string? Equipo, DateTime HoraEntrada, DateTime? HoraSalida);

public record EntradaAlumno(string? Matricula, string? Equipo);

public record EntradaRegistradaDto(
    string Alumno, string Materia, string Grupo, string Laboratorio, string? Equipo, DateTime HoraEntrada);

public static class ClasesEndpoints
{
    public const string LimiteRegistro = "registro";
    public const int MaximoListado = 100;

    public static void MapClases(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/clases").WithTags("Clases").RequireAuthorization();

        grupo.MapGet("/", async (bool? abiertas, int? cursoId, int? laboratorioId, AsistenciasDbContext db) =>
            await ADto(db.Clases
                    .Where(c => (abiertas == null || (c.CerradaEn == null) == abiertas)
                        && (cursoId == null || c.CursoId == cursoId)
                        && (laboratorioId == null || c.LaboratorioId == laboratorioId))
                    .OrderByDescending(c => c.AbiertaEn).ThenByDescending(c => c.Id)
                    .Take(MaximoListado))
                .ToListAsync());

        grupo.MapGet("/{id:int}", async (int id, AsistenciasDbContext db) =>
            await ADto(db.Clases.Where(c => c.Id == id)).SingleOrDefaultAsync() is { } clase
                ? Results.Ok(clase)
                : Persistencia.NoExiste("La clase"));

        grupo.MapGet("/{id:int}/asistencias", async (int id, AsistenciasDbContext db) =>
        {
            if (!await db.Clases.AnyAsync(c => c.Id == id))
                return Persistencia.NoExiste("La clase");

            return Results.Ok(await db.Asistencias.AsNoTracking()
                .Where(a => a.ClaseId == id && a.AlumnoId != null)
                .OrderBy(a => a.Alumno!.Apellido).ThenBy(a => a.Alumno!.Nombre).ThenBy(a => a.Id)
                .Select(a => new AsistenteDto(
                    a.Id, a.Alumno!.Id, a.Alumno.Matricula, a.Alumno.Nombre, a.Alumno.Apellido,
                    a.Equipo!.Etiqueta, a.HoraEntrada, a.HoraSalida))
                .ToListAsync());
        });

        grupo.MapPost("/", async (ClaseEntrada entrada, ClaimsPrincipal principal, AsistenciasDbContext db) =>
        {
            var v = new Validador();
            v.Si(entrada.CursoId is null, "cursoId", "Es obligatorio.");
            if (v.Error is { } sinCurso) return sinCurso;

            var curso = await db.Cursos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == entrada.CursoId);
            if (curso is null) return Persistencia.NoExiste("El curso");
            if (!ReglasClase.PuedeAbrir(principal.RolUsuario(), principal.IdUsuario(), curso.DocenteId))
                return Results.Forbid();

            // Si no se indica laboratorio, la clase se da en el que tiene asignado el curso
            var laboratorioId = entrada.LaboratorioId ?? curso.LaboratorioId;
            v.Si(laboratorioId is null, "laboratorioId", "El curso no tiene laboratorio asignado; indica uno.");
            v.Si(laboratorioId is not null && !await db.Laboratorios.AnyAsync(l => l.Id == laboratorioId && l.Activo),
                "laboratorioId", "Debe ser un laboratorio activo.");
            v.Si(entrada.PracticaId is not null
                    && !await db.Practicas.AnyAsync(p => p.Id == entrada.PracticaId && p.MateriaId == curso.MateriaId),
                "practicaId", "Debe ser una práctica de la materia del curso.");
            if (v.Error is { } error) return error;

            if (await db.Clases.AnyAsync(c => c.CerradaEn == null && c.CursoId == curso.Id))
                return Conflicto("El curso ya tiene una clase abierta.");
            if (await db.Clases.AnyAsync(c => c.CerradaEn == null && c.LaboratorioId == laboratorioId))
                return Conflicto("El laboratorio ya tiene una clase abierta.");

            var clase = new Clase
            {
                CursoId = curso.Id,
                LaboratorioId = laboratorioId!.Value,
                PracticaId = entrada.PracticaId,
                AbiertaPorId = principal.IdUsuario(),
            };
            db.Clases.Add(clase);
            // Si dos personas abren a la vez, la base rechaza a la segunda
            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Created($"/api/clases/{clase.Id}", await ADto(db.Clases.Where(c => c.Id == clase.Id)).SingleAsync());
        });

        // Cerrar la clase registra la salida de todos los que seguían dentro
        grupo.MapPost("/{id:int}/cerrar", async (int id, ClaimsPrincipal principal, AsistenciasDbContext db) =>
        {
            var clase = await db.Clases.Include(c => c.Curso).SingleOrDefaultAsync(c => c.Id == id);
            if (clase is null) return Persistencia.NoExiste("La clase");
            if (!ReglasClase.PuedeCerrar(principal.RolUsuario(), principal.IdUsuario(), clase.Curso!.DocenteId))
                return Results.Forbid();
            if (clase.CerradaEn is not null) return Conflicto("La clase ya está cerrada.");

            DateTime? ahora = DateTime.UtcNow;
            await using var transaccion = await db.Database.BeginTransactionAsync();
            await db.Asistencias
                .Where(a => a.ClaseId == id && a.HoraSalida == null)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.HoraSalida, ahora));
            clase.CerradaEn = ahora;
            await db.SaveChangesAsync();
            await transaccion.CommitAsync();

            return Results.Ok(await ADto(db.Clases.Where(c => c.Id == id)).SingleAsync());
        });

        // Sin sesión: los alumnos no tienen cuenta y registran su entrada con su número de control.
        // Solo entra quien está inscrito en un curso que tiene la clase abierta en ese momento.
        app.MapPost("/api/registro/entrada", async (EntradaAlumno entrada, AsistenciasDbContext db) =>
        {
            var v = new Validador();
            var matricula = v.Texto("matricula", entrada.Matricula, 20);
            var etiqueta = v.Opcional("equipo", entrada.Equipo, 50);
            if (v.Error is { } error) return error;

            var alumno = await db.Alumnos.AsNoTracking().SingleOrDefaultAsync(a => a.Matricula == matricula && a.Activo);
            if (alumno is null)
                return Results.Problem(statusCode: 404, title: "No hay un alumno activo con ese número de control.");

            var clase = await db.Clases.AsNoTracking()
                .Where(c => c.CerradaEn == null && c.Curso!.Inscripciones.Any(i => i.AlumnoId == alumno.Id))
                .OrderByDescending(c => c.AbiertaEn)
                .Select(c => new
                {
                    c.Id, c.CursoId, c.LaboratorioId, c.PracticaId,
                    Materia = c.Curso!.Materia!.Nombre, c.Curso.Grupo, Laboratorio = c.Laboratorio!.Nombre,
                })
                .FirstOrDefaultAsync();
            if (clase is null) return Conflicto("No tienes ninguna clase abierta en este momento.");
            if (await db.Asistencias.AnyAsync(a => a.AlumnoId == alumno.Id && a.HoraSalida == null))
                return Conflicto("Ya tienes una entrada registrada.");

            Equipo? equipo = null;
            if (etiqueta is not null)
            {
                equipo = await db.Equipos.AsNoTracking()
                    .SingleOrDefaultAsync(e => e.LaboratorioId == clase.LaboratorioId && e.Etiqueta == etiqueta);
                if (equipo is null)
                    return Results.Problem(statusCode: 404, title: "Ese equipo no existe en el laboratorio de la clase.");

                var ocupado = await db.Asistencias.AnyAsync(a => a.EquipoId == equipo.Id && a.HoraSalida == null);
                switch (ReglasEquipo.Estado(equipo.FueraDeServicio, ocupado))
                {
                    case EstadoEquipo.FueraDeServicio: return Conflicto("Ese equipo está fuera de servicio.");
                    case EstadoEquipo.EnUso: return Conflicto("Ese equipo ya está ocupado.");
                }
            }

            var asistencia = new Asistencia
            {
                LaboratorioId = clase.LaboratorioId,
                EquipoId = equipo?.Id,
                TipoUsuario = TipoUsuario.Estudiante,
                AlumnoId = alumno.Id,
                CursoId = clase.CursoId,
                PracticaId = clase.PracticaId,
                ClaseId = clase.Id,
            };
            db.Asistencias.Add(asistencia);
            // Si dos registros llegan a la vez por el mismo alumno o equipo, la base rechaza al segundo
            if (await db.GuardarAsync() is { } conflicto) return conflicto;

            return Results.Ok(new EntradaRegistradaDto(
                $"{alumno.Nombre} {alumno.Apellido}", clase.Materia, clase.Grupo, clase.Laboratorio,
                equipo?.Etiqueta, asistencia.HoraEntrada));
        }).WithTags("Registro").RequireRateLimiting(LimiteRegistro);
    }

    private static IResult Conflicto(string titulo) => Results.Problem(statusCode: 409, title: titulo);

    private static IQueryable<ClaseDto> ADto(IQueryable<Clase> clases) =>
        clases.AsNoTracking().Select(c => new ClaseDto(
            c.Id, c.CursoId, c.Curso!.Materia!.Nombre, c.Curso.Grupo, c.LaboratorioId, c.Laboratorio!.Nombre,
            c.PracticaId, c.Practica!.Titulo, c.AbiertaPorId, c.AbiertaPor!.Nombre + " " + c.AbiertaPor.Apellido,
            c.AbiertaEn, c.CerradaEn, c.Asistencias.Count()));
}

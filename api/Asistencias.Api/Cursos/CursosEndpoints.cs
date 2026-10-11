using System.Security.Claims;
using Asistencias.Api.Autenticacion;
using Asistencias.Api.Comun;
using Asistencias.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Cursos;

public record CursoDto(
    int Id, int MateriaId, string MateriaClave, string Materia, int PeriodoId, string Periodo, string Grupo,
    int? DocenteId, string? Docente, int? LaboratorioId, string? Laboratorio, int Inscritos);

public record CursoEntrada(int? MateriaId, int? PeriodoId, string? Grupo, int? DocenteId, int? LaboratorioId);

public record InscritoDto(int Id, string Matricula, string Nombre, string Apellido, bool Activo);

public static class CursosEndpoints
{
    public static void MapCursos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/cursos").WithTags("Cursos").RequireAuthorization();

        grupo.MapGet("/", async (
            int? periodoId, int? materiaId, int? docenteId, int? laboratorioId, AsistenciasDbContext db) =>
            await ADto(db.Cursos
                    .Where(c => (periodoId == null || c.PeriodoId == periodoId)
                        && (materiaId == null || c.MateriaId == materiaId)
                        && (docenteId == null || c.DocenteId == docenteId)
                        && (laboratorioId == null || c.LaboratorioId == laboratorioId))
                    .OrderByDescending(c => c.Periodo!.FechaInicio)
                    .ThenBy(c => c.Materia!.Nombre)
                    .ThenBy(c => c.Grupo))
                .ToListAsync());

        grupo.MapGet("/{id:int}", async (int id, AsistenciasDbContext db) =>
            await ADto(db.Cursos.Where(c => c.Id == id)).SingleOrDefaultAsync() is { } curso
                ? Results.Ok(curso)
                : Persistencia.NoExiste("El curso"));

        grupo.MapPost("/", async (CursoEntrada entrada, AsistenciasDbContext db) =>
        {
            var v = new Validador();
            var curso = new Curso { Grupo = "" };
            await AplicarAsync(curso, entrada, v, db);
            if (v.Error is { } error) return error;

            db.Cursos.Add(curso);
            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Created($"/api/cursos/{curso.Id}", await ADto(db.Cursos.Where(c => c.Id == curso.Id)).SingleAsync());
        }).RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapPut("/{id:int}", async (int id, CursoEntrada entrada, AsistenciasDbContext db) =>
        {
            var curso = await db.Cursos.FindAsync(id);
            if (curso is null) return Persistencia.NoExiste("El curso");

            var v = new Validador();
            await AplicarAsync(curso, entrada, v, db);
            if (v.Error is { } error) return error;

            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Ok(await ADto(db.Cursos.Where(c => c.Id == id)).SingleAsync());
        }).RequireAuthorization(Politicas.SoloAdmin);

        // Las inscripciones se van con el curso; si ya tiene asistencias, la base lo impide
        grupo.MapDelete("/{id:int}", async (int id, AsistenciasDbContext db) =>
        {
            var curso = await db.Cursos.FindAsync(id);
            if (curso is null) return Persistencia.NoExiste("El curso");

            db.Cursos.Remove(curso);
            return await db.GuardarAsync() ?? Results.NoContent();
        }).RequireAuthorization(Politicas.SoloAdmin);

        grupo.MapGet("/{id:int}/alumnos", async (int id, AsistenciasDbContext db) =>
        {
            if (!await db.Cursos.AnyAsync(c => c.Id == id))
                return Persistencia.NoExiste("El curso");

            return Results.Ok(await db.Inscripciones.AsNoTracking()
                .Where(i => i.CursoId == id)
                .Select(i => i.Alumno!)
                .OrderBy(a => a.Apellido).ThenBy(a => a.Nombre).ThenBy(a => a.Id)
                .Select(a => new InscritoDto(a.Id, a.Matricula, a.Nombre, a.Apellido, a.Activo))
                .ToListAsync());
        });

        // Inscribir dos veces al mismo alumno no es un error: queda inscrito una sola vez
        grupo.MapPut("/{id:int}/alumnos/{alumnoId:int}", async (
            int id, int alumnoId, ClaimsPrincipal principal, AsistenciasDbContext db) =>
        {
            var curso = await db.Cursos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
            if (curso is null) return Persistencia.NoExiste("El curso");
            if (!ReglasCurso.PuedeInscribir(principal.RolUsuario(), principal.IdUsuario(), curso.DocenteId))
                return Results.Forbid();

            var alumno = await db.Alumnos.AsNoTracking().SingleOrDefaultAsync(a => a.Id == alumnoId);
            if (alumno is null) return Persistencia.NoExiste("El alumno");
            if (await db.Inscripciones.AnyAsync(i => i.CursoId == id && i.AlumnoId == alumnoId))
                return Results.NoContent();
            if (!alumno.Activo)
                return Results.Problem(statusCode: 409, title: "El alumno está desactivado.");

            db.Inscripciones.Add(new Inscripcion { CursoId = id, AlumnoId = alumnoId });
            return await db.GuardarAsync() ?? Results.NoContent();
        });

        grupo.MapDelete("/{id:int}/alumnos/{alumnoId:int}", async (
            int id, int alumnoId, ClaimsPrincipal principal, AsistenciasDbContext db) =>
        {
            var curso = await db.Cursos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
            if (curso is null) return Persistencia.NoExiste("El curso");
            if (!ReglasCurso.PuedeInscribir(principal.RolUsuario(), principal.IdUsuario(), curso.DocenteId))
                return Results.Forbid();

            var inscripcion = await db.Inscripciones.FindAsync(id, alumnoId);
            if (inscripcion is null) return Persistencia.NoExiste("La inscripción");

            db.Inscripciones.Remove(inscripcion);
            return await db.GuardarAsync() ?? Results.NoContent();
        });

        app.MapGet("/api/alumnos/{id:int}/cursos", async (int id, int? periodoId, AsistenciasDbContext db) =>
        {
            if (!await db.Alumnos.AnyAsync(a => a.Id == id))
                return Persistencia.NoExiste("El alumno");

            return Results.Ok(await ADto(db.Cursos
                    .Where(c => c.Inscripciones.Any(i => i.AlumnoId == id)
                        && (periodoId == null || c.PeriodoId == periodoId))
                    .OrderByDescending(c => c.Periodo!.FechaInicio)
                    .ThenBy(c => c.Materia!.Nombre))
                .ToListAsync());
        }).WithTags("Cursos").RequireAuthorization();
    }

    private static IQueryable<CursoDto> ADto(IQueryable<Curso> cursos) =>
        cursos.AsNoTracking().Select(c => new CursoDto(
            c.Id, c.MateriaId, c.Materia!.Clave, c.Materia.Nombre, c.PeriodoId, c.Periodo!.Nombre, c.Grupo,
            c.DocenteId, c.DocenteId == null ? null : c.Docente!.Nombre + " " + c.Docente.Apellido,
            c.LaboratorioId, c.Laboratorio!.Nombre, c.Inscripciones.Count()));

    private static async Task AplicarAsync(Curso curso, CursoEntrada entrada, Validador v, AsistenciasDbContext db)
    {
        v.Si(entrada.MateriaId is null, "materiaId", "Es obligatoria.");
        v.Si(entrada.PeriodoId is null, "periodoId", "Es obligatorio.");
        curso.MateriaId = entrada.MateriaId ?? curso.MateriaId;
        curso.PeriodoId = entrada.PeriodoId ?? curso.PeriodoId;
        curso.Grupo = v.Texto("grupo", entrada.Grupo, 20);

        // Solo se revisa al asignarlo: un curso conserva a su docente aunque después se desactive su cuenta
        if (entrada.DocenteId is { } docenteId && docenteId != curso.DocenteId)
        {
            var docente = await db.Usuarios.AsNoTracking()
                .Where(u => u.Id == docenteId)
                .Select(u => new { u.Rol, u.Activo })
                .SingleOrDefaultAsync();
            v.Si(docente is null || !ReglasCurso.PuedeImpartir(docente.Rol, docente.Activo),
                "docenteId", "Debe ser un docente con la cuenta activa.");
        }
        curso.DocenteId = entrada.DocenteId;
        curso.LaboratorioId = entrada.LaboratorioId;
    }
}

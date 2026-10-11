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

// Equipo es la etiqueta de la computadora del laboratorio; EquipoPropio, que el alumno trae la suya
public record EntradaAlumno(string? Matricula, string? Equipo, bool? EquipoPropio);

public record EntradaRegistradaDto(
    string Alumno, string Materia, string Grupo, string Laboratorio, string? Equipo, DateTime HoraEntrada);

public record AsistenciaCursoDto(int TotalClases, IReadOnlyList<AsistenciaAlumnoDto> Alumnos);

public record AsistenciaAlumnoDto(
    int AlumnoId, string Matricula, string Nombre, string Apellido, int Asistencias, int Faltas, decimal? Porcentaje);

public static class ClasesEndpoints
{
    public const string LimiteRegistro = "registro";
    public const int MaximoListado = 100;

    public static void MapClases(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/clases").WithTags("Clases").RequireAuthorization();

        grupo.MapGet("/", async (bool? abiertas, int? cursoId, int? laboratorioId, int? docenteId, AsistenciasDbContext db) =>
            await ADto(db.Clases
                    .Where(c => (abiertas == null || (c.CerradaEn == null) == abiertas)
                        && (cursoId == null || c.CursoId == cursoId)
                        && (laboratorioId == null || c.LaboratorioId == laboratorioId)
                        && (docenteId == null || c.Curso!.DocenteId == docenteId))
                    .OrderByDescending(c => c.AbiertaEn).ThenByDescending(c => c.Id)
                    .Take(MaximoListado))
                .ToListAsync());

        grupo.MapGet("/{id:int}", async (int id, AsistenciasDbContext db) =>
            await ADto(db.Clases.Where(c => c.Id == id)).SingleOrDefaultAsync() is { } clase
                ? Results.Ok(clase)
                : Persistencia.NoExiste("La clase")).Produces<ClaseDto>();

        grupo.MapGet("/{id:int}/asistencias", async (int id, AsistenciasDbContext db) =>
            await db.Clases.AnyAsync(c => c.Id == id)
                ? Results.Ok(await AsistentesAsync(db, id))
                : Persistencia.NoExiste("La clase")).Produces<List<AsistenteDto>>();

        // La lista del grupo con quién asistió y quién faltó
        grupo.MapGet("/{id:int}/lista", async (int id, AsistenciasDbContext db) =>
        {
            var cursoId = await db.Clases.Where(c => c.Id == id).Select(c => (int?)c.CursoId).SingleOrDefaultAsync();
            if (cursoId is null) return Persistencia.NoExiste("La clase");

            var inscritos = await db.Inscripciones.AsNoTracking()
                .Where(i => i.CursoId == cursoId)
                .Select(i => new AlumnoDeLista(i.Alumno!.Id, i.Alumno.Matricula, i.Alumno.Nombre, i.Alumno.Apellido))
                .ToListAsync();
            return Results.Ok(ListaDeAsistencia.Armar(inscritos, await AsistentesAsync(db, id)));
        }).Produces<ListaDeClaseDto>();

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
        }).Produces<ClaseDto>(StatusCodes.Status201Created);

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
        }).Produces<ClaseDto>();

        // Para el alumno que no pudo registrarse solo: lo registra quien lleva la clase
        grupo.MapPost("/{id:int}/asistencias", async (
            int id, EntradaAlumno entrada, ClaimsPrincipal principal, AsistenciasDbContext db) =>
        {
            var docenteId = await db.Clases.Where(c => c.Id == id).Select(c => new { c.Curso!.DocenteId }).SingleOrDefaultAsync();
            if (docenteId is null) return Persistencia.NoExiste("La clase");
            if (!ReglasClase.PuedeCorregir(principal.RolUsuario(), principal.IdUsuario(), docenteId.DocenteId))
                return Results.Forbid();

            var v = new Validador();
            var matricula = v.Texto("matricula", entrada.Matricula, 20);
            var etiqueta = EquipoIndicado(v, entrada);
            if (v.Error is { } error) return error;

            var clase = await Abiertas(db.Clases.Where(c => c.Id == id)).SingleOrDefaultAsync();
            if (clase is null) return Conflicto("La clase ya está cerrada.");

            var alumno = await db.Alumnos.AsNoTracking().SingleOrDefaultAsync(a => a.Matricula == matricula && a.Activo);
            if (alumno is null) return SinAlumno();
            if (!await db.Inscripciones.AnyAsync(i => i.CursoId == clase.CursoId && i.AlumnoId == alumno.Id))
                return Conflicto("El alumno no está inscrito en el curso.");

            return await RegistrarAsync(db, alumno, clase, etiqueta);
        }).Produces<EntradaRegistradaDto>();

        // Para quitar una entrada registrada por error. Queda anotado en la bitácora quién la quitó.
        grupo.MapDelete("/{id:int}/asistencias/{asistenciaId:long}", async (
            int id, long asistenciaId, ClaimsPrincipal principal, AsistenciasDbContext db) =>
        {
            var docenteId = await db.Clases.Where(c => c.Id == id).Select(c => new { c.Curso!.DocenteId }).SingleOrDefaultAsync();
            if (docenteId is null) return Persistencia.NoExiste("La clase");
            if (!ReglasClase.PuedeCorregir(principal.RolUsuario(), principal.IdUsuario(), docenteId.DocenteId))
                return Results.Forbid();

            var asistencia = await db.Asistencias.SingleOrDefaultAsync(a => a.Id == asistenciaId && a.ClaseId == id);
            if (asistencia is null) return Persistencia.NoExiste("La asistencia");

            db.Asistencias.Remove(asistencia);
            return await db.GuardarAsync() ?? Results.NoContent();
        });

        // Sin sesión: los alumnos no tienen cuenta y registran su entrada con su número de control.
        // Solo entra quien está inscrito en un curso que tiene la clase abierta en ese momento.
        app.MapPost("/api/registro/entrada", async (EntradaAlumno entrada, AsistenciasDbContext db) =>
        {
            var v = new Validador();
            var matricula = v.Texto("matricula", entrada.Matricula, 20);
            var etiqueta = EquipoIndicado(v, entrada);
            if (v.Error is { } error) return error;

            var alumno = await db.Alumnos.AsNoTracking().SingleOrDefaultAsync(a => a.Matricula == matricula && a.Activo);
            if (alumno is null) return SinAlumno();

            var clase = await Abiertas(db.Clases
                    .Where(c => c.Curso!.Inscripciones.Any(i => i.AlumnoId == alumno.Id))
                    .OrderByDescending(c => c.AbiertaEn))
                .FirstOrDefaultAsync();
            if (clase is null) return Conflicto("No tienes ninguna clase abierta en este momento.");

            return await RegistrarAsync(db, alumno, clase, etiqueta);
        }).WithTags("Registro").RequireRateLimiting(LimiteRegistro).Produces<EntradaRegistradaDto>();

        // Asistencias y faltas de cada alumno inscrito, sobre las clases que se han dado del curso.
        // Las inscripciones no guardan fecha: a quien se inscribió tarde le cuentan como faltas las clases anteriores.
        app.MapGet("/api/cursos/{id:int}/asistencia", async (int id, AsistenciasDbContext db) =>
        {
            if (!await db.Cursos.AnyAsync(c => c.Id == id))
                return Persistencia.NoExiste("El curso");

            var totalClases = await db.Clases.CountAsync(c => c.CursoId == id);
            var alumnos = await db.Inscripciones.AsNoTracking()
                .Where(i => i.CursoId == id)
                .OrderBy(i => i.Alumno!.Apellido).ThenBy(i => i.Alumno!.Nombre).ThenBy(i => i.AlumnoId)
                .Select(i => new AlumnoDeLista(i.Alumno!.Id, i.Alumno.Matricula, i.Alumno.Nombre, i.Alumno.Apellido))
                .ToListAsync();
            // Una clase cuenta una sola vez por alumno, aunque tenga dos entradas en ella
            var asistencias = (await db.Asistencias
                    .Where(a => a.CursoId == id && a.ClaseId != null && a.AlumnoId != null)
                    .Select(a => new { a.AlumnoId, a.ClaseId })
                    .Distinct()
                    .ToListAsync())
                .GroupBy(a => a.AlumnoId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            return Results.Ok(new AsistenciaCursoDto(totalClases, alumnos
                .Select(a =>
                {
                    var suyas = asistencias.GetValueOrDefault(a.Id);
                    return new AsistenciaAlumnoDto(
                        a.Id, a.Matricula, a.Nombre, a.Apellido, suyas, totalClases - suyas,
                        ListaDeAsistencia.Porcentaje(suyas, totalClases));
                })
                .ToList()));
        }).WithTags("Clases").RequireAuthorization().Produces<AsistenciaCursoDto>();
    }

    private sealed record ClaseAbierta(
        int Id, int CursoId, int LaboratorioId, int? PracticaId, string Materia, string Grupo, string Laboratorio);

    private static IQueryable<ClaseAbierta> Abiertas(IQueryable<Clase> clases) =>
        clases.AsNoTracking().Where(c => c.CerradaEn == null).Select(c => new ClaseAbierta(
            c.Id, c.CursoId, c.LaboratorioId, c.PracticaId,
            c.Curso!.Materia!.Nombre, c.Curso.Grupo, c.Laboratorio!.Nombre));

    // Siempre se sabe qué ocupa el alumno: una computadora del laboratorio o la suya, nunca ninguna ni las dos.
    // Devuelve la etiqueta, o null si trae su propio equipo.
    private static string? EquipoIndicado(Validador v, EntradaAlumno entrada)
    {
        var etiqueta = v.Opcional("equipo", entrada.Equipo, 50);
        var propio = entrada.EquipoPropio == true;
        v.Si(etiqueta is null && !propio, "equipo", "Indica la computadora que ocupas o marca que traes tu propio equipo.");
        v.Si(etiqueta is not null && propio, "equipo", "Indica una computadora o tu propio equipo, no las dos cosas.");
        return etiqueta;
    }

    // Lo que tienen en común el registro que hace el alumno y el que hace quien lleva la clase
    private static async Task<IResult> RegistrarAsync(
        AsistenciasDbContext db, Alumno alumno, ClaseAbierta clase, string? etiqueta)
    {
        if (await db.Asistencias.AnyAsync(a => a.AlumnoId == alumno.Id && a.HoraSalida == null))
            return Conflicto("Ya hay una entrada registrada para ese número de control.");

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
    }

    private static Task<List<AsistenteDto>> AsistentesAsync(AsistenciasDbContext db, int claseId) =>
        db.Asistencias.AsNoTracking()
            .Where(a => a.ClaseId == claseId && a.AlumnoId != null)
            .OrderBy(a => a.Alumno!.Apellido).ThenBy(a => a.Alumno!.Nombre).ThenBy(a => a.Id)
            .Select(a => new AsistenteDto(
                a.Id, a.Alumno!.Id, a.Alumno.Matricula, a.Alumno.Nombre, a.Alumno.Apellido,
                a.Equipo!.Etiqueta, a.HoraEntrada, a.HoraSalida))
            .ToListAsync();

    private static IResult Conflicto(string titulo) => Results.Problem(statusCode: 409, title: titulo);

    private static IResult SinAlumno() =>
        Results.Problem(statusCode: 404, title: "No hay un alumno activo con ese número de control.");

    private static IQueryable<ClaseDto> ADto(IQueryable<Clase> clases) =>
        clases.AsNoTracking().Select(c => new ClaseDto(
            c.Id, c.CursoId, c.Curso!.Materia!.Nombre, c.Curso.Grupo, c.LaboratorioId, c.Laboratorio!.Nombre,
            c.PracticaId, c.Practica!.Titulo, c.AbiertaPorId, c.AbiertaPor!.Nombre + " " + c.AbiertaPor.Apellido,
            c.AbiertaEn, c.CerradaEn, c.Asistencias.Count()));
}

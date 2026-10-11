using Asistencias.Api.Catalogos;
using Asistencias.Api.Comun;
using Asistencias.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Alumnos;

public record AlumnoDto(
    int Id, string Matricula, string Nombre, string Apellido, int? CarreraId, string? Carrera,
    byte? Semestre, Turno? Turno, string? Grupo, bool Activo);

public record AlumnoEntrada(
    string? Matricula, string? Nombre, string? Apellido, int? CarreraId,
    byte? Semestre, Turno? Turno, string? Grupo, bool? Activo);

public record Pagina<T>(int Total, int NumeroPagina, int Tamano, IReadOnlyList<T> Elementos);

public record LoteAlumnosDto(int Creados, int Actualizados);

public static class AlumnosEndpoints
{
    public const int TamanoMaximo = 100;
    public const int LoteMaximo = 2000;

    public static void MapAlumnos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/alumnos").WithTags("Alumnos").RequireAuthorization();

        // Paginado: la lista de alumnos puede llegar a miles
        grupo.MapGet("/", async (
            string? buscar, int? carreraId, bool? activo, int? pagina, int? tamano, AsistenciasDbContext db) =>
        {
            var numero = Math.Max(pagina ?? 1, 1);
            var porPagina = Math.Clamp(tamano ?? 25, 1, TamanoMaximo);
            var texto = buscar?.Trim();

            var consulta = db.Alumnos.AsNoTracking().Where(a =>
                (carreraId == null || a.CarreraId == carreraId)
                && (activo == null || a.Activo == activo)
                && (string.IsNullOrEmpty(texto)
                    || a.Matricula.StartsWith(texto)
                    || (a.Nombre + " " + a.Apellido).Contains(texto)));

            var total = await consulta.CountAsync();
            var elementos = await ADto(consulta.OrderBy(a => a.Apellido).ThenBy(a => a.Nombre).ThenBy(a => a.Id))
                .Skip((numero - 1) * porPagina).Take(porPagina)
                .ToListAsync();
            return new Pagina<AlumnoDto>(total, numero, porPagina, elementos);
        });

        grupo.MapGet("/{id:int}", async (int id, AsistenciasDbContext db) =>
            await ADto(db.Alumnos.AsNoTracking().Where(a => a.Id == id)).SingleOrDefaultAsync() is { } alumno
                ? Results.Ok(alumno)
                : Persistencia.NoExiste("El alumno"));

        grupo.MapPost("/", async (AlumnoEntrada entrada, AsistenciasDbContext db) =>
        {
            var v = new Validador();
            var alumno = new Alumno { Matricula = "", Nombre = "", Apellido = "" };
            Aplicar(alumno, entrada, v);
            if (v.Error is { } error) return error;

            db.Alumnos.Add(alumno);
            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Created($"/api/alumnos/{alumno.Id}", await ADto(db.Alumnos.Where(a => a.Id == alumno.Id)).SingleAsync());
        }).RequireAuthorization(Politicas.Personal);

        // Carga de una lista completa: crea a los que no existen y actualiza a los que sí, según la matrícula.
        // Si un solo renglón tiene un error no se guarda ninguno, para no dejar la lista a medias.
        grupo.MapPost("/lote", async (List<AlumnoEntrada> entradas, AsistenciasDbContext db) =>
        {
            if (entradas.Count is 0 or > LoteMaximo)
                return Results.Problem(statusCode: 400, title: $"La lista debe tener entre 1 y {LoteMaximo} alumnos.");

            var matriculas = entradas.Select(e => e.Matricula?.Trim() ?? "").ToList();
            var existentes = await db.Alumnos.Where(a => matriculas.Contains(a.Matricula)).ToListAsync();
            var porMatricula = existentes.ToDictionary(a => a.Matricula, StringComparer.OrdinalIgnoreCase);
            var errores = new Dictionary<string, string[]>();
            var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var creados = 0;

            for (var i = 0; i < entradas.Count; i++)
            {
                var v = new Validador();
                v.Si(matriculas[i].Length > 0 && !vistas.Add(matriculas[i]), "matricula", "Está repetida en la lista.");
                if (!porMatricula.TryGetValue(matriculas[i], out var alumno))
                {
                    alumno = new Alumno { Matricula = "", Nombre = "", Apellido = "" };
                    db.Alumnos.Add(alumno);
                    creados++;
                }
                Aplicar(alumno, entradas[i], v);
                foreach (var (campo, mensajes) in v.Errores)
                    errores[$"[{i}].{campo}"] = mensajes;
            }
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Ok(new LoteAlumnosDto(creados, entradas.Count - creados));
        }).RequireAuthorization(Politicas.Personal);

        grupo.MapPut("/{id:int}", async (int id, AlumnoEntrada entrada, AsistenciasDbContext db) =>
        {
            var alumno = await db.Alumnos.FindAsync(id);
            if (alumno is null) return Persistencia.NoExiste("El alumno");

            var v = new Validador();
            Aplicar(alumno, entrada, v);
            if (v.Error is { } error) return error;

            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Ok(await ADto(db.Alumnos.Where(a => a.Id == id)).SingleAsync());
        }).RequireAuthorization(Politicas.Personal);

        grupo.MapDelete("/{id:int}", async (int id, AsistenciasDbContext db) =>
        {
            var alumno = await db.Alumnos.FindAsync(id);
            if (alumno is null) return Persistencia.NoExiste("El alumno");

            db.Alumnos.Remove(alumno);
            return await db.GuardarAsync() ?? Results.NoContent();
        }).RequireAuthorization(Politicas.SoloAdmin);
    }

    private static IQueryable<AlumnoDto> ADto(IQueryable<Alumno> alumnos) =>
        alumnos.Select(a => new AlumnoDto(
            a.Id, a.Matricula, a.Nombre, a.Apellido, a.CarreraId, a.Carrera!.Nombre,
            a.Semestre, a.Turno, a.Grupo, a.Activo));

    private static void Aplicar(Alumno alumno, AlumnoEntrada entrada, Validador v)
    {
        alumno.Matricula = v.Texto("matricula", entrada.Matricula, 20);
        alumno.Nombre = v.Texto("nombre", entrada.Nombre, 100);
        alumno.Apellido = v.Texto("apellido", entrada.Apellido, 100);
        alumno.CarreraId = entrada.CarreraId;
        v.Si(entrada.Semestre is 0 or > CatalogosEndpoints.SemestreMaximo, "semestre",
            $"Debe estar entre 1 y {CatalogosEndpoints.SemestreMaximo}.");
        alumno.Semestre = entrada.Semestre;
        alumno.Turno = entrada.Turno;
        alumno.Grupo = v.Opcional("grupo", entrada.Grupo, 20);
        alumno.Activo = entrada.Activo ?? alumno.Activo;
    }
}

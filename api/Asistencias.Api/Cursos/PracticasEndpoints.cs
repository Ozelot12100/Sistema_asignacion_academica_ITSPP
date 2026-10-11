using System.Security.Claims;
using Asistencias.Api.Autenticacion;
using Asistencias.Api.Comun;
using Asistencias.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Cursos;

public record PracticaDto(int Id, int MateriaId, int Numero, string Titulo, string? Descripcion, int? DuracionMinutos);

public record PracticaEntrada(int? Numero, string? Titulo, string? Descripcion, int? DuracionMinutos);

public static class PracticasEndpoints
{
    public const int NumeroMaximo = 999;
    public const int DuracionMaxima = 1440;

    public static void MapPracticas(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api").WithTags("Prácticas").RequireAuthorization();

        grupo.MapGet("/materias/{idMateria:int}/practicas", async (int idMateria, AsistenciasDbContext db) =>
        {
            if (!await db.Materias.AnyAsync(m => m.Id == idMateria))
                return Persistencia.NoExiste("La materia");

            return Results.Ok(await db.Practicas.AsNoTracking()
                .Where(p => p.MateriaId == idMateria)
                .OrderBy(p => p.Numero)
                .Select(p => new PracticaDto(p.Id, p.MateriaId, p.Numero, p.Titulo, p.Descripcion, p.DuracionMinutos))
                .ToListAsync());
        });

        grupo.MapPost("/materias/{idMateria:int}/practicas", async (
            int idMateria, PracticaEntrada entrada, ClaimsPrincipal principal, AsistenciasDbContext db) =>
        {
            if (!await db.Materias.AnyAsync(m => m.Id == idMateria))
                return Persistencia.NoExiste("La materia");
            if (!await PuedeEditarAsync(principal, idMateria, db)) return Results.Forbid();

            var v = new Validador();
            var practica = new Practica { MateriaId = idMateria, Titulo = "" };
            Aplicar(practica, entrada, v);
            if (v.Error is { } error) return error;

            db.Practicas.Add(practica);
            if (await db.GuardarAsync() is { } conflicto) return conflicto;
            return Results.Created($"/api/practicas/{practica.Id}", ADto(practica));
        });

        grupo.MapPut("/practicas/{id:int}", async (
            int id, PracticaEntrada entrada, ClaimsPrincipal principal, AsistenciasDbContext db) =>
        {
            var practica = await db.Practicas.FindAsync(id);
            if (practica is null) return Persistencia.NoExiste("La práctica");
            if (!await PuedeEditarAsync(principal, practica.MateriaId, db)) return Results.Forbid();

            var v = new Validador();
            Aplicar(practica, entrada, v);
            if (v.Error is { } error) return error;

            return await db.GuardarAsync() ?? Results.Ok(ADto(practica));
        });

        grupo.MapDelete("/practicas/{id:int}", async (int id, ClaimsPrincipal principal, AsistenciasDbContext db) =>
        {
            var practica = await db.Practicas.FindAsync(id);
            if (practica is null) return Persistencia.NoExiste("La práctica");
            if (!await PuedeEditarAsync(principal, practica.MateriaId, db)) return Results.Forbid();

            db.Practicas.Remove(practica);
            return await db.GuardarAsync() ?? Results.NoContent();
        });
    }

    private static async Task<bool> PuedeEditarAsync(ClaimsPrincipal principal, int idMateria, AsistenciasDbContext db)
    {
        var idUsuario = principal.IdUsuario();
        return ReglasCurso.PuedeEditarPracticas(
            principal.RolUsuario(),
            await db.Cursos.AnyAsync(c => c.MateriaId == idMateria && c.DocenteId == idUsuario));
    }

    private static PracticaDto ADto(Practica p) =>
        new(p.Id, p.MateriaId, p.Numero, p.Titulo, p.Descripcion, p.DuracionMinutos);

    private static void Aplicar(Practica practica, PracticaEntrada entrada, Validador v)
    {
        v.Si(entrada.Numero is null or < 1 or > NumeroMaximo, "numero", $"Debe estar entre 1 y {NumeroMaximo}.");
        practica.Numero = entrada.Numero ?? practica.Numero;
        practica.Titulo = v.Texto("titulo", entrada.Titulo, 150);
        practica.Descripcion = v.Opcional("descripcion", entrada.Descripcion, 5000);
        v.Si(entrada.DuracionMinutos is < 1 or > DuracionMaxima, "duracionMinutos", $"Debe estar entre 1 y {DuracionMaxima}.");
        practica.DuracionMinutos = entrada.DuracionMinutos;
    }
}

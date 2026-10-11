using Asistencias.Api.Alumnos;
using Asistencias.Api.Comun;
using Asistencias.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Api.Auditoria;

public record EntradaBitacoraDto(
    long Id, DateTime Fecha, int? UsuarioId, string? Usuario, string Accion, string? Entidad, string? EntidadId, string? Detalles);

public static class BitacoraEndpoints
{
    public static void MapBitacora(this IEndpointRouteBuilder app)
    {
        // Solo consulta: la bitácora no se edita ni se borra
        app.MapGet("/api/bitacora", async (
            int? usuarioId, string? accion, string? entidad, string? entidadId, DateTime? desde, DateTime? hasta,
            int? pagina, int? tamano, AsistenciasDbContext db) =>
        {
            var numero = Math.Max(pagina ?? 1, 1);
            var porPagina = Math.Clamp(tamano ?? 50, 1, AlumnosEndpoints.TamanoMaximo);

            var consulta = db.Bitacora.AsNoTracking().Where(b =>
                (usuarioId == null || b.UsuarioId == usuarioId)
                && (accion == null || b.Accion == accion)
                && (entidad == null || b.Entidad == entidad)
                && (entidadId == null || b.EntidadId == entidadId)
                && (desde == null || b.Fecha >= desde)
                && (hasta == null || b.Fecha < hasta));

            var total = await consulta.CountAsync();
            var elementos = await consulta
                .OrderByDescending(b => b.Id)
                .Skip((numero - 1) * porPagina).Take(porPagina)
                .Select(b => new EntradaBitacoraDto(
                    b.Id, b.Fecha, b.UsuarioId,
                    db.Usuarios.Where(u => u.Id == b.UsuarioId).Select(u => u.Nombre + " " + u.Apellido).FirstOrDefault(),
                    b.Accion, b.Entidad, b.EntidadId, b.Detalles))
                .ToListAsync();
            return new Pagina<EntradaBitacoraDto>(total, numero, porPagina, elementos);
        }).WithTags("Bitácora").RequireAuthorization(Politicas.SoloAdmin);
    }
}

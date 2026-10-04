using Asistencias.Api.Datos;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Asistencias.Api.Comun;

public static class Persistencia
{
    // Guarda y traduce a una respuesta los rechazos de la base que son culpa de los datos enviados.
    // Devuelve null si se guardó.
    public static async Task<IResult?> GuardarAsync(this AsistenciasDbContext db)
    {
        try
        {
            await db.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            return Results.Problem(statusCode: 409, title: "Ya existe un registro con esos datos.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.RowIsReferenced2 })
        {
            return Results.Problem(statusCode: 409,
                title: "No se puede eliminar porque otros registros dependen de él. Desactívalo en su lugar.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.NoReferencedRow2 })
        {
            return Results.Problem(statusCode: 400, title: "Uno de los registros relacionados no existe.");
        }
    }

    public static IResult NoExiste(string que) => Results.Problem(statusCode: 404, title: $"{que} no existe.");
}

public static class Politicas
{
    public const string SoloAdmin = "solo-admin";
    // Administradores y laboratoristas: quienes operan los laboratorios
    public const string Personal = "personal";

    public static void Registrar(Microsoft.AspNetCore.Authorization.AuthorizationOptions options)
    {
        options.AddPolicy(SoloAdmin, p => p.RequireRole("admin"));
        options.AddPolicy(Personal, p => p.RequireRole("admin", "laboratorista"));
    }
}

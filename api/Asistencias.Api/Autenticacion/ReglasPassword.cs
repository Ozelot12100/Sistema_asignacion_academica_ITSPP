using Asistencias.Api.Comun;

namespace Asistencias.Api.Autenticacion;

public static class ReglasPassword
{
    // Se exige longitud y no mezcla de símbolos: una frase larga es más segura y más fácil de recordar
    public const int LongitudMinima = 12;
    public const int LongitudMaxima = 128;

    public static bool EsValida(string? password) =>
        password is { Length: >= LongitudMinima and <= LongitudMaxima };

    public static void Validar(Validador v, string campo, string? password) =>
        v.Si(!EsValida(password), campo, $"Debe tener entre {LongitudMinima} y {LongitudMaxima} caracteres.");
}

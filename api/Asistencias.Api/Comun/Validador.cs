using System.Net.Mail;

namespace Asistencias.Api.Comun;

// Acumula los errores de validación de una petición para responderlos todos juntos en un 400
public class Validador
{
    private readonly Dictionary<string, string[]> _errores = [];

    public string Texto(string campo, string? valor, int maximo)
    {
        var texto = valor?.Trim() ?? "";
        if (texto.Length == 0) Agregar(campo, "Es obligatorio.");
        else if (texto.Length > maximo) Agregar(campo, $"Máximo {maximo} caracteres.");
        return texto;
    }

    public string? Opcional(string campo, string? valor, int maximo)
    {
        var texto = valor?.Trim();
        if (string.IsNullOrEmpty(texto)) return null;
        if (texto.Length > maximo) Agregar(campo, $"Máximo {maximo} caracteres.");
        return texto;
    }

    public string Email(string campo, string? valor)
    {
        var email = Texto(campo, valor, 150);
        if (email.Length > 0 && (!MailAddress.TryCreate(email, out var direccion) || direccion.Address != email))
            Agregar(campo, "No es un correo válido.");
        return email;
    }

    public void Si(bool condicion, string campo, string mensaje)
    {
        if (condicion) Agregar(campo, mensaje);
    }

    public IReadOnlyDictionary<string, string[]> Errores => _errores;

    public IResult? Error => _errores.Count == 0 ? null : Results.ValidationProblem(_errores);

    private void Agregar(string campo, string mensaje) => _errores.TryAdd(campo, [mensaje]);
}

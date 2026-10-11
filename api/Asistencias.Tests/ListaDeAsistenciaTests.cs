using Asistencias.Api.Clases;

namespace Asistencias.Tests;

public class ListaDeAsistenciaTests
{
    private static readonly DateTime Inicio = new(2026, 10, 12, 15, 0, 0, DateTimeKind.Utc);

    private static AsistenteDto Entrada(long id, int alumnoId, string apellido, int minuto = 0) =>
        new(id, alumnoId, $"M{alumnoId}", "Nombre", apellido, null, Inicio.AddMinutes(minuto), null);

    [Fact]
    public void Cuenta_presentes_y_faltas_y_ordena_por_apellido()
    {
        AlumnoDeLista[] inscritos = [new(1, "M1", "Nombre", "Zavala"), new(2, "M2", "Nombre", "Acosta"), new(3, "M3", "Nombre", "Mora")];
        AsistenteDto[] asistentes = [Entrada(10, 1, "Zavala"), Entrada(11, 3, "Mora")];

        var lista = ListaDeAsistencia.Armar(inscritos, asistentes);

        Assert.Equal((3, 2, 1), (lista.Inscritos, lista.Presentes, lista.Faltas));
        Assert.Equal(["Acosta", "Mora", "Zavala"], lista.Alumnos.Select(r => r.Apellido));
        Assert.Equal([false, true, true], lista.Alumnos.Select(r => r.Presente));
    }

    [Fact]
    public void Quien_asistio_sin_seguir_inscrito_aparece_pero_no_cuenta_como_inscrito()
    {
        AlumnoDeLista[] inscritos = [new(1, "M1", "Nombre", "Acosta")];
        AsistenteDto[] asistentes = [Entrada(10, 9, "Baja")];

        var lista = ListaDeAsistencia.Armar(inscritos, asistentes);

        Assert.Equal((1, 1, 1), (lista.Inscritos, lista.Presentes, lista.Faltas));
        var deBaja = Assert.Single(lista.Alumnos, r => r.AlumnoId == 9);
        Assert.False(deBaja.Inscrito);
        Assert.True(deBaja.Presente);
    }

    [Fact]
    public void Dos_entradas_del_mismo_alumno_cuentan_una_vez_y_se_muestra_la_primera()
    {
        AlumnoDeLista[] inscritos = [new(1, "M1", "Nombre", "Acosta")];
        AsistenteDto[] asistentes = [Entrada(11, 1, "Acosta", minuto: 20), Entrada(10, 1, "Acosta", minuto: 5)];

        var renglon = Assert.Single(ListaDeAsistencia.Armar(inscritos, asistentes).Alumnos);

        Assert.Equal(10, renglon.AsistenciaId);
    }

    [Theory]
    [InlineData(3, 3, "100")]
    [InlineData(1, 3, "33.3")]
    [InlineData(2, 3, "66.7")]
    [InlineData(0, 4, "0")]
    public void Porcentaje_de_asistencia(int asistencias, int totalClases, string esperado) =>
        Assert.Equal(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture),
            ListaDeAsistencia.Porcentaje(asistencias, totalClases));

    [Fact]
    public void Sin_clases_no_hay_porcentaje() => Assert.Null(ListaDeAsistencia.Porcentaje(0, 0));
}

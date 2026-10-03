using AsignacionAcademica.Api.Horarios;

namespace AsignacionAcademica.Tests;

public class ReglasHorarioTests
{
    private static Franja F(string inicio, string fin)
    {
        Assert.True(ReglasHorario.TryCrearFranja(inicio, fin, out var franja));
        return franja;
    }

    [Theory]
    [InlineData("08:00", "10:00")]
    [InlineData("07:00", "07:50")]
    [InlineData("19:10", "20:00")]
    public void TryCrearFranja_acepta_horas_validas(string inicio, string fin) =>
        Assert.True(ReglasHorario.TryCrearFranja(inicio, fin, out _));

    [Theory]
    [InlineData(null, "10:00")]
    [InlineData("08:00", null)]
    [InlineData("8:00", "10:00")]
    [InlineData("08:00", "24:00")]
    [InlineData("08:60", "10:00")]
    [InlineData("10:00", "10:00")]
    [InlineData("10:00", "08:00")]
    [InlineData("08:00:00", "10:00:00")]
    public void TryCrearFranja_rechaza_formato_o_rango_invalido(string? inicio, string? fin) =>
        Assert.False(ReglasHorario.TryCrearFranja(inicio, fin, out _));

    [Theory]
    [InlineData("07:00", "20:00", true)]
    [InlineData("07:00", "07:50", true)]
    [InlineData("06:59", "08:00", false)]
    [InlineData("19:00", "20:01", false)]
    [InlineData("19:00", "20:59", false)]
    public void DentroDeJornada_considera_los_minutos(string inicio, string fin, bool esperado) =>
        Assert.Equal(esperado, ReglasHorario.DentroDeJornada(F(inicio, fin)));

    [Theory]
    [InlineData("08:00", "10:00", "08:00", "10:00", true)]
    [InlineData("08:00", "10:00", "09:00", "11:00", true)]
    [InlineData("08:00", "10:00", "07:00", "08:30", true)]
    [InlineData("08:00", "12:00", "09:00", "10:00", true)]
    [InlineData("09:00", "10:00", "08:00", "12:00", true)]
    [InlineData("08:00", "10:00", "10:00", "12:00", false)]
    [InlineData("10:00", "12:00", "08:00", "10:00", false)]
    [InlineData("08:00", "09:00", "13:00", "14:00", false)]
    public void SeTraslapaCon_detecta_traslapes_parciales(string i1, string f1, string i2, string f2, bool esperado)
    {
        Assert.Equal(esperado, F(i1, f1).SeTraslapaCon(F(i2, f2)));
        Assert.Equal(esperado, F(i2, f2).SeTraslapaCon(F(i1, f1)));
    }

    [Fact]
    public void EstaCubierta_con_un_bloque_mas_amplio() =>
        Assert.True(ReglasHorario.EstaCubierta(F("08:00", "10:00"), [F("08:00", "12:00")]));

    [Fact]
    public void EstaCubierta_con_bloques_contiguos_en_cualquier_orden() =>
        Assert.True(ReglasHorario.EstaCubierta(F("08:00", "10:00"), [F("09:00", "10:00"), F("08:00", "09:00")]));

    [Fact]
    public void EstaCubierta_falla_si_hay_un_hueco() =>
        Assert.False(ReglasHorario.EstaCubierta(F("08:00", "11:00"), [F("08:00", "09:00"), F("10:00", "11:00")]));

    [Fact]
    public void EstaCubierta_falla_si_empieza_antes_o_termina_despues()
    {
        Assert.False(ReglasHorario.EstaCubierta(F("07:30", "09:00"), [F("08:00", "12:00")]));
        Assert.False(ReglasHorario.EstaCubierta(F("11:00", "12:30"), [F("08:00", "12:00")]));
    }

    [Fact]
    public void EstaCubierta_falla_sin_disponibilidad() =>
        Assert.False(ReglasHorario.EstaCubierta(F("08:00", "10:00"), []));
}

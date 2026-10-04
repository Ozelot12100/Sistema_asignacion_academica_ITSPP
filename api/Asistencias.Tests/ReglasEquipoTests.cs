using Asistencias.Api.Laboratorios;

namespace Asistencias.Tests;

public class ReglasEquipoTests
{
    [Theory]
    [InlineData(false, false, EstadoEquipo.Libre)]
    [InlineData(false, true, EstadoEquipo.EnUso)]
    [InlineData(true, false, EstadoEquipo.FueraDeServicio)]
    [InlineData(true, true, EstadoEquipo.FueraDeServicio)]
    public void Estado_del_equipo(bool fueraDeServicio, bool ocupado, EstadoEquipo esperado) =>
        Assert.Equal(esperado, ReglasEquipo.Estado(fueraDeServicio, ocupado));

    [Fact]
    public void OrdenarPorEtiqueta_usa_orden_natural()
    {
        string[] etiquetas = ["PC-10", "PC-2", "PC-1", "PC-21", "PC-3"];

        Assert.Equal(["PC-1", "PC-2", "PC-3", "PC-10", "PC-21"], etiquetas.OrdenarPorEtiqueta(e => e));
    }
}

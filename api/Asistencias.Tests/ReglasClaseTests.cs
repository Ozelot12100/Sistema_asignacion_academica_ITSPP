using Asistencias.Api.Clases;
using Asistencias.Api.Datos;

namespace Asistencias.Tests;

public class ReglasClaseTests
{
    [Theory]
    [InlineData(Rol.Admin, 1, 2, true)]
    [InlineData(Rol.Docente, 1, 1, true)]
    [InlineData(Rol.Docente, 1, 2, false)]
    [InlineData(Rol.Docente, 1, null, false)]
    [InlineData(Rol.Laboratorista, 1, 1, false)]
    public void Quien_abre_una_clase(Rol rol, int usuarioId, int? docenteDelCurso, bool esperado) =>
        Assert.Equal(esperado, ReglasClase.PuedeAbrir(rol, usuarioId, docenteDelCurso));

    [Theory]
    [InlineData(Rol.Admin, 1, 2, true)]
    [InlineData(Rol.Laboratorista, 1, 2, true)]
    [InlineData(Rol.Docente, 1, 1, true)]
    [InlineData(Rol.Docente, 1, 2, false)]
    public void Quien_cierra_una_clase(Rol rol, int usuarioId, int? docenteDelCurso, bool esperado) =>
        Assert.Equal(esperado, ReglasClase.PuedeCerrar(rol, usuarioId, docenteDelCurso));
}

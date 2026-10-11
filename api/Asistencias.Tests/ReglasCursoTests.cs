using Asistencias.Api.Cursos;
using Asistencias.Api.Datos;

namespace Asistencias.Tests;

public class ReglasCursoTests
{
    [Theory]
    [InlineData(Rol.Docente, true, true)]
    [InlineData(Rol.Docente, false, false)]
    [InlineData(Rol.Laboratorista, true, false)]
    [InlineData(Rol.Admin, true, false)]
    public void Solo_imparte_un_docente_activo(Rol rol, bool activo, bool esperado) =>
        Assert.Equal(esperado, ReglasCurso.PuedeImpartir(rol, activo));

    [Theory]
    [InlineData(Rol.Admin, 1, 2, true)]
    [InlineData(Rol.Laboratorista, 1, null, true)]
    [InlineData(Rol.Docente, 1, 1, true)]
    [InlineData(Rol.Docente, 1, 2, false)]
    [InlineData(Rol.Docente, 1, null, false)]
    public void Quien_lleva_la_lista_de_un_curso(Rol rol, int usuarioId, int? docenteDelCurso, bool esperado) =>
        Assert.Equal(esperado, ReglasCurso.PuedeInscribir(rol, usuarioId, docenteDelCurso));

    [Theory]
    [InlineData(Rol.Admin, false, true)]
    [InlineData(Rol.Docente, true, true)]
    [InlineData(Rol.Docente, false, false)]
    [InlineData(Rol.Laboratorista, true, false)]
    public void Quien_edita_las_practicas_de_una_materia(Rol rol, bool imparteLaMateria, bool esperado) =>
        Assert.Equal(esperado, ReglasCurso.PuedeEditarPracticas(rol, imparteLaMateria));
}

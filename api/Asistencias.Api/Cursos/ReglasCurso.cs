using Asistencias.Api.Datos;

namespace Asistencias.Api.Cursos;

public static class ReglasCurso
{
    // Un curso solo se le asigna a quien tiene rol de docente y la cuenta activa
    public static bool PuedeImpartir(Rol rol, bool activo) => rol == Rol.Docente && activo;

    // La lista de un curso la llevan quienes operan los laboratorios y el docente que lo imparte
    public static bool PuedeInscribir(Rol rol, int usuarioId, int? docenteDelCurso) =>
        rol is Rol.Admin or Rol.Laboratorista || (rol == Rol.Docente && usuarioId == docenteDelCurso);

    // Las prácticas son de la materia: las edita el administrador o un docente que la imparta
    public static bool PuedeEditarPracticas(Rol rol, bool imparteLaMateria) =>
        rol == Rol.Admin || (rol == Rol.Docente && imparteLaMateria);
}

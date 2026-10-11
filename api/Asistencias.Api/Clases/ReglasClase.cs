using Asistencias.Api.Datos;

namespace Asistencias.Api.Clases;

public static class ReglasClase
{
    // La clase la abre quien la imparte; el administrador puede hacerlo en su lugar
    public static bool PuedeAbrir(Rol rol, int usuarioId, int? docenteDelCurso) =>
        rol == Rol.Admin || (rol == Rol.Docente && usuarioId == docenteDelCurso);

    // El laboratorista también la cierra, para liberar el laboratorio si el docente se fue sin hacerlo
    public static bool PuedeCerrar(Rol rol, int usuarioId, int? docenteDelCurso) =>
        rol == Rol.Laboratorista || PuedeAbrir(rol, usuarioId, docenteDelCurso);

    // Registrar a mano a un alumno o quitar una entrada equivocada: los mismos que pueden cerrar la clase
    public static bool PuedeCorregir(Rol rol, int usuarioId, int? docenteDelCurso) =>
        PuedeCerrar(rol, usuarioId, docenteDelCurso);
}

namespace AsignacionAcademica.Api
{
    public record ProfesorDto(string nombre, string especialidad, string titulo);

    public record DisponibilidadDto(
        string dia,
        string hora_inicio,
        string hora_fin,
        bool disponible
    );

    public record AulaDto(
        string nombre,
        int capacidad,
        string tipo
    );

    public record AsignaturaDto(
        string nombre,
        int semestre,
        int creditos
    );

    public record GrupoDto(
        string nombre,
        int numero_alumnos,
        string curso,
        string turno,
        int semestre
    );

    public record ClaseDto(
        int asignaturaId,
        int profesorId,
        int aulaId,
        int grupoId,
        string dia,
        string horaInicio,
        string horaFin
    );

    public record UsuarioDto(
        string nombre,
        string username,
        string correo,
        string contrasena,
        string rol
    );
}

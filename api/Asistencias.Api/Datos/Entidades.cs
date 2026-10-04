namespace Asistencias.Api.Datos;

// Solo están mapeadas las tablas y columnas que la API usa hoy; el esquema completo está en db/esquema.sql.

public enum Rol { Admin, Laboratorista, Docente }

public enum TipoUsuario { Estudiante, Docente, Invitado }

public class Usuario
{
    public int Id { get; set; }
    public Rol Rol { get; set; }
    public string? NumeroEmpleado { get; set; }
    public required string Nombre { get; set; }
    public required string Apellido { get; set; }
    public required string Email { get; set; }
    public string PasswordHash { get; set; } = "";
    public string? Departamento { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public class Laboratorio
{
    public int Id { get; set; }
    public required string Clave { get; set; }
    public required string Nombre { get; set; }
    public string? Ubicacion { get; set; }
    public bool Activo { get; set; } = true;
    public List<Equipo> Equipos { get; set; } = [];
}

public class Equipo
{
    public int Id { get; set; }
    public int LaboratorioId { get; set; }
    public required string Etiqueta { get; set; }
    public bool FueraDeServicio { get; set; }
    public string? Notas { get; set; }
}

public class Asistencia
{
    public long Id { get; set; }
    public int LaboratorioId { get; set; }
    public int? EquipoId { get; set; }
    public TipoUsuario TipoUsuario { get; set; }
    public int? AlumnoId { get; set; }
    public int? UsuarioId { get; set; }
    public string? InvitadoNombre { get; set; }
    public DateTime HoraEntrada { get; set; } = DateTime.UtcNow;
    public DateTime? HoraSalida { get; set; }
}

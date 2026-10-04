namespace Asistencias.Api.Datos;

// Solo están mapeadas las tablas y columnas que la API usa hoy; el esquema completo está en db/esquema.sql.

public enum Rol { Admin, Laboratorista, Docente }

public enum TipoUsuario { Estudiante, Docente, Invitado }

public enum Turno { Matutino, Vespertino }

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

public class Carrera
{
    public int Id { get; set; }
    public required string Clave { get; set; }
    public required string Nombre { get; set; }
    public bool Activa { get; set; } = true;
}

public class Periodo
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
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

public class Alumno
{
    public int Id { get; set; }
    public required string Matricula { get; set; }
    public required string Nombre { get; set; }
    public required string Apellido { get; set; }
    public int? CarreraId { get; set; }
    public Carrera? Carrera { get; set; }
    public byte? Semestre { get; set; }
    public Turno? Turno { get; set; }
    public string? Grupo { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public class Materia
{
    public int Id { get; set; }
    public required string Clave { get; set; }
    public required string Nombre { get; set; }
    public int? CarreraId { get; set; }
    public Carrera? Carrera { get; set; }
    public byte? Semestre { get; set; }
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

public class EntradaBitacora
{
    public long Id { get; set; }
    public int? UsuarioId { get; set; }
    public required string Accion { get; set; }
    public string? Entidad { get; set; }
    public string? EntidadId { get; set; }
    public string? Detalles { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

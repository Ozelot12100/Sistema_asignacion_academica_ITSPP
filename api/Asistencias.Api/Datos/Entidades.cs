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

public class Curso
{
    public int Id { get; set; }
    public int MateriaId { get; set; }
    public Materia? Materia { get; set; }
    public int PeriodoId { get; set; }
    public Periodo? Periodo { get; set; }
    public required string Grupo { get; set; }
    public int? DocenteId { get; set; }
    public Usuario? Docente { get; set; }
    public int? LaboratorioId { get; set; }
    public Laboratorio? Laboratorio { get; set; }
    public List<Inscripcion> Inscripciones { get; set; } = [];
}

public class Inscripcion
{
    public int CursoId { get; set; }
    public Curso? Curso { get; set; }
    public int AlumnoId { get; set; }
    public Alumno? Alumno { get; set; }
}

public class Practica
{
    public int Id { get; set; }
    public int MateriaId { get; set; }
    public int Numero { get; set; }
    public required string Titulo { get; set; }
    public string? Descripcion { get; set; }
    public int? DuracionMinutos { get; set; }
}

public class Clase
{
    public int Id { get; set; }
    public int CursoId { get; set; }
    public Curso? Curso { get; set; }
    public int LaboratorioId { get; set; }
    public Laboratorio? Laboratorio { get; set; }
    public int? PracticaId { get; set; }
    public Practica? Practica { get; set; }
    public int AbiertaPorId { get; set; }
    public Usuario? AbiertaPor { get; set; }
    public DateTime AbiertaEn { get; set; } = DateTime.UtcNow;
    public DateTime? CerradaEn { get; set; }
    public List<Asistencia> Asistencias { get; set; } = [];
}

public class Asistencia
{
    public long Id { get; set; }
    public int LaboratorioId { get; set; }
    public int? EquipoId { get; set; }
    public Equipo? Equipo { get; set; }
    public TipoUsuario TipoUsuario { get; set; }
    public int? AlumnoId { get; set; }
    public Alumno? Alumno { get; set; }
    public int? UsuarioId { get; set; }
    public string? InvitadoNombre { get; set; }
    public int? CursoId { get; set; }
    public int? PracticaId { get; set; }
    public int? ClaseId { get; set; }
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

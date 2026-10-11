using Asistencias.Api.Autenticacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Asistencias.Api.Datos;

// El esquema lo define db/esquema.sql, no EF Core: aquí solo se describe cómo leerlo.
public class AsistenciasDbContext(
    DbContextOptions<AsistenciasDbContext> options, IHttpContextAccessor? http = null) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<Periodo> Periodos => Set<Periodo>();
    public DbSet<Laboratorio> Laboratorios => Set<Laboratorio>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Alumno> Alumnos => Set<Alumno>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Inscripcion> Inscripciones => Set<Inscripcion>();
    public DbSet<Practica> Practicas => Set<Practica>();
    public DbSet<Clase> Clases => Set<Clase>();
    public DbSet<Asistencia> Asistencias => Set<Asistencia>();
    public DbSet<EntradaBitacora> Bitacora => Set<EntradaBitacora>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<FechaUtc>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().Property(u => u.Rol).HasConversion(EnMinusculas<Rol>());
        modelBuilder.Entity<Alumno>().Property(a => a.Turno).HasConversion(EnMinusculas<Turno>());
        modelBuilder.Entity<Asistencia>().Property(a => a.TipoUsuario).HasConversion(EnMinusculas<TipoUsuario>());
        modelBuilder.Entity<Inscripcion>().HasKey(i => new { i.CursoId, i.AlumnoId });
        modelBuilder.Entity<Clase>().Property(c => c.AbiertaPorId).HasColumnName("abierta_por");
    }

    // Las columnas ENUM de la base guardan los valores en minúsculas
    private static ValueConverter<T, string> EnMinusculas<T>() where T : struct, Enum =>
        new(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<T>(v, true));

    // La base guarda las fechas en UTC sin zona; al leerlas se marcan como UTC para que el JSON lleve la "Z"
    private sealed class FechaUtc() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    // Cada alta, cambio o baja queda anotado en la bitácora junto con quién lo hizo, en la misma transacción.
    // Las asistencias no se anotan: ellas mismas son el registro.
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var cambios = ChangeTracker.Entries()
            .Where(e => e.Entity is not (EntradaBitacora or Asistencia)
                && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => new CambioPendiente(e, e.State, e.State == EntityState.Deleted ? Llave(e) : null, Detalles(e)))
            .ToList();

        if (cambios.Count == 0)
            return await base.SaveChangesAsync(cancellationToken);

        await using var transaccion = Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;

        var afectados = await base.SaveChangesAsync(cancellationToken);

        var usuarioId = http?.HttpContext?.User is { Identity.IsAuthenticated: true } principal
            ? principal.IdUsuario()
            : (int?)null;
        foreach (var c in cambios)
            Bitacora.Add(new EntradaBitacora
            {
                UsuarioId = usuarioId,
                Accion = c.Estado switch
                {
                    EntityState.Added => "crear",
                    EntityState.Modified => "modificar",
                    _ => "eliminar",
                },
                Entidad = c.Entrada.Metadata.GetTableName(),
                // La llave de un registro nuevo solo se conoce después de guardarlo
                EntidadId = c.LlaveBorrada ?? Llave(c.Entrada),
                Detalles = c.Detalles,
            });
        await base.SaveChangesAsync(cancellationToken);

        if (transaccion is not null)
            await transaccion.CommitAsync(cancellationToken);
        return afectados;
    }

    private sealed record CambioPendiente(EntityEntry Entrada, EntityState Estado, string? LlaveBorrada, string? Detalles);

    private static string Llave(EntityEntry entrada) =>
        string.Join(",", entrada.Metadata.FindPrimaryKey()!.Properties.Select(p => entrada.Property(p.Name).CurrentValue));

    // Solo los nombres de los campos que cambiaron, nunca sus valores: así no se filtran contraseñas ni datos personales
    private static string? Detalles(EntityEntry entrada) =>
        entrada.State == EntityState.Modified
            ? "campos: " + string.Join(", ", entrada.Properties.Where(p => p.IsModified).Select(p => p.Metadata.GetColumnName()))
            : null;
}

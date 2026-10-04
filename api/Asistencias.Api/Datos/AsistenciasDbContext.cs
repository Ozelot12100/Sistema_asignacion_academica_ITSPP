using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Asistencias.Api.Datos;

// El esquema lo define db/esquema.sql, no EF Core: aquí solo se describe cómo leerlo.
public class AsistenciasDbContext(DbContextOptions<AsistenciasDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Laboratorio> Laboratorios => Set<Laboratorio>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Asistencia> Asistencias => Set<Asistencia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().Property(u => u.Rol).HasConversion(EnMinusculas<Rol>());
        modelBuilder.Entity<Asistencia>().Property(a => a.TipoUsuario).HasConversion(EnMinusculas<TipoUsuario>());
    }

    // Las columnas ENUM de la base guardan los valores en minúsculas
    private static ValueConverter<T, string> EnMinusculas<T>() where T : struct, Enum =>
        new(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<T>(v, true));
}

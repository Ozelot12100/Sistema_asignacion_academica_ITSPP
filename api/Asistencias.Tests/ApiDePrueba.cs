using System.Net.Http.Json;
using Asistencias.Api.Autenticacion;
using Asistencias.Api.Datos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Asistencias.Tests;

// Levanta la API contra una base de datos nueva, creada con db/esquema.sql y db/datos-iniciales.sql,
// y la borra al terminar. Necesita un servidor MariaDB; ver db/README.md.
public class ApiDePrueba : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string PasswordDePrueba = "contraseña-de-prueba";

    private static readonly string Servidor =
        Environment.GetEnvironmentVariable("ASISTENCIAS_TEST_DB") ?? "Server=localhost;Port=3306;Uid=root;Pwd=local;";

    private readonly string _base = $"asistencias_test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"{Servidor}Database={_base};");
        builder.UseSetting("Login:IntentosPorMinuto", "1000");
        builder.UseSetting("Registro:IntentosPorMinuto", "1000");
    }

    public async Task InitializeAsync()
    {
        await using var conexion = new MySqlConnection(Servidor);
        await conexion.OpenAsync();
        await EjecutarAsync(conexion, $"CREATE DATABASE `{_base}`; USE `{_base}`;");
        await EjecutarAsync(conexion, await File.ReadAllTextAsync(RutaEnDb("esquema.sql")));
        await EjecutarAsync(conexion, await File.ReadAllTextAsync(RutaEnDb("datos-iniciales.sql")));
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using var conexion = new MySqlConnection(Servidor);
        await conexion.OpenAsync();
        await EjecutarAsync(conexion, $"DROP DATABASE IF EXISTS `{_base}`;");
    }

    public async Task<Usuario> CrearUsuarioAsync(Rol rol, bool activo = true)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AsistenciasDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

        var usuario = new Usuario
        {
            Rol = rol,
            Nombre = "Usuario",
            Apellido = "De Prueba",
            Email = $"{Guid.NewGuid():N}@prueba.mx",
            Activo = activo,
        };
        usuario.PasswordHash = hasher.HashPassword(usuario, PasswordDePrueba);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    public async Task<HttpClient> ClienteConSesionAsync(Rol rol = Rol.Laboratorista) => (await SesionAsync(rol)).Cliente;

    public async Task<(HttpClient Cliente, Usuario Usuario)> SesionAsync(Rol rol)
    {
        var usuario = await CrearUsuarioAsync(rol);
        var cliente = CreateClient();
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new CredencialesDto(usuario.Email, PasswordDePrueba));
        respuesta.EnsureSuccessStatusCode();
        return (cliente, usuario);
    }

    public async Task<T> EnBaseAsync<T>(Func<AsistenciasDbContext, Task<T>> consulta)
    {
        using var scope = Services.CreateScope();
        return await consulta(scope.ServiceProvider.GetRequiredService<AsistenciasDbContext>());
    }

    public async Task EnBaseAsync(Func<AsistenciasDbContext, Task> accion)
    {
        using var scope = Services.CreateScope();
        await accion(scope.ServiceProvider.GetRequiredService<AsistenciasDbContext>());
    }

    private static async Task EjecutarAsync(MySqlConnection conexion, string sql)
    {
        await using var comando = new MySqlCommand(sql, conexion);
        await comando.ExecuteNonQueryAsync();
    }

    private static string RutaEnDb(string archivo)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var ruta = Path.Combine(dir.FullName, "db", archivo);
            if (File.Exists(ruta)) return ruta;
        }
        throw new FileNotFoundException($"No se encontró db/{archivo} subiendo desde {AppContext.BaseDirectory}.");
    }
}

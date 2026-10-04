using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Asistencias.Api.Autenticacion;
using Asistencias.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Tests;

public class AutenticacionTests(ApiDePrueba api) : IClassFixture<ApiDePrueba>
{
    private static Task<HttpResponseMessage> Login(HttpClient cliente, string? email, string? password) =>
        cliente.PostAsJsonAsync("/api/auth/login", new CredencialesDto(email, password));

    [Fact]
    public async Task Login_correcto_abre_sesion_y_devuelve_al_usuario()
    {
        var usuario = await api.CrearUsuarioAsync(Rol.Docente);
        var cliente = api.CreateClient();

        var respuesta = await Login(cliente, usuario.Email, ApiDePrueba.PasswordDePrueba);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(usuario.Id, (int)cuerpo!["id"]!);
        Assert.Equal("docente", (string)cuerpo["rol"]!);
        Assert.Null(cuerpo["passwordHash"]);

        var yo = await cliente.GetFromJsonAsync<JsonObject>("/api/auth/yo");
        Assert.Equal(usuario.Email, (string)yo!["email"]!);
    }

    [Fact]
    public async Task Login_no_distingue_mayusculas_en_el_correo()
    {
        var usuario = await api.CrearUsuarioAsync(Rol.Admin);

        var respuesta = await Login(api.CreateClient(), usuario.Email.ToUpperInvariant(), ApiDePrueba.PasswordDePrueba);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Login_con_password_incorrecta_responde_401()
    {
        var usuario = await api.CrearUsuarioAsync(Rol.Admin);
        var cliente = api.CreateClient();

        var respuesta = await Login(cliente, usuario.Email, "otra-contraseña");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/auth/yo")).StatusCode);
    }

    [Fact]
    public async Task Login_con_correo_inexistente_responde_igual_que_password_incorrecta()
    {
        var respuesta = await Login(api.CreateClient(), "nadie@prueba.mx", ApiDePrueba.PasswordDePrueba);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Login_de_cuenta_desactivada_responde_401()
    {
        var usuario = await api.CrearUsuarioAsync(Rol.Laboratorista, activo: false);

        var respuesta = await Login(api.CreateClient(), usuario.Email, ApiDePrueba.PasswordDePrueba);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(null, "x")]
    [InlineData("a@prueba.mx", null)]
    [InlineData(" ", "x")]
    public async Task Login_sin_datos_responde_400(string? email, string? password)
    {
        var respuesta = await Login(api.CreateClient(), email, password);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Sin_sesion_las_rutas_protegidas_responden_401()
    {
        var cliente = api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/auth/yo")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/laboratorios")).StatusCode);
    }

    [Fact]
    public async Task Logout_cierra_la_sesion()
    {
        var cliente = await api.ClienteConSesionAsync();

        var respuesta = await cliente.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/auth/yo")).StatusCode);
    }

    [Fact]
    public async Task Desactivar_la_cuenta_invalida_la_sesion_abierta()
    {
        var usuario = await api.CrearUsuarioAsync(Rol.Docente);
        var cliente = api.CreateClient();
        (await Login(cliente, usuario.Email, ApiDePrueba.PasswordDePrueba)).EnsureSuccessStatusCode();

        await api.EnBaseAsync(db => db.Usuarios.Where(u => u.Id == usuario.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Activo, false)));

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/auth/yo")).StatusCode);
    }

    [Fact]
    public async Task Cambiar_el_rol_invalida_la_sesion_abierta()
    {
        var usuario = await api.CrearUsuarioAsync(Rol.Admin);
        var cliente = api.CreateClient();
        (await Login(cliente, usuario.Email, ApiDePrueba.PasswordDePrueba)).EnsureSuccessStatusCode();

        await api.EnBaseAsync(db => db.Usuarios.Where(u => u.Id == usuario.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Rol, Rol.Docente)));

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/auth/yo")).StatusCode);
    }

    [Fact]
    public async Task La_password_se_guarda_con_hash()
    {
        var usuario = await api.CrearUsuarioAsync(Rol.Admin);

        Assert.DoesNotContain(ApiDePrueba.PasswordDePrueba, usuario.PasswordHash);
        Assert.True(usuario.PasswordHash.Length > 40);
    }
}

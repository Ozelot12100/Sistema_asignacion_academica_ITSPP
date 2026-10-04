using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Asistencias.Api.Autenticacion;
using Asistencias.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Tests;

public class UsuariosTests(ApiDePrueba api) : IClassFixture<ApiDePrueba>
{
    private const string PasswordNueva = "otra-contraseña-larga";

    private static object Datos(string? email = null, string rol = "docente", string? password = PasswordNueva, bool? activo = null) => new
    {
        rol,
        nombre = "Nuevo",
        apellido = "Usuario",
        email = email ?? $"{Guid.NewGuid():N}@prueba.mx",
        departamento = "Sistemas",
        password,
        activo,
    };

    private Task<HttpResponseMessage> Login(string email, string password) =>
        api.CreateClient().PostAsJsonAsync("/api/auth/login", new CredencialesDto(email, password));

    [Theory]
    [InlineData(Rol.Docente)]
    [InlineData(Rol.Laboratorista)]
    public async Task Solo_el_administrador_gestiona_usuarios(Rol rol)
    {
        var cliente = await api.ClienteConSesionAsync(rol);

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/usuarios", Datos())).StatusCode);
    }

    [Fact]
    public async Task Crear_usuario_permite_que_inicie_sesion_y_no_devuelve_la_password()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var email = $"{Guid.NewGuid():N}@prueba.mx";

        var respuesta = await admin.PostAsJsonAsync("/api/usuarios", Datos(email, "laboratorista"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var texto = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", texto, StringComparison.OrdinalIgnoreCase);
        var cuerpo = JsonNode.Parse(texto)!;
        Assert.Equal("laboratorista", (string)cuerpo["rol"]!);
        Assert.True((bool)cuerpo["activo"]!);
        Assert.EndsWith("Z", (string)cuerpo["creadoEn"]!);

        Assert.Equal(HttpStatusCode.OK, (await Login(email, PasswordNueva)).StatusCode);

        var lista = await admin.GetFromJsonAsync<JsonArray>("/api/usuarios?rol=laboratorista");
        Assert.Contains(lista!, u => (string)u!["email"]! == email);
        Assert.All(lista!, u => Assert.Equal("laboratorista", (string)u!["rol"]!));
    }

    [Fact]
    public async Task Correo_repetido_responde_409()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var existente = await api.CrearUsuarioAsync(Rol.Docente);

        var respuesta = await admin.PostAsJsonAsync("/api/usuarios", Datos(existente.Email));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task Datos_invalidos_responden_400_con_cada_campo()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);

        var respuesta = await admin.PostAsJsonAsync("/api/usuarios",
            new { nombre = " ", apellido = "X", email = "no-es-correo", password = "corta" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var errores = (await respuesta.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject();
        Assert.Equal(["email", "nombre", "password", "rol"], errores.Select(e => e.Key).Order());
    }

    [Fact]
    public async Task Rol_desconocido_responde_400()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);

        var respuesta = await admin.PostAsJsonAsync("/api/usuarios", Datos(rol: "director"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Desactivar_un_usuario_le_impide_iniciar_sesion()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var usuario = await api.CrearUsuarioAsync(Rol.Docente);

        var respuesta = await admin.PutAsJsonAsync($"/api/usuarios/{usuario.Id}", Datos(usuario.Email, password: null, activo: false));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(usuario.Email, ApiDePrueba.PasswordDePrueba)).StatusCode);
    }

    [Fact]
    public async Task Editar_no_cambia_la_password()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var usuario = await api.CrearUsuarioAsync(Rol.Docente);

        await admin.PutAsJsonAsync($"/api/usuarios/{usuario.Id}", Datos(usuario.Email, password: "se-ignora-al-editar"));

        Assert.Equal(HttpStatusCode.OK, (await Login(usuario.Email, ApiDePrueba.PasswordDePrueba)).StatusCode);
    }

    [Fact]
    public async Task Editar_usuario_inexistente_responde_404()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);

        var respuesta = await admin.PutAsJsonAsync("/api/usuarios/999999", Datos());

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_unico_administrador_activo_no_puede_desactivarse_ni_cambiar_de_rol()
    {
        var (admin, yo) = await api.SesionAsync(Rol.Admin);
        await api.EnBaseAsync(db => db.Usuarios.Where(u => u.Rol == Rol.Admin && u.Id != yo.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Activo, false)));

        var desactivar = await admin.PutAsJsonAsync($"/api/usuarios/{yo.Id}", Datos(yo.Email, "admin", null, activo: false));
        var degradar = await admin.PutAsJsonAsync($"/api/usuarios/{yo.Id}", Datos(yo.Email, "docente", null));

        Assert.Equal(HttpStatusCode.Conflict, desactivar.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, degradar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/auth/yo")).StatusCode);
    }

    [Fact]
    public async Task Habiendo_otro_administrador_si_se_puede_cambiar_de_rol()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var otro = await api.CrearUsuarioAsync(Rol.Admin);

        var respuesta = await admin.PutAsJsonAsync($"/api/usuarios/{otro.Id}", Datos(otro.Email, "docente", null));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_administrador_restablece_la_password_de_otro()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var usuario = await api.CrearUsuarioAsync(Rol.Docente);

        var corta = await admin.PutAsJsonAsync($"/api/usuarios/{usuario.Id}/password", new { password = "corta" });
        var buena = await admin.PutAsJsonAsync($"/api/usuarios/{usuario.Id}/password", new { password = PasswordNueva });

        Assert.Equal(HttpStatusCode.BadRequest, corta.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, buena.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(usuario.Email, ApiDePrueba.PasswordDePrueba)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(usuario.Email, PasswordNueva)).StatusCode);
    }

    [Fact]
    public async Task Cada_quien_cambia_su_password_si_conoce_la_actual()
    {
        var (cliente, yo) = await api.SesionAsync(Rol.Docente);

        var sinLaActual = await cliente.PutAsJsonAsync("/api/auth/password", new { actual = "no-es-la-actual", nueva = PasswordNueva });
        var correcta = await cliente.PutAsJsonAsync("/api/auth/password", new { actual = ApiDePrueba.PasswordDePrueba, nueva = PasswordNueva });

        Assert.Equal(HttpStatusCode.BadRequest, sinLaActual.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, correcta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(yo.Email, PasswordNueva)).StatusCode);
    }

    [Fact]
    public async Task Los_cambios_quedan_en_la_bitacora_con_su_autor_y_sin_valores()
    {
        var (admin, yo) = await api.SesionAsync(Rol.Admin);
        var creado = await (await admin.PostAsJsonAsync("/api/usuarios", Datos())).Content.ReadFromJsonAsync<JsonObject>();
        var id = (int)creado!["id"]!;
        await admin.PutAsJsonAsync($"/api/usuarios/{id}/password", new { password = PasswordNueva });

        var entradas = await api.EnBaseAsync(db => db.Bitacora
            .Where(b => b.Entidad == "usuarios" && b.EntidadId == id.ToString())
            .OrderBy(b => b.Id).ToListAsync());

        Assert.Equal(["crear", "modificar"], entradas.Select(e => e.Accion));
        Assert.All(entradas, e => Assert.Equal(yo.Id, e.UsuarioId));
        Assert.Null(entradas[0].Detalles);
        Assert.Equal("campos: password_hash", entradas[1].Detalles);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Asistencias.Api.Datos;

namespace Asistencias.Tests;

public class BitacoraTests(ApiDePrueba api) : IClassFixture<ApiDePrueba>
{
    [Fact]
    public async Task El_administrador_consulta_la_bitacora_filtrada_y_paginada()
    {
        var (admin, usuario) = await api.SesionAsync(Rol.Admin);
        var clave = $"BIT-{Guid.NewGuid():N}"[..18];
        var creada = await admin.PostAsJsonAsync("/api/carreras", new { clave, nombre = $"Carrera {clave}" });
        var id = (int)(await creada.Content.ReadFromJsonAsync<JsonObject>())!["id"]!;
        await admin.PutAsJsonAsync($"/api/carreras/{id}", new { clave, nombre = $"Editada {clave}" });
        await admin.DeleteAsync($"/api/carreras/{id}");

        var deLaCarrera = await admin.GetFromJsonAsync<JsonObject>($"/api/bitacora?entidad=carreras&entidadId={id}");
        var soloCambios = await admin.GetFromJsonAsync<JsonObject>($"/api/bitacora?entidad=carreras&entidadId={id}&accion=modificar");
        var pagina2 = await admin.GetFromJsonAsync<JsonObject>($"/api/bitacora?usuarioId={usuario.Id}&tamano=2&pagina=2");
        var futuro = await admin.GetFromJsonAsync<JsonObject>("/api/bitacora?desde=2999-01-01");

        Assert.Equal(3, (int)deLaCarrera!["total"]!);
        // Lo más reciente primero
        Assert.Equal(["eliminar", "modificar", "crear"], deLaCarrera["elementos"]!.AsArray().Select(b => (string)b!["accion"]!));
        Assert.All(deLaCarrera["elementos"]!.AsArray(), b => Assert.Equal("Usuario De Prueba", (string)b!["usuario"]!));
        var cambio = Assert.Single(soloCambios!["elementos"]!.AsArray());
        Assert.Equal("campos: nombre", (string)cambio!["detalles"]!);
        Assert.Equal(3, (int)pagina2!["total"]!);
        Assert.Single(pagina2["elementos"]!.AsArray());
        Assert.Equal(0, (int)futuro!["total"]!);
    }

    [Theory]
    [InlineData(Rol.Docente)]
    [InlineData(Rol.Laboratorista)]
    public async Task Solo_el_administrador_consulta_la_bitacora(Rol rol)
    {
        var cliente = await api.ClienteConSesionAsync(rol);

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/bitacora")).StatusCode);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Asistencias.Api.Datos;

namespace Asistencias.Tests;

public class AlumnosTests(ApiDePrueba api) : IClassFixture<ApiDePrueba>
{
    private static string Matricula() => Guid.NewGuid().ToString("N")[..12];

    [Fact]
    public async Task El_laboratorista_da_de_alta_y_edita_alumnos()
    {
        var cliente = await api.ClienteConSesionAsync(Rol.Laboratorista);
        var matricula = Matricula();

        var creado = await cliente.PostAsJsonAsync("/api/alumnos",
            new { matricula, nombre = " Luis ", apellido = "Mora", semestre = 5, turno = "vespertino", grupo = "5A" });

        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var cuerpo = await creado.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("Luis", (string)cuerpo!["nombre"]!);
        Assert.Equal("vespertino", (string)cuerpo["turno"]!);
        Assert.Null(cuerpo["carrera"]);

        var id = (int)cuerpo["id"]!;
        var editado = await cliente.PutAsJsonAsync($"/api/alumnos/{id}",
            new { matricula, nombre = "Luis", apellido = "Mora", semestre = 6, activo = false });
        var leido = await cliente.GetFromJsonAsync<JsonObject>($"/api/alumnos/{id}");

        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);
        Assert.Equal(6, (int)leido!["semestre"]!);
        Assert.False((bool)leido["activo"]!);
        Assert.Null(leido["turno"]);
    }

    [Fact]
    public async Task Matricula_repetida_responde_409()
    {
        var cliente = await api.ClienteConSesionAsync(Rol.Laboratorista);
        var alumno = new { matricula = Matricula(), nombre = "Eva", apellido = "Luna" };
        await cliente.PostAsJsonAsync("/api/alumnos", alumno);

        var respuesta = await cliente.PostAsJsonAsync("/api/alumnos", alumno);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task Datos_invalidos_responden_400()
    {
        var cliente = await api.ClienteConSesionAsync(Rol.Laboratorista);

        var vacio = await cliente.PostAsJsonAsync("/api/alumnos", new { });
        var semestre = await cliente.PostAsJsonAsync("/api/alumnos", new { matricula = Matricula(), nombre = "A", apellido = "B", semestre = 0 });
        var turno = await cliente.PostAsJsonAsync("/api/alumnos", new { matricula = Matricula(), nombre = "A", apellido = "B", turno = "nocturno" });
        var carrera = await cliente.PostAsJsonAsync("/api/alumnos", new { matricula = Matricula(), nombre = "A", apellido = "B", carreraId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, vacio.StatusCode);
        var errores = (await vacio.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject();
        Assert.Equal(["apellido", "matricula", "nombre"], errores.Select(e => e.Key).Order());
        Assert.Equal(HttpStatusCode.BadRequest, semestre.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, turno.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, carrera.StatusCode);
    }

    [Fact]
    public async Task El_docente_consulta_pero_no_modifica_y_solo_el_administrador_borra()
    {
        var laboratorista = await api.ClienteConSesionAsync(Rol.Laboratorista);
        var docente = await api.ClienteConSesionAsync(Rol.Docente);
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var alumno = new { matricula = Matricula(), nombre = "Raúl", apellido = "Paz" };
        var id = (int)(await (await laboratorista.PostAsJsonAsync("/api/alumnos", alumno)).Content.ReadFromJsonAsync<JsonObject>())!["id"]!;

        Assert.Equal(HttpStatusCode.OK, (await docente.GetAsync($"/api/alumnos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await docente.PostAsJsonAsync("/api/alumnos", alumno)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await docente.PutAsJsonAsync($"/api/alumnos/{id}", alumno)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await laboratorista.DeleteAsync($"/api/alumnos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/alumnos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await docente.GetAsync($"/api/alumnos/{id}")).StatusCode);
    }

    [Fact]
    public async Task La_carga_en_lote_crea_a_los_nuevos_y_actualiza_a_los_existentes()
    {
        var cliente = await api.ClienteConSesionAsync(Rol.Laboratorista);
        var docente = await api.ClienteConSesionAsync(Rol.Docente);
        var (existente, nuevo) = (Matricula(), Matricula());
        await cliente.PostAsJsonAsync("/api/alumnos", new { matricula = existente, nombre = "Eva", apellido = "Luna", semestre = 1 });
        object[] lote =
        [
            new { matricula = existente, nombre = "Eva", apellido = "Luna", semestre = 2 },
            new { matricula = nuevo, nombre = "Iván", apellido = "Soto", semestre = 1 },
        ];

        var respuesta = await cliente.PostAsJsonAsync("/api/alumnos/lote", lote);

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(1, (int)cuerpo!["creados"]!);
        Assert.Equal(1, (int)cuerpo["actualizados"]!);
        var actualizado = await cliente.GetFromJsonAsync<JsonObject>($"/api/alumnos?buscar={existente}");
        Assert.Equal(2, (int)actualizado!["elementos"]![0]!["semestre"]!);
        Assert.Equal(HttpStatusCode.Forbidden, (await docente.PostAsJsonAsync("/api/alumnos/lote", lote)).StatusCode);
    }

    [Fact]
    public async Task La_carga_en_lote_no_guarda_nada_si_un_renglon_tiene_error()
    {
        var cliente = await api.ClienteConSesionAsync(Rol.Laboratorista);
        var (bueno, repetido) = (Matricula(), Matricula());
        object[] lote =
        [
            new { matricula = bueno, nombre = "Ana", apellido = "Ruiz" },
            new { matricula = repetido, nombre = "Luis", apellido = "Mora" },
            new { matricula = repetido, nombre = "Luis", apellido = "Mora" },
            new { matricula = Matricula(), nombre = "", apellido = "Paz", semestre = 0 },
        ];

        var respuesta = await cliente.PostAsJsonAsync("/api/alumnos/lote", lote);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var errores = (await respuesta.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject();
        Assert.Equal(["[2].matricula", "[3].nombre", "[3].semestre"], errores.Select(e => e.Key).Order());
        Assert.Equal(0, (int)(await cliente.GetFromJsonAsync<JsonObject>($"/api/alumnos?buscar={bueno}"))!["total"]!);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/alumnos/lote", Array.Empty<object>())).StatusCode);
    }

    [Fact]
    public async Task Busca_por_matricula_o_nombre_y_pagina_los_resultados()
    {
        var cliente = await api.ClienteConSesionAsync(Rol.Laboratorista);
        var marca = Guid.NewGuid().ToString("N")[..8];
        for (var i = 1; i <= 5; i++)
            (await cliente.PostAsJsonAsync("/api/alumnos",
                new { matricula = $"{marca}{i}", nombre = $"Nombre{i}", apellido = $"Apellido{marca}" })).EnsureSuccessStatusCode();

        var porMatricula = await cliente.GetFromJsonAsync<JsonObject>($"/api/alumnos?buscar={marca}3");
        var porNombre = await cliente.GetFromJsonAsync<JsonObject>($"/api/alumnos?buscar=Nombre2 Apellido{marca}");
        var pagina2 = await cliente.GetFromJsonAsync<JsonObject>($"/api/alumnos?buscar=Apellido{marca}&tamano=2&pagina=2");
        var tamanoExcesivo = await cliente.GetFromJsonAsync<JsonObject>($"/api/alumnos?buscar={marca}&tamano=5000");

        Assert.Equal(1, (int)porMatricula!["total"]!);
        Assert.Equal($"{marca}3", (string)porMatricula["elementos"]![0]!["matricula"]!);
        Assert.Equal(1, (int)porNombre!["total"]!);
        Assert.Equal(5, (int)pagina2!["total"]!);
        Assert.Equal(["Nombre3", "Nombre4"], pagina2["elementos"]!.AsArray().Select(a => (string)a!["nombre"]!));
        Assert.Equal(100, (int)tamanoExcesivo!["tamano"]!);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Asistencias.Api.Datos;

namespace Asistencias.Tests;

public class CatalogosTests(ApiDePrueba api) : IClassFixture<ApiDePrueba>
{
    private static string Unico(string prefijo) => $"{prefijo}-{Guid.NewGuid():N}"[..18];

    private static async Task<int> IdCreado(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (int)(await respuesta.Content.ReadFromJsonAsync<JsonObject>())!["id"]!;
    }

    [Fact]
    public async Task Carrera_ciclo_completo()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var clave = Unico("ISC");

        var id = await IdCreado(await admin.PostAsJsonAsync("/api/carreras", new { clave, nombre = $"Carrera {clave}" }));
        var repetida = await admin.PostAsJsonAsync("/api/carreras", new { clave, nombre = "Otra" });
        var editada = await admin.PutAsJsonAsync($"/api/carreras/{id}", new { clave, nombre = $"Editada {clave}", activa = false });

        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.DoesNotContain(await admin.GetFromJsonAsync<JsonArray>("/api/carreras") ?? [], c => (int)c!["id"]! == id);
        Assert.Contains(await admin.GetFromJsonAsync<JsonArray>("/api/carreras?incluirInactivas=true") ?? [], c => (int)c!["id"]! == id);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/carreras/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/carreras/{id}")).StatusCode);
    }

    [Fact]
    public async Task Carrera_con_alumnos_no_se_puede_borrar()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var clave = Unico("IND");
        var id = await IdCreado(await admin.PostAsJsonAsync("/api/carreras", new { clave, nombre = $"Carrera {clave}" }));
        await IdCreado(await admin.PostAsJsonAsync("/api/alumnos",
            new { matricula = Unico("M"), nombre = "Ana", apellido = "Ruiz", carreraId = id }));

        var respuesta = await admin.DeleteAsync($"/api/carreras/{id}");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(Rol.Docente)]
    [InlineData(Rol.Laboratorista)]
    public async Task Los_catalogos_se_consultan_con_cualquier_rol_pero_solo_el_administrador_los_modifica(Rol rol)
    {
        var cliente = await api.ClienteConSesionAsync(rol);

        foreach (var ruta in (string[])["/api/carreras", "/api/periodos", "/api/materias", "/api/laboratorios"])
        {
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(ruta)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync(ruta, new { })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.DeleteAsync($"{ruta}/1")).StatusCode);
        }
    }

    [Fact]
    public async Task Periodo_exige_que_la_fecha_de_fin_sea_posterior()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var nombre = Unico("Ago-Dic");

        var invertido = await admin.PostAsJsonAsync("/api/periodos", new { nombre, fechaInicio = "2026-12-11", fechaFin = "2026-08-17" });
        var sinFechas = await admin.PostAsJsonAsync("/api/periodos", new { nombre });
        var bueno = await admin.PostAsJsonAsync("/api/periodos", new { nombre, fechaInicio = "2026-08-17", fechaFin = "2026-12-11" });

        Assert.Equal(HttpStatusCode.BadRequest, invertido.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, sinFechas.StatusCode);
        var cuerpo = await bueno.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(HttpStatusCode.Created, bueno.StatusCode);
        Assert.Equal("2026-08-17", (string)cuerpo!["fechaInicio"]!);
        Assert.Equal("2026-12-11", (string)cuerpo["fechaFin"]!);
    }

    [Fact]
    public async Task Materia_muestra_su_carrera_y_rechaza_una_carrera_inexistente()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var clave = Unico("ISC");
        var idCarrera = await IdCreado(await admin.PostAsJsonAsync("/api/carreras", new { clave, nombre = $"Carrera {clave}" }));

        var creada = await admin.PostAsJsonAsync("/api/materias",
            new { clave = Unico("PROG"), nombre = "Programación", carreraId = idCarrera, semestre = 3 });
        var sinCarrera = await admin.PostAsJsonAsync("/api/materias", new { clave = Unico("X"), nombre = "X", carreraId = 999999 });
        var semestreMalo = await admin.PostAsJsonAsync("/api/materias", new { clave = Unico("Y"), nombre = "Y", semestre = 0 });

        var cuerpo = await creada.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        Assert.Equal($"Carrera {clave}", (string)cuerpo!["carrera"]!);
        Assert.Equal(HttpStatusCode.BadRequest, sinCarrera.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, semestreMalo.StatusCode);

        var deLaCarrera = await admin.GetFromJsonAsync<JsonArray>($"/api/materias?carreraId={idCarrera}");
        Assert.Single(deLaCarrera!);
    }

    [Fact]
    public async Task Laboratorio_y_equipos_ciclo_completo()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var clave = Unico("redes");

        var idLab = await IdCreado(await admin.PostAsJsonAsync("/api/laboratorios", new { clave, nombre = "Redes", ubicacion = "Edificio B" }));
        var idEquipo = await IdCreado(await admin.PostAsJsonAsync($"/api/laboratorios/{idLab}/equipos", new { etiqueta = "PC-1" }));
        var repetido = await admin.PostAsJsonAsync($"/api/laboratorios/{idLab}/equipos", new { etiqueta = "PC-1" });
        var enLabInexistente = await admin.PostAsJsonAsync("/api/laboratorios/999999/equipos", new { etiqueta = "PC-1" });

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, enLabInexistente.StatusCode);

        // Con equipos, el laboratorio no se puede borrar
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/laboratorios/{idLab}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/equipos/{idEquipo}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/laboratorios/{idLab}")).StatusCode);
    }

    [Fact]
    public async Task La_misma_etiqueta_puede_existir_en_dos_laboratorios()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var a = await IdCreado(await admin.PostAsJsonAsync("/api/laboratorios", new { clave = Unico("a"), nombre = "A" }));
        var b = await IdCreado(await admin.PostAsJsonAsync("/api/laboratorios", new { clave = Unico("b"), nombre = "B" }));

        await IdCreado(await admin.PostAsJsonAsync($"/api/laboratorios/{a}/equipos", new { etiqueta = "PC-1" }));
        await IdCreado(await admin.PostAsJsonAsync($"/api/laboratorios/{b}/equipos", new { etiqueta = "PC-1" }));
    }

    [Fact]
    public async Task El_laboratorista_marca_un_equipo_fuera_de_servicio_y_el_docente_no()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var idLab = await IdCreado(await admin.PostAsJsonAsync("/api/laboratorios", new { clave = Unico("serv"), nombre = "Servicio" }));
        var idEquipo = await IdCreado(await admin.PostAsJsonAsync($"/api/laboratorios/{idLab}/equipos", new { etiqueta = "PC-1" }));
        var laboratorista = await api.ClienteConSesionAsync(Rol.Laboratorista);
        var docente = await api.ClienteConSesionAsync(Rol.Docente);
        var cambio = new { fueraDeServicio = true, notas = "No enciende" };

        var delDocente = await docente.PutAsJsonAsync($"/api/equipos/{idEquipo}/servicio", cambio);
        var delLaboratorista = await laboratorista.PutAsJsonAsync($"/api/equipos/{idEquipo}/servicio", cambio);
        var renombrar = await laboratorista.PutAsJsonAsync($"/api/equipos/{idEquipo}", new { etiqueta = "PC-99" });

        Assert.Equal(HttpStatusCode.Forbidden, delDocente.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, renombrar.StatusCode);
        var cuerpo = await delLaboratorista.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(HttpStatusCode.OK, delLaboratorista.StatusCode);
        Assert.Equal("fuera_de_servicio", (string)cuerpo!["estado"]!);
        Assert.Equal("No enciende", (string)cuerpo["notas"]!);
    }

    [Fact]
    public async Task Un_equipo_con_asistencias_no_se_puede_borrar()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var idLab = await IdCreado(await admin.PostAsJsonAsync("/api/laboratorios", new { clave = Unico("hist"), nombre = "Historial" }));
        var idEquipo = await IdCreado(await admin.PostAsJsonAsync($"/api/laboratorios/{idLab}/equipos", new { etiqueta = "PC-1" }));
        await api.EnBaseAsync(async db =>
        {
            db.Asistencias.Add(new Asistencia
            {
                LaboratorioId = idLab, EquipoId = idEquipo, TipoUsuario = TipoUsuario.Invitado, InvitadoNombre = "Visita",
                HoraEntrada = DateTime.UtcNow.AddHours(-2), HoraSalida = DateTime.UtcNow.AddHours(-1),
            });
            await db.SaveChangesAsync();
        });

        var respuesta = await admin.DeleteAsync($"/api/equipos/{idEquipo}");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }
}

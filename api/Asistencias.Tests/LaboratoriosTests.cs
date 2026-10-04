using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Asistencias.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Asistencias.Tests;

public class LaboratoriosTests(ApiDePrueba api) : IClassFixture<ApiDePrueba>
{
    private async Task<int> IdDeProgramacionAsync()
    {
        var id = 0;
        await api.EnBaseAsync(async db => id = (await db.Laboratorios.SingleAsync(l => l.Clave == "programacion")).Id);
        return id;
    }

    [Fact]
    public async Task Lista_los_laboratorios_con_el_conteo_de_sus_equipos()
    {
        var cliente = await api.ClienteConSesionAsync();

        var laboratorios = await cliente.GetFromJsonAsync<JsonArray>("/api/laboratorios");

        var programacion = Assert.Single(laboratorios!, l => (string)l!["clave"]! == "programacion");
        Assert.Equal(25, (int)programacion!["totalEquipos"]!);
    }

    [Fact]
    public async Task No_lista_los_laboratorios_desactivados()
    {
        await api.EnBaseAsync(async db =>
        {
            db.Laboratorios.Add(new Laboratorio { Clave = "cerrado", Nombre = "Cerrado", Activo = false });
            await db.SaveChangesAsync();
        });
        var cliente = await api.ClienteConSesionAsync();

        var laboratorios = await cliente.GetFromJsonAsync<JsonArray>("/api/laboratorios");

        Assert.DoesNotContain(laboratorios!, l => (string)l!["clave"]! == "cerrado");
    }

    [Fact]
    public async Task Equipos_de_un_laboratorio_inexistente_responde_404()
    {
        var cliente = await api.ClienteConSesionAsync();

        var respuesta = await cliente.GetAsync("/api/laboratorios/999999/equipos");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Equipos_salen_en_orden_natural_y_con_su_estado()
    {
        // Laboratorio propio para no depender de lo que hagan otras pruebas
        var idLab = 0;
        await api.EnBaseAsync(async db =>
        {
            var lab = new Laboratorio { Clave = "estados", Nombre = "Estados" };
            lab.Equipos.AddRange(
            [
                new Equipo { Etiqueta = "PC-10" },
                new Equipo { Etiqueta = "PC-2", FueraDeServicio = true, Notas = "Sin monitor" },
                new Equipo { Etiqueta = "PC-1" },
                new Equipo { Etiqueta = "PC-3" },
            ]);
            db.Laboratorios.Add(lab);
            await db.SaveChangesAsync();
            idLab = lab.Id;

            int Id(string etiqueta) => lab.Equipos.Single(e => e.Etiqueta == etiqueta).Id;
            db.Asistencias.AddRange(
                // Abierta: PC-1 está en uso
                new Asistencia { LaboratorioId = idLab, EquipoId = Id("PC-1"), TipoUsuario = TipoUsuario.Invitado, InvitadoNombre = "Visita" },
                // Cerrada: PC-3 ya quedó libre
                new Asistencia
                {
                    LaboratorioId = idLab, EquipoId = Id("PC-3"), TipoUsuario = TipoUsuario.Invitado, InvitadoNombre = "Visita",
                    HoraEntrada = DateTime.UtcNow.AddHours(-2), HoraSalida = DateTime.UtcNow.AddHours(-1),
                },
                // Con equipo propio: no ocupa ningún equipo
                new Asistencia { LaboratorioId = idLab, TipoUsuario = TipoUsuario.Invitado, InvitadoNombre = "Con laptop" });
            await db.SaveChangesAsync();
        });
        var cliente = await api.ClienteConSesionAsync();

        var equipos = await cliente.GetFromJsonAsync<JsonArray>($"/api/laboratorios/{idLab}/equipos");

        Assert.Equal(["PC-1", "PC-2", "PC-3", "PC-10"], equipos!.Select(e => (string)e!["etiqueta"]!));
        Assert.Equal(["en_uso", "fuera_de_servicio", "libre", "libre"], equipos!.Select(e => (string)e!["estado"]!));
        Assert.Equal("Sin monitor", (string)equipos![1]!["notas"]!);

        var laboratorios = await cliente.GetFromJsonAsync<JsonArray>("/api/laboratorios");
        var lab = Assert.Single(laboratorios!, l => (int)l!["id"]! == idLab);
        Assert.Equal(4, (int)lab!["totalEquipos"]!);
        Assert.Equal(1, (int)lab["equiposEnUso"]!);
        Assert.Equal(1, (int)lab["equiposFueraDeServicio"]!);
    }

    [Fact]
    public async Task Los_25_equipos_de_programacion_empiezan_libres()
    {
        var cliente = await api.ClienteConSesionAsync(Rol.Docente);

        var equipos = await cliente.GetFromJsonAsync<JsonArray>($"/api/laboratorios/{await IdDeProgramacionAsync()}/equipos");

        Assert.Equal(25, equipos!.Count);
        Assert.Equal("PC-1", (string)equipos[0]!["etiqueta"]!);
        Assert.Equal("PC-25", (string)equipos[24]!["etiqueta"]!);
        Assert.All(equipos, e => Assert.Equal("libre", (string)e!["estado"]!));
    }
}

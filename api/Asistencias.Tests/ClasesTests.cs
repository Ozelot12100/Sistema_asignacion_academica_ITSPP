using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Asistencias.Api.Datos;

namespace Asistencias.Tests;

public class ClasesTests(ApiDePrueba api) : IClassFixture<ApiDePrueba>
{
    private static string Unico(string prefijo) => $"{prefijo}-{Guid.NewGuid():N}"[..18];

    private static async Task<int> IdCreado(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (int)(await respuesta.Content.ReadFromJsonAsync<JsonObject>())!["id"]!;
    }

    private sealed record Escenario(
        HttpClient Admin, HttpClient Docente, int CursoId, int MateriaId, int LaboratorioId, int EquipoId);

    // Un curso con docente y laboratorio propios, y un equipo PC-1 en ese laboratorio
    private async Task<Escenario> CrearEscenario()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var (docente, usuario) = await api.SesionAsync(Rol.Docente);
        var materiaId = await IdCreado(await admin.PostAsJsonAsync("/api/materias", new { clave = Unico("MAT"), nombre = "Redes" }));
        var periodoId = await IdCreado(await admin.PostAsJsonAsync("/api/periodos",
            new { nombre = Unico("Ago-Dic"), fechaInicio = "2026-08-17", fechaFin = "2026-12-11" }));
        var laboratorioId = await IdCreado(await admin.PostAsJsonAsync("/api/laboratorios", new { clave = Unico("lab"), nombre = "Lab de Redes" }));
        var equipoId = await IdCreado(await admin.PostAsJsonAsync($"/api/laboratorios/{laboratorioId}/equipos", new { etiqueta = "PC-1" }));
        var cursoId = await IdCreado(await admin.PostAsJsonAsync("/api/cursos",
            new { materiaId, periodoId, grupo = "5A", docenteId = usuario.Id, laboratorioId }));
        return new Escenario(admin, docente, cursoId, materiaId, laboratorioId, equipoId);
    }

    private static async Task<string> InscribirAlumno(Escenario e, bool inscrito = true, bool activo = true)
    {
        var matricula = Guid.NewGuid().ToString("N")[..8];
        var id = await IdCreado(await e.Admin.PostAsJsonAsync("/api/alumnos", new { matricula, nombre = "Ana", apellido = "Ruiz" }));
        if (inscrito)
            (await e.Admin.PutAsync($"/api/cursos/{e.CursoId}/alumnos/{id}", null)).EnsureSuccessStatusCode();
        if (!activo)
            (await e.Admin.PutAsJsonAsync($"/api/alumnos/{id}", new { matricula, nombre = "Ana", apellido = "Ruiz", activo = false })).EnsureSuccessStatusCode();
        return matricula;
    }

    private Task<HttpResponseMessage> RegistrarEntrada(string matricula, string? equipo = null) =>
        api.CreateClient().PostAsJsonAsync("/api/registro/entrada", new { matricula, equipo });

    [Fact]
    public async Task El_docente_abre_la_clase_los_alumnos_entran_y_al_cerrarla_salen_todos()
    {
        var e = await CrearEscenario();
        var matricula = await InscribirAlumno(e);

        var antesDeAbrir = await RegistrarEntrada(matricula);
        var abierta = await e.Docente.PostAsJsonAsync("/api/clases", new { cursoId = e.CursoId });
        var claseId = await IdCreado(abierta);
        var entrada = await RegistrarEntrada(matricula);
        var repetida = await RegistrarEntrada(matricula);

        Assert.Equal(HttpStatusCode.Conflict, antesDeAbrir.StatusCode);
        Assert.Equal(HttpStatusCode.OK, entrada.StatusCode);
        var cuerpo = await entrada.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("Ana Ruiz", (string)cuerpo!["alumno"]!);
        Assert.Equal("Redes", (string)cuerpo["materia"]!);
        Assert.Equal("Lab de Redes", (string)cuerpo["laboratorio"]!);
        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);

        var dentro = Assert.Single((await e.Docente.GetFromJsonAsync<JsonArray>($"/api/clases/{claseId}/asistencias"))!);
        Assert.Equal(matricula, (string)dentro!["matricula"]!);
        Assert.Null(dentro["horaSalida"]);

        var cerrada = await e.Docente.PostAsync($"/api/clases/{claseId}/cerrar", null);
        var cuerpoCerrada = await cerrada.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(HttpStatusCode.OK, cerrada.StatusCode);
        Assert.NotNull(cuerpoCerrada!["cerradaEn"]);
        Assert.Equal(1, (int)cuerpoCerrada["registrados"]!);

        var fuera = Assert.Single((await e.Docente.GetFromJsonAsync<JsonArray>($"/api/clases/{claseId}/asistencias"))!);
        Assert.NotNull(fuera!["horaSalida"]);
        Assert.Equal(HttpStatusCode.Conflict, (await e.Docente.PostAsync($"/api/clases/{claseId}/cerrar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await RegistrarEntrada(matricula)).StatusCode);
    }

    [Fact]
    public async Task Solo_registra_su_entrada_un_alumno_activo_e_inscrito_en_el_curso()
    {
        var e = await CrearEscenario();
        await IdCreado(await e.Docente.PostAsJsonAsync("/api/clases", new { cursoId = e.CursoId }));
        var noInscrito = await InscribirAlumno(e, inscrito: false);
        var desactivado = await InscribirAlumno(e, activo: false);

        Assert.Equal(HttpStatusCode.Conflict, (await RegistrarEntrada(noInscrito)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await RegistrarEntrada(desactivado)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await RegistrarEntrada("no-existe")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await RegistrarEntrada(" ")).StatusCode);
    }

    [Fact]
    public async Task El_alumno_ocupa_un_equipo_del_laboratorio_y_lo_libera_al_cerrar_la_clase()
    {
        var e = await CrearEscenario();
        var fueraDeServicio = await IdCreado(await e.Admin.PostAsJsonAsync($"/api/laboratorios/{e.LaboratorioId}/equipos", new { etiqueta = "PC-2" }));
        await e.Admin.PutAsJsonAsync($"/api/equipos/{fueraDeServicio}/servicio", new { fueraDeServicio = true });
        var claseId = await IdCreado(await e.Docente.PostAsJsonAsync("/api/clases", new { cursoId = e.CursoId }));
        var primero = await InscribirAlumno(e);
        var segundo = await InscribirAlumno(e);

        var entrada = await RegistrarEntrada(primero, "pc-1");
        var mismoEquipo = await RegistrarEntrada(segundo, "PC-1");
        var descompuesto = await RegistrarEntrada(segundo, "PC-2");
        var inexistente = await RegistrarEntrada(segundo, "PC-99");
        var sinEquipo = await RegistrarEntrada(segundo);

        Assert.Equal(HttpStatusCode.OK, entrada.StatusCode);
        Assert.Equal("PC-1", (string)(await entrada.Content.ReadFromJsonAsync<JsonObject>())!["equipo"]!);
        Assert.Equal(HttpStatusCode.Conflict, mismoEquipo.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, descompuesto.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sinEquipo.StatusCode);
        Assert.Equal("en_uso", await EstadoDelEquipo());

        await e.Docente.PostAsync($"/api/clases/{claseId}/cerrar", null);

        Assert.Equal("libre", await EstadoDelEquipo());

        async Task<string> EstadoDelEquipo() =>
            (string)(await e.Admin.GetFromJsonAsync<JsonArray>($"/api/laboratorios/{e.LaboratorioId}/equipos"))!
                .Single(x => (int)x!["id"]! == e.EquipoId)!["estado"]!;
    }

    [Fact]
    public async Task La_lista_de_la_clase_dice_quien_asistio_y_quien_falto()
    {
        var e = await CrearEscenario();
        var presente = await InscribirAlumno(e);
        var ausente = await InscribirAlumno(e);
        var claseId = await IdCreado(await e.Docente.PostAsJsonAsync("/api/clases", new { cursoId = e.CursoId }));
        await RegistrarEntrada(presente, "PC-1");

        var lista = await e.Docente.GetFromJsonAsync<JsonObject>($"/api/clases/{claseId}/lista");

        Assert.Equal(2, (int)lista!["inscritos"]!);
        Assert.Equal(1, (int)lista["presentes"]!);
        Assert.Equal(1, (int)lista["faltas"]!);
        var renglones = lista["alumnos"]!.AsArray().ToDictionary(r => (string)r!["matricula"]!, r => r!);
        Assert.True((bool)renglones[presente]["presente"]!);
        Assert.Equal("PC-1", (string)renglones[presente]["equipo"]!);
        Assert.False((bool)renglones[ausente]["presente"]!);
        Assert.Null(renglones[ausente]["horaEntrada"]);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Docente.GetAsync("/api/clases/999999/lista")).StatusCode);
    }

    [Fact]
    public async Task El_resumen_del_curso_cuenta_asistencias_y_faltas_por_alumno()
    {
        var e = await CrearEscenario();
        var constante = await InscribirAlumno(e);
        var irregular = await InscribirAlumno(e);

        var sinClases = await e.Docente.GetFromJsonAsync<JsonObject>($"/api/cursos/{e.CursoId}/asistencia");
        for (var i = 0; i < 3; i++)
        {
            var claseId = await IdCreado(await e.Docente.PostAsJsonAsync("/api/clases", new { cursoId = e.CursoId }));
            await RegistrarEntrada(constante);
            if (i == 0) await RegistrarEntrada(irregular);
            await e.Docente.PostAsync($"/api/clases/{claseId}/cerrar", null);
        }
        var resumen = await e.Docente.GetFromJsonAsync<JsonObject>($"/api/cursos/{e.CursoId}/asistencia");

        Assert.Equal(0, (int)sinClases!["totalClases"]!);
        Assert.All(sinClases["alumnos"]!.AsArray(), a => Assert.Null(a!["porcentaje"]));
        Assert.Equal(3, (int)resumen!["totalClases"]!);
        var alumnos = resumen["alumnos"]!.AsArray().ToDictionary(a => (string)a!["matricula"]!, a => a!);
        Assert.Equal(3, (int)alumnos[constante]["asistencias"]!);
        Assert.Equal(100m, (decimal)alumnos[constante]["porcentaje"]!);
        Assert.Equal(1, (int)alumnos[irregular]["asistencias"]!);
        Assert.Equal(2, (int)alumnos[irregular]["faltas"]!);
        Assert.Equal(33.3m, (decimal)alumnos[irregular]["porcentaje"]!);
    }

    [Fact]
    public async Task Quien_lleva_la_clase_registra_a_mano_y_quita_entradas_equivocadas()
    {
        var e = await CrearEscenario();
        var otroDocente = await api.ClienteConSesionAsync(Rol.Docente);
        var laboratorista = await api.ClienteConSesionAsync(Rol.Laboratorista);
        var matricula = await InscribirAlumno(e);
        var noInscrito = await InscribirAlumno(e, inscrito: false);
        var claseId = await IdCreado(await e.Docente.PostAsJsonAsync("/api/clases", new { cursoId = e.CursoId }));
        var ruta = $"/api/clases/{claseId}/asistencias";

        Assert.Equal(HttpStatusCode.Forbidden, (await otroDocente.PostAsJsonAsync(ruta, new { matricula })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await e.Docente.PostAsJsonAsync(ruta, new { matricula = noInscrito })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Docente.PostAsJsonAsync(ruta, new { matricula = "no-existe" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.Docente.PostAsJsonAsync(ruta, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await e.Docente.PostAsJsonAsync(ruta, new { matricula, equipo = "PC-1" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await laboratorista.PostAsJsonAsync(ruta, new { matricula })).StatusCode);

        var asistenciaId = (long)Assert.Single((await e.Docente.GetFromJsonAsync<JsonArray>(ruta))!)!["asistenciaId"]!;
        Assert.Equal(HttpStatusCode.Forbidden, (await otroDocente.DeleteAsync($"{ruta}/{asistenciaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await laboratorista.DeleteAsync($"{ruta}/{asistenciaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await laboratorista.DeleteAsync($"{ruta}/{asistenciaId}")).StatusCode);

        // Al quitarla, el alumno y el equipo quedan libres para registrarse de nuevo
        Assert.Equal(HttpStatusCode.OK, (await RegistrarEntrada(matricula, "PC-1")).StatusCode);
        var enBitacora = await api.EnBaseAsync(db => Task.FromResult(db.Bitacora
            .Count(b => b.Entidad == "asistencias" && b.EntidadId == asistenciaId.ToString() && b.Accion == "eliminar")));
        Assert.Equal(1, enBitacora);

        await e.Docente.PostAsync($"/api/clases/{claseId}/cerrar", null);
        Assert.Equal(HttpStatusCode.Conflict, (await e.Docente.PostAsJsonAsync(ruta, new { matricula })).StatusCode);
    }

    [Fact]
    public async Task Solo_abre_la_clase_el_docente_del_curso_o_el_administrador()
    {
        var e = await CrearEscenario();
        var otroDocente = await api.ClienteConSesionAsync(Rol.Docente);
        var laboratorista = await api.ClienteConSesionAsync(Rol.Laboratorista);
        var clase = new { cursoId = e.CursoId };

        Assert.Equal(HttpStatusCode.Forbidden, (await otroDocente.PostAsJsonAsync("/api/clases", clase)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await laboratorista.PostAsJsonAsync("/api/clases", clase)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().PostAsJsonAsync("/api/clases", clase)).StatusCode);
        var claseId = await IdCreado(await e.Admin.PostAsJsonAsync("/api/clases", clase));

        Assert.Equal(HttpStatusCode.OK, (await laboratorista.GetAsync($"/api/clases/{claseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otroDocente.PostAsync($"/api/clases/{claseId}/cerrar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await laboratorista.PostAsync($"/api/clases/{claseId}/cerrar", null)).StatusCode);
    }

    [Fact]
    public async Task No_hay_dos_clases_abiertas_del_mismo_curso_ni_en_el_mismo_laboratorio()
    {
        var e = await CrearEscenario();
        var otro = await CrearEscenario();
        var claseId = await IdCreado(await e.Docente.PostAsJsonAsync("/api/clases", new { cursoId = e.CursoId }));

        var mismoCurso = await e.Docente.PostAsJsonAsync("/api/clases", new { cursoId = e.CursoId });
        var mismoLaboratorio = await otro.Docente.PostAsJsonAsync("/api/clases", new { cursoId = otro.CursoId, laboratorioId = e.LaboratorioId });
        var abiertas = await e.Admin.GetFromJsonAsync<JsonArray>($"/api/clases?abiertas=true&laboratorioId={e.LaboratorioId}");

        Assert.Equal(HttpStatusCode.Conflict, mismoCurso.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, mismoLaboratorio.StatusCode);
        Assert.Equal(claseId, (int)Assert.Single(abiertas!)!["id"]!);

        await e.Docente.PostAsync($"/api/clases/{claseId}/cerrar", null);

        await IdCreado(await otro.Docente.PostAsJsonAsync("/api/clases", new { cursoId = otro.CursoId, laboratorioId = e.LaboratorioId }));
        Assert.Empty((await e.Admin.GetFromJsonAsync<JsonArray>($"/api/clases?abiertas=true&cursoId={e.CursoId}"))!);
    }

    [Fact]
    public async Task Abrir_una_clase_valida_el_laboratorio_y_la_practica()
    {
        var e = await CrearEscenario();
        var otro = await CrearEscenario();
        var practicaAjena = await IdCreado(await otro.Admin.PostAsJsonAsync($"/api/materias/{otro.MateriaId}/practicas", new { numero = 1, titulo = "Ajena" }));
        var practica = await IdCreado(await e.Admin.PostAsJsonAsync($"/api/materias/{e.MateriaId}/practicas", new { numero = 1, titulo = "Cableado" }));
        var periodoId = await IdCreado(await e.Admin.PostAsJsonAsync("/api/periodos",
            new { nombre = Unico("Ene-Jun"), fechaInicio = "2027-01-18", fechaFin = "2027-06-11" }));
        var sinLaboratorio = await IdCreado(await e.Admin.PostAsJsonAsync("/api/cursos", new { materiaId = e.MateriaId, periodoId, grupo = "5B" }));

        Assert.Equal(HttpStatusCode.BadRequest, (await e.Admin.PostAsJsonAsync("/api/clases", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Admin.PostAsJsonAsync("/api/clases", new { cursoId = 999999 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.Admin.PostAsJsonAsync("/api/clases", new { cursoId = sinLaboratorio })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.Admin.PostAsJsonAsync("/api/clases", new { cursoId = e.CursoId, laboratorioId = 999999 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.Admin.PostAsJsonAsync("/api/clases", new { cursoId = e.CursoId, practicaId = practicaAjena })).StatusCode);

        var abierta = await e.Docente.PostAsJsonAsync("/api/clases", new { cursoId = e.CursoId, practicaId = practica });
        var cuerpo = await abierta.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(HttpStatusCode.Created, abierta.StatusCode);
        Assert.Equal("Cableado", (string)cuerpo!["practica"]!);
        Assert.Equal("Usuario De Prueba", (string)cuerpo["abiertaPor"]!);
        Assert.Null(cuerpo["cerradaEn"]);
    }
}

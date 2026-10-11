using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Asistencias.Api.Datos;

namespace Asistencias.Tests;

public class CursosTests(ApiDePrueba api) : IClassFixture<ApiDePrueba>
{
    private static string Unico(string prefijo) => $"{prefijo}-{Guid.NewGuid():N}"[..18];

    private static async Task<int> IdCreado(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (int)(await respuesta.Content.ReadFromJsonAsync<JsonObject>())!["id"]!;
    }

    private static async Task<int> CrearMateria(HttpClient admin) =>
        await IdCreado(await admin.PostAsJsonAsync("/api/materias", new { clave = Unico("MAT"), nombre = "Programación" }));

    private static async Task<int> CrearPeriodo(HttpClient admin) =>
        await IdCreado(await admin.PostAsJsonAsync("/api/periodos",
            new { nombre = Unico("Ago-Dic"), fechaInicio = "2026-08-17", fechaFin = "2026-12-11" }));

    private static async Task<int> CrearAlumno(HttpClient admin, bool activo = true) =>
        await IdCreado(await admin.PostAsJsonAsync("/api/alumnos",
            new { matricula = Unico("M"), nombre = "Ana", apellido = "Ruiz", activo }));

    private async Task<int> CrearCurso(HttpClient admin, int? docenteId = null, int? materiaId = null) =>
        await IdCreado(await admin.PostAsJsonAsync("/api/cursos", new
        {
            materiaId = materiaId ?? await CrearMateria(admin),
            periodoId = await CrearPeriodo(admin),
            grupo = "5A",
            docenteId,
        }));

    [Fact]
    public async Task Curso_ciclo_completo()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var docente = await api.CrearUsuarioAsync(Rol.Docente);
        var materiaId = await CrearMateria(admin);
        var periodoId = await CrearPeriodo(admin);
        var laboratorioId = await IdCreado(await admin.PostAsJsonAsync("/api/laboratorios", new { clave = Unico("lab"), nombre = "Redes" }));
        var curso = new { materiaId, periodoId, grupo = " 5A ", docenteId = docente.Id, laboratorioId };

        var creado = await admin.PostAsJsonAsync("/api/cursos", curso);
        var repetido = await admin.PostAsJsonAsync("/api/cursos", curso);

        var cuerpo = await creado.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        Assert.Equal("5A", (string)cuerpo!["grupo"]!);
        Assert.Equal("Programación", (string)cuerpo["materia"]!);
        Assert.Equal("Usuario De Prueba", (string)cuerpo["docente"]!);
        Assert.Equal("Redes", (string)cuerpo["laboratorio"]!);
        Assert.Equal(0, (int)cuerpo["inscritos"]!);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);

        var id = (int)cuerpo["id"]!;
        var editado = await admin.PutAsJsonAsync($"/api/cursos/{id}", new { materiaId, periodoId, grupo = "5B" });
        var leido = await admin.GetFromJsonAsync<JsonObject>($"/api/cursos/{id}");
        var delPeriodo = await admin.GetFromJsonAsync<JsonArray>($"/api/cursos?periodoId={periodoId}");
        var delDocente = await admin.GetFromJsonAsync<JsonArray>($"/api/cursos?docenteId={docente.Id}");

        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);
        Assert.Equal("5B", (string)leido!["grupo"]!);
        Assert.Null(leido["docente"]);
        Assert.Null(leido["laboratorio"]);
        Assert.Single(delPeriodo!);
        Assert.Empty(delDocente!);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/cursos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/cursos/{id}")).StatusCode);
    }

    [Fact]
    public async Task Curso_con_datos_invalidos_responde_400()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var materiaId = await CrearMateria(admin);
        var periodoId = await CrearPeriodo(admin);
        var laboratorista = await api.CrearUsuarioAsync(Rol.Laboratorista);
        var docenteInactivo = await api.CrearUsuarioAsync(Rol.Docente, activo: false);

        var vacio = await admin.PostAsJsonAsync("/api/cursos", new { });
        var sinMateria = await admin.PostAsJsonAsync("/api/cursos", new { materiaId = 999999, periodoId, grupo = "A" });
        var noEsDocente = await admin.PostAsJsonAsync("/api/cursos", new { materiaId, periodoId, grupo = "A", docenteId = laboratorista.Id });
        var inactivo = await admin.PostAsJsonAsync("/api/cursos", new { materiaId, periodoId, grupo = "B", docenteId = docenteInactivo.Id });
        var inexistente = await admin.PostAsJsonAsync("/api/cursos", new { materiaId, periodoId, grupo = "C", docenteId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, vacio.StatusCode);
        var errores = (await vacio.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject();
        Assert.Equal(["grupo", "materiaId", "periodoId"], errores.Select(e => e.Key).Order());
        Assert.Equal(HttpStatusCode.BadRequest, sinMateria.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noEsDocente.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, inactivo.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, inexistente.StatusCode);
    }

    [Fact]
    public async Task Un_curso_conserva_a_su_docente_aunque_despues_se_desactive()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var docente = await api.CrearUsuarioAsync(Rol.Docente);
        var materiaId = await CrearMateria(admin);
        var periodoId = await CrearPeriodo(admin);
        var id = await IdCreado(await admin.PostAsJsonAsync("/api/cursos", new { materiaId, periodoId, grupo = "A", docenteId = docente.Id }));
        await api.EnBaseAsync(async db =>
        {
            (await db.Usuarios.FindAsync(docente.Id))!.Activo = false;
            await db.SaveChangesAsync();
        });

        var editado = await admin.PutAsJsonAsync($"/api/cursos/{id}", new { materiaId, periodoId, grupo = "B", docenteId = docente.Id });

        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);
    }

    [Theory]
    [InlineData(Rol.Docente)]
    [InlineData(Rol.Laboratorista)]
    public async Task Los_cursos_se_consultan_con_cualquier_rol_pero_solo_el_administrador_los_modifica(Rol rol)
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var id = await CrearCurso(admin);
        var cliente = await api.ClienteConSesionAsync(rol);

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/cursos")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/cursos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/cursos/{id}/alumnos")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/cursos", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PutAsJsonAsync($"/api/cursos/{id}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.DeleteAsync($"/api/cursos/{id}")).StatusCode);
    }

    [Fact]
    public async Task El_laboratorista_inscribe_y_da_de_baja_alumnos()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var laboratorista = await api.ClienteConSesionAsync(Rol.Laboratorista);
        var cursoId = await CrearCurso(admin);
        var alumnoId = await CrearAlumno(admin);
        var ruta = $"/api/cursos/{cursoId}/alumnos/{alumnoId}";

        var primera = await laboratorista.PutAsync(ruta, null);
        var segunda = await laboratorista.PutAsync(ruta, null);

        Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, segunda.StatusCode);
        var inscritos = await laboratorista.GetFromJsonAsync<JsonArray>($"/api/cursos/{cursoId}/alumnos");
        var curso = await laboratorista.GetFromJsonAsync<JsonObject>($"/api/cursos/{cursoId}");
        var cursosDelAlumno = await laboratorista.GetFromJsonAsync<JsonArray>($"/api/alumnos/{alumnoId}/cursos");
        Assert.Equal(alumnoId, (int)Assert.Single(inscritos!)!["id"]!);
        Assert.Equal(1, (int)curso!["inscritos"]!);
        Assert.Equal(cursoId, (int)Assert.Single(cursosDelAlumno!)!["id"]!);

        Assert.Equal(HttpStatusCode.NoContent, (await laboratorista.DeleteAsync(ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await laboratorista.DeleteAsync(ruta)).StatusCode);
        Assert.Empty((await laboratorista.GetFromJsonAsync<JsonArray>($"/api/cursos/{cursoId}/alumnos"))!);
    }

    [Fact]
    public async Task Se_inscribe_una_lista_de_numeros_de_control_y_se_informa_cuales_no()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var otroDocente = await api.ClienteConSesionAsync(Rol.Docente);
        var cursoId = await CrearCurso(admin);
        var ruta = $"/api/cursos/{cursoId}/alumnos";
        async Task<string> Alumno(bool activo = true)
        {
            var matricula = Unico("L");
            await IdCreado(await admin.PostAsJsonAsync("/api/alumnos", new { matricula, nombre = "Ana", apellido = "Ruiz", activo }));
            return matricula;
        }
        var (a, b, yaInscrito, inactivo) = (await Alumno(), await Alumno(), await Alumno(), await Alumno(activo: false));
        await admin.PostAsJsonAsync(ruta, new { matriculas = new[] { yaInscrito } });

        var respuesta = await admin.PostAsJsonAsync(ruta, new { matriculas = new[] { a, $" {b} ", a, yaInscrito, inactivo, "no-existe" } });

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(2, (int)cuerpo!["inscritos"]!);
        Assert.Equal(1, (int)cuerpo["yaInscritos"]!);
        Assert.Equal(["no-existe"], cuerpo["noEncontrados"]!.AsArray().Select(m => (string)m!));
        Assert.Equal([inactivo], cuerpo["desactivados"]!.AsArray().Select(m => (string)m!));
        Assert.Equal(3, (await admin.GetFromJsonAsync<JsonArray>(ruta))!.Count);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(ruta, new { matriculas = Array.Empty<string>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otroDocente.PostAsJsonAsync(ruta, new { matriculas = new[] { a } })).StatusCode);
    }

    [Fact]
    public async Task No_se_inscribe_a_un_alumno_desactivado_ni_a_uno_inexistente()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var cursoId = await CrearCurso(admin);
        var inactivoId = await CrearAlumno(admin, activo: false);

        var inactivo = await admin.PutAsync($"/api/cursos/{cursoId}/alumnos/{inactivoId}", null);
        var inexistente = await admin.PutAsync($"/api/cursos/{cursoId}/alumnos/999999", null);
        var cursoInexistente = await admin.PutAsync($"/api/cursos/999999/alumnos/{inactivoId}", null);

        Assert.Equal(HttpStatusCode.Conflict, inactivo.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, cursoInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/cursos/999999/alumnos")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/alumnos/999999/cursos")).StatusCode);
    }

    [Fact]
    public async Task El_docente_solo_lleva_la_lista_de_sus_propios_cursos()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var (titular, usuarioTitular) = await api.SesionAsync(Rol.Docente);
        var otroDocente = await api.ClienteConSesionAsync(Rol.Docente);
        var cursoId = await CrearCurso(admin, docenteId: usuarioTitular.Id);
        var ruta = $"/api/cursos/{cursoId}/alumnos/{await CrearAlumno(admin)}";

        Assert.Equal(HttpStatusCode.Forbidden, (await otroDocente.PutAsync(ruta, null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await titular.PutAsync(ruta, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otroDocente.DeleteAsync(ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await titular.DeleteAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task Borrar_un_curso_quita_sus_inscripciones_pero_no_a_los_alumnos()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var cursoId = await CrearCurso(admin);
        var alumnoId = await CrearAlumno(admin);
        await admin.PutAsync($"/api/cursos/{cursoId}/alumnos/{alumnoId}", null);

        var respuesta = await admin.DeleteAsync($"/api/cursos/{cursoId}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/alumnos/{alumnoId}")).StatusCode);
        Assert.Empty((await admin.GetFromJsonAsync<JsonArray>($"/api/alumnos/{alumnoId}/cursos"))!);
    }

    [Fact]
    public async Task Un_curso_o_una_practica_con_asistencias_no_se_puede_borrar()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var materiaId = await CrearMateria(admin);
        var cursoId = await CrearCurso(admin, materiaId: materiaId);
        var practicaId = await IdCreado(await admin.PostAsJsonAsync($"/api/materias/{materiaId}/practicas", new { numero = 1, titulo = "Ciclos" }));
        var laboratorioId = await IdCreado(await admin.PostAsJsonAsync("/api/laboratorios", new { clave = Unico("hist"), nombre = "Historial" }));
        await api.EnBaseAsync(async db =>
        {
            db.Asistencias.Add(new Asistencia
            {
                LaboratorioId = laboratorioId, TipoUsuario = TipoUsuario.Invitado, InvitadoNombre = "Visita",
                CursoId = cursoId, PracticaId = practicaId,
                HoraEntrada = DateTime.UtcNow.AddHours(-2), HoraSalida = DateTime.UtcNow.AddHours(-1),
            });
            await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/cursos/{cursoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/practicas/{practicaId}")).StatusCode);
    }

    [Fact]
    public async Task Practica_ciclo_completo()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var materiaId = await CrearMateria(admin);
        var ruta = $"/api/materias/{materiaId}/practicas";

        var segunda = await admin.PostAsJsonAsync(ruta, new { numero = 2, titulo = " Arreglos ", descripcion = "Recorridos", duracionMinutos = 90 });
        var primera = await admin.PostAsJsonAsync(ruta, new { numero = 1, titulo = "Ciclos" });
        var repetida = await admin.PostAsJsonAsync(ruta, new { numero = 2, titulo = "Otra" });

        var cuerpo = await segunda.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        Assert.Equal("Arreglos", (string)cuerpo!["titulo"]!);
        Assert.Equal(90, (int)cuerpo["duracionMinutos"]!);
        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
        var lista = await admin.GetFromJsonAsync<JsonArray>(ruta);
        Assert.Equal([1, 2], lista!.Select(p => (int)p!["numero"]!));

        var id = (int)cuerpo["id"]!;
        var editada = await admin.PutAsJsonAsync($"/api/practicas/{id}", new { numero = 3, titulo = "Arreglos" });
        var cuerpoEditada = await editada.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.Equal(3, (int)cuerpoEditada!["numero"]!);
        Assert.Null(cuerpoEditada["duracionMinutos"]);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/practicas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/practicas/{id}")).StatusCode);
    }

    [Fact]
    public async Task Practica_con_datos_invalidos_responde_400()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var ruta = $"/api/materias/{await CrearMateria(admin)}/practicas";

        var vacia = await admin.PostAsJsonAsync(ruta, new { });
        var numeroCero = await admin.PostAsJsonAsync(ruta, new { numero = 0, titulo = "X" });
        var duracionCero = await admin.PostAsJsonAsync(ruta, new { numero = 1, titulo = "X", duracionMinutos = 0 });
        var sinMateria = await admin.PostAsJsonAsync("/api/materias/999999/practicas", new { numero = 1, titulo = "X" });

        Assert.Equal(HttpStatusCode.BadRequest, vacia.StatusCode);
        var errores = (await vacia.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject();
        Assert.Equal(["numero", "titulo"], errores.Select(e => e.Key).Order());
        Assert.Equal(HttpStatusCode.BadRequest, numeroCero.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duracionCero.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, sinMateria.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/materias/999999/practicas")).StatusCode);
    }

    [Fact]
    public async Task Las_practicas_las_edita_el_administrador_o_un_docente_que_imparte_la_materia()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var (titular, usuarioTitular) = await api.SesionAsync(Rol.Docente);
        var otroDocente = await api.ClienteConSesionAsync(Rol.Docente);
        var laboratorista = await api.ClienteConSesionAsync(Rol.Laboratorista);
        var materiaId = await CrearMateria(admin);
        await CrearCurso(admin, docenteId: usuarioTitular.Id, materiaId: materiaId);
        var ruta = $"/api/materias/{materiaId}/practicas";
        var practica = new { numero = 1, titulo = "Ciclos" };

        Assert.Equal(HttpStatusCode.Forbidden, (await otroDocente.PostAsJsonAsync(ruta, practica)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await laboratorista.PostAsJsonAsync(ruta, practica)).StatusCode);
        var id = await IdCreado(await titular.PostAsJsonAsync(ruta, practica));

        Assert.Equal(HttpStatusCode.OK, (await laboratorista.GetAsync(ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otroDocente.PutAsJsonAsync($"/api/practicas/{id}", practica)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await laboratorista.DeleteAsync($"/api/practicas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await titular.PutAsJsonAsync($"/api/practicas/{id}", practica)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await titular.DeleteAsync($"/api/practicas/{id}")).StatusCode);
    }

    [Fact]
    public async Task Inscribir_y_dar_de_baja_queda_en_la_bitacora()
    {
        var admin = await api.ClienteConSesionAsync(Rol.Admin);
        var cursoId = await CrearCurso(admin);
        var alumnoId = await CrearAlumno(admin);
        var ruta = $"/api/cursos/{cursoId}/alumnos/{alumnoId}";

        await admin.PutAsync(ruta, null);
        await admin.DeleteAsync(ruta);

        var acciones = await api.EnBaseAsync(db => Task.FromResult(db.Bitacora
            .Where(b => b.Entidad == "inscripciones" && b.EntidadId == $"{cursoId},{alumnoId}")
            .OrderBy(b => b.Id).Select(b => b.Accion).ToList()));
        Assert.Equal(["crear", "eliminar"], acciones);
    }
}

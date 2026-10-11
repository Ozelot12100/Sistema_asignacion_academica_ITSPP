import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate } from "react-router-dom";
import { api, resultado } from "../api/cliente";
import { Aviso, Boton, Cargando, Tarjeta } from "../componentes/ui";
import { fechaYHora, mensaje } from "../formato";
import type { Usuario } from "../sesion";

// El docente ve sus cursos y sus clases; el administrador y el laboratorista, los de todos
export function Panel({ usuario }: { usuario: Usuario }) {
  const soloLoMio = usuario.rol === "docente" ? { docenteId: usuario.id } : {};
  const puedeAbrir = usuario.rol !== "laboratorista";
  const navegar = useNavigate();
  const cache = useQueryClient();

  const cursos = useQuery({
    queryKey: ["cursos", soloLoMio],
    queryFn: () => resultado(api.GET("/api/cursos", { params: { query: soloLoMio } })),
    enabled: puedeAbrir,
  });
  const abiertas = useQuery({
    queryKey: ["clases", { abiertas: true, ...soloLoMio }],
    queryFn: () => resultado(api.GET("/api/clases", { params: { query: { abiertas: true, ...soloLoMio } } })),
    refetchInterval: 15_000,
  });
  const cerradas = useQuery({
    queryKey: ["clases", { abiertas: false, ...soloLoMio }],
    queryFn: () =>
      resultado(api.GET("/api/clases", { params: { query: { abiertas: false, ...soloLoMio } } })),
  });

  const abrir = useMutation({
    mutationFn: (cursoId: number) =>
      resultado(api.POST("/api/clases", { body: { cursoId, laboratorioId: null, practicaId: null } })),
    onSuccess: (clase) => {
      void cache.invalidateQueries({ queryKey: ["clases"] });
      navegar(`/clases/${clase.id}`);
    },
  });

  if (abiertas.isPending || (puedeAbrir && cursos.isPending)) return <Cargando />;
  const error = abiertas.error ?? cursos.error ?? abrir.error;
  const claseAbiertaDe = new Map((abiertas.data ?? []).map((c) => [c.cursoId, c]));

  return (
    <div className="space-y-6">
      {error && <Aviso tipo="error">{mensaje(error)}</Aviso>}

      <section>
        <h2 className="mb-2 text-lg font-bold">Clases abiertas</h2>
        {abiertas.data?.length ? (
          <ul className="grid gap-3 sm:grid-cols-2">
            {abiertas.data.map((clase) => (
              <li key={clase.id}>
                <Link to={`/clases/${clase.id}`} className="block">
                  <Tarjeta className="border-l-4 border-l-emerald-500 transition hover:shadow-md">
                    <p className="font-semibold">
                      {clase.materia} · Grupo {clase.grupo}
                    </p>
                    <p className="text-sm text-slate-600">{clase.laboratorio}</p>
                    <p className="mt-1 text-sm text-slate-500">
                      Abierta {fechaYHora(clase.abiertaEn)} · {clase.registrados} registrados
                    </p>
                  </Tarjeta>
                </Link>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-slate-500">No hay ninguna clase abierta en este momento.</p>
        )}
      </section>

      {puedeAbrir && (
        <section>
          <h2 className="mb-2 text-lg font-bold">{usuario.rol === "docente" ? "Mis cursos" : "Cursos"}</h2>
          {cursos.data?.length ? (
            <ul className="grid gap-3 sm:grid-cols-2">
              {cursos.data.map((curso) => {
                const abierta = claseAbiertaDe.get(curso.id);
                return (
                  <li key={curso.id}>
                    <Tarjeta className="flex h-full flex-col gap-3">
                      <div>
                        <p className="font-semibold">
                          {curso.materia} · Grupo {curso.grupo}
                        </p>
                        <p className="text-sm text-slate-600">
                          {curso.periodo} · {curso.laboratorio ?? "Sin laboratorio asignado"}
                        </p>
                        <p className="text-sm text-slate-500">
                          {curso.inscritos} {curso.inscritos === 1 ? "alumno inscrito" : "alumnos inscritos"}
                          {usuario.rol !== "docente" && curso.docente ? ` · ${curso.docente}` : ""}
                        </p>
                      </div>
                      {abierta ? (
                        <Boton
                          variante="secundario"
                          className="mt-auto"
                          onClick={() => navegar(`/clases/${abierta.id}`)}
                        >
                          Ver la clase abierta
                        </Boton>
                      ) : (
                        <Boton
                          className="mt-auto"
                          disabled={abrir.isPending || curso.laboratorioId === null}
                          onClick={() => abrir.mutate(curso.id)}
                        >
                          Abrir clase
                        </Boton>
                      )}
                    </Tarjeta>
                  </li>
                );
              })}
            </ul>
          ) : (
            <p className="text-sm text-slate-500">
              {usuario.rol === "docente"
                ? "Todavía no tienes cursos asignados. Pídele al administrador que te asigne uno."
                : "Todavía no hay cursos registrados."}
            </p>
          )}
        </section>
      )}

      <section>
        <h2 className="mb-2 text-lg font-bold">Clases anteriores</h2>
        {cerradas.data?.length ? (
          <Tarjeta sinRelleno>
            <ul className="divide-y divide-slate-200">
              {cerradas.data.slice(0, 20).map((clase) => (
                <li key={clase.id}>
                  <Link
                    to={`/clases/${clase.id}`}
                    className="flex flex-wrap justify-between gap-x-4 px-5 py-3 hover:bg-slate-50"
                  >
                    <span className="font-medium">
                      {clase.materia} · Grupo {clase.grupo}
                    </span>
                    <span className="text-sm text-slate-500">
                      {fechaYHora(clase.abiertaEn)} · {clase.registrados} asistentes
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          </Tarjeta>
        ) : (
          <p className="text-sm text-slate-500">Aún no hay clases cerradas.</p>
        )}
      </section>
    </div>
  );
}

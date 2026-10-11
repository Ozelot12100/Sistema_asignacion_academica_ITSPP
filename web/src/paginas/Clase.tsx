import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { api, resultado } from "../api/cliente";
import { Aviso, Boton, Campo, Cargando, Tarjeta } from "../componentes/ui";
import { fechaYHora, hora, mensaje } from "../formato";
import type { Usuario } from "../sesion";

export function Clase({ usuario }: { usuario: Usuario }) {
  const id = Number(useParams().id);
  const cache = useQueryClient();
  const [matricula, setMatricula] = useState("");
  const [equipo, setEquipo] = useState("");

  const clase = useQuery({
    queryKey: ["clases", id],
    queryFn: () => resultado(api.GET("/api/clases/{id}", { params: { path: { id } } })),
  });
  const abierta = clase.data ? clase.data.cerradaEn === null : false;
  const lista = useQuery({
    queryKey: ["clases", id, "lista"],
    queryFn: () => resultado(api.GET("/api/clases/{id}/lista", { params: { path: { id } } })),
    // Mientras la clase está abierta, la lista se actualiza sola conforme llegan los alumnos
    refetchInterval: abierta ? 5_000 : false,
  });

  const refrescar = () => cache.invalidateQueries({ queryKey: ["clases"] });
  const registrar = useMutation({
    mutationFn: () =>
      resultado(
        api.POST("/api/clases/{id}/asistencias", {
          params: { path: { id } },
          body: { matricula: matricula.trim(), equipo: equipo.trim() || null },
        }),
      ),
    onSuccess: () => {
      setMatricula("");
      setEquipo("");
      void refrescar();
    },
  });
  const quitar = useMutation({
    mutationFn: (asistenciaId: number) =>
      resultado(
        api.DELETE("/api/clases/{id}/asistencias/{asistenciaId}", { params: { path: { id, asistenciaId } } }),
      ),
    onSuccess: refrescar,
  });
  const cerrar = useMutation({
    mutationFn: () => resultado(api.POST("/api/clases/{id}/cerrar", { params: { path: { id } } })),
    onSuccess: refrescar,
  });

  if (clase.isPending || lista.isPending) return <Cargando />;
  if (clase.isError || lista.isError)
    return <Aviso tipo="error">{mensaje(clase.error ?? lista.error)}</Aviso>;

  // El servidor decide quién puede; aquí solo se evita mostrar botones que responderían "sin permiso"
  const puedeLlevarla = usuario.rol !== "docente" || clase.data.abiertaPorId === usuario.id;
  const error = registrar.error ?? quitar.error ?? cerrar.error;

  function enviar(evento: FormEvent) {
    evento.preventDefault();
    registrar.mutate();
  }

  return (
    <div className="space-y-4">
      <Link to="/panel" className="text-sm font-semibold text-blue-800 hover:underline">
        ← Volver al panel
      </Link>

      <Tarjeta className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-xl font-bold">
            {clase.data.materia} · Grupo {clase.data.grupo}
          </h2>
          <p className="text-slate-600">
            {clase.data.laboratorio}
            {clase.data.practica ? ` · Práctica: ${clase.data.practica}` : ""}
          </p>
          <p className="mt-1 text-sm text-slate-500">
            Abierta {fechaYHora(clase.data.abiertaEn)} por {clase.data.abiertaPor}
            {clase.data.cerradaEn ? ` · Cerrada ${fechaYHora(clase.data.cerradaEn)}` : ""}
          </p>
        </div>
        <span
          className={`rounded-full px-3 py-1 text-sm font-semibold ${abierta ? "bg-emerald-100 text-emerald-800" : "bg-slate-200 text-slate-700"}`}
        >
          {abierta ? "Abierta" : "Cerrada"}
        </span>
      </Tarjeta>

      <div className="grid grid-cols-3 gap-3 text-center">
        {(
          [
            ["Inscritos", lista.data.inscritos, "text-slate-900"],
            ["Presentes", lista.data.presentes, "text-emerald-700"],
            [
              abierta ? "Sin registrar" : "Faltas",
              lista.data.faltas,
              abierta ? "text-slate-500" : "text-red-700",
            ],
          ] as const
        ).map(([titulo, valor, color]) => (
          <Tarjeta key={titulo} className="py-3">
            <p className={`text-3xl font-bold ${color}`}>{valor}</p>
            <p className="text-sm text-slate-500">{titulo}</p>
          </Tarjeta>
        ))}
      </div>

      {error && <Aviso tipo="error">{mensaje(error)}</Aviso>}

      {abierta && puedeLlevarla && (
        <Tarjeta>
          <h3 className="mb-3 font-semibold">Registrar a un alumno a mano</h3>
          <form onSubmit={enviar} className="grid items-end gap-3 sm:grid-cols-[1fr_1fr_auto]">
            <Campo
              etiqueta="Número de control"
              value={matricula}
              onChange={(e) => setMatricula(e.target.value)}
              inputMode="numeric"
              autoComplete="off"
              maxLength={20}
              required
            />
            <Campo
              etiqueta="Equipo (opcional)"
              value={equipo}
              onChange={(e) => setEquipo(e.target.value)}
              autoComplete="off"
              maxLength={50}
            />
            <Boton type="submit" disabled={registrar.isPending}>
              Registrar
            </Boton>
          </form>
        </Tarjeta>
      )}

      <Tarjeta sinRelleno className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="px-4 py-3">Alumno</th>
              <th className="px-4 py-3">N.º de control</th>
              <th className="px-4 py-3">Asistencia</th>
              <th className="px-4 py-3">Equipo</th>
              <th className="px-4 py-3">Entrada</th>
              <th className="px-4 py-3">Salida</th>
              {puedeLlevarla && <th className="px-4 py-3" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200">
            {lista.data.alumnos.map((alumno) => (
              <tr key={alumno.alumnoId}>
                <td className="px-4 py-3 font-medium">
                  {alumno.apellido} {alumno.nombre}
                  {!alumno.inscrito && <span className="ml-2 text-xs text-amber-700">(ya no inscrito)</span>}
                </td>
                <td className="px-4 py-3 tabular-nums">{alumno.matricula}</td>
                <td className="px-4 py-3">
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-semibold ${alumno.presente ? "bg-emerald-100 text-emerald-800" : abierta ? "bg-slate-100 text-slate-600" : "bg-red-100 text-red-800"}`}
                  >
                    {alumno.presente ? "Presente" : abierta ? "Sin registrar" : "Falta"}
                  </span>
                </td>
                <td className="px-4 py-3">{alumno.equipo ?? "—"}</td>
                <td className="px-4 py-3 tabular-nums">{hora(alumno.horaEntrada)}</td>
                <td className="px-4 py-3 tabular-nums">{hora(alumno.horaSalida)}</td>
                {puedeLlevarla && (
                  <td className="px-4 py-3 text-right">
                    {alumno.asistenciaId !== null && (
                      <button
                        className="text-xs font-semibold text-red-700 hover:underline disabled:opacity-50"
                        disabled={quitar.isPending}
                        onClick={() => {
                          if (confirm(`¿Quitar la entrada de ${alumno.nombre} ${alumno.apellido}?`))
                            quitar.mutate(alumno.asistenciaId!);
                        }}
                      >
                        Quitar entrada
                      </button>
                    )}
                  </td>
                )}
              </tr>
            ))}
            {lista.data.alumnos.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-6 text-center text-slate-500">
                  Este curso no tiene alumnos inscritos.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </Tarjeta>

      {abierta && puedeLlevarla && (
        <div className="flex justify-end">
          <Boton
            variante="peligro"
            disabled={cerrar.isPending}
            onClick={() => {
              if (confirm("¿Cerrar la clase? Se registrará la salida de todos los alumnos presentes."))
                cerrar.mutate();
            }}
          >
            Cerrar clase
          </Boton>
        </div>
      )}
    </div>
  );
}

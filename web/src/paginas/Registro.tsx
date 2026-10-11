import { useMutation } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { api, resultado } from "../api/cliente";
import { CampoDeEquipo, paraLaApi, SIN_EQUIPO } from "../componentes/CampoDeEquipo";
import { Aviso, Boton, Campo, Tarjeta } from "../componentes/ui";
import { hora, mensaje } from "../formato";

// La pantalla del alumno: no pide sesión, solo su número de control
export function Registro() {
  const [matricula, setMatricula] = useState("");
  const [equipo, setEquipo] = useState(SIN_EQUIPO);
  const registrar = useMutation({
    mutationFn: () =>
      resultado(
        api.POST("/api/registro/entrada", {
          body: { matricula: matricula.trim(), ...paraLaApi(equipo) },
        }),
      ),
  });

  function enviar(evento: FormEvent) {
    evento.preventDefault();
    registrar.mutate();
  }

  // En una computadora compartida, el siguiente alumno empieza con el formulario limpio
  function otroAlumno() {
    setMatricula("");
    setEquipo(SIN_EQUIPO);
    registrar.reset();
  }

  return (
    <main className="mx-auto flex min-h-dvh max-w-md flex-col justify-center gap-4 p-4">
      <header className="text-center">
        <p className="text-sm font-semibold uppercase tracking-wide text-blue-800">ITSPP · Laboratorios</p>
        <h1 className="text-2xl font-bold">Registro de entrada</h1>
      </header>

      {registrar.isSuccess ? (
        <Tarjeta className="text-center">
          <p className="text-4xl" aria-hidden>
            ✓
          </p>
          <h2 className="mt-1 text-xl font-bold text-emerald-700">Entrada registrada</h2>
          <p className="mt-2 text-lg font-semibold">{registrar.data.alumno}</p>
          <dl className="mt-3 space-y-1 text-sm text-slate-600">
            <div>
              {registrar.data.materia} · Grupo {registrar.data.grupo}
            </div>
            <div>{registrar.data.laboratorio}</div>
            <div>{registrar.data.equipo ? `Equipo ${registrar.data.equipo}` : "Con tu propio equipo"}</div>
            <div>Hora de entrada: {hora(registrar.data.horaEntrada)}</div>
          </dl>
          <p className="mt-3 text-xs text-slate-500">
            No necesitas registrar tu salida: se registra cuando el docente cierra la clase.
          </p>
          <Boton variante="secundario" className="mt-4 w-full" onClick={otroAlumno} autoFocus>
            Registrar a otro alumno
          </Boton>
        </Tarjeta>
      ) : (
        <Tarjeta>
          <form onSubmit={enviar} className="space-y-4">
            <Campo
              etiqueta="Número de control"
              value={matricula}
              onChange={(e) => setMatricula(e.target.value)}
              inputMode="numeric"
              autoComplete="off"
              maxLength={20}
              placeholder="Ejemplo: 23303080"
              className="text-center text-xl tracking-widest"
              autoFocus
              required
            />
            <CampoDeEquipo valor={equipo} alCambiar={setEquipo} />
            {registrar.isError && <Aviso tipo="error">{mensaje(registrar.error)}</Aviso>}
            <Boton type="submit" className="w-full py-3 text-base" disabled={registrar.isPending}>
              {registrar.isPending ? "Registrando…" : "Registrar entrada"}
            </Boton>
          </form>
        </Tarjeta>
      )}

      <p className="text-center text-sm text-slate-500">
        ¿Eres docente o personal?{" "}
        <Link to="/entrar" className="font-semibold text-blue-800 hover:underline">
          Entrar al sistema
        </Link>
      </p>
    </main>
  );
}

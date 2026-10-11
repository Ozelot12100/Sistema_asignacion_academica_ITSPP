import { useState, type FormEvent } from "react";
import { Link, Navigate, useNavigate } from "react-router-dom";
import { Aviso, Boton, Campo, Cargando, Tarjeta } from "../componentes/ui";
import { mensaje } from "../formato";
import { useEntrar, useSesion } from "../sesion";

export function Entrar() {
  const sesion = useSesion();
  const entrar = useEntrar();
  const navegar = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  if (sesion.isPending) return <Cargando />;
  if (sesion.data) return <Navigate to="/panel" replace />;

  function enviar(evento: FormEvent) {
    evento.preventDefault();
    entrar.mutate({ email, password }, { onSuccess: () => navegar("/panel", { replace: true }) });
  }

  return (
    <main className="mx-auto flex min-h-dvh max-w-md flex-col justify-center gap-4 p-4">
      <header className="text-center">
        <p className="text-sm font-semibold uppercase tracking-wide text-blue-800">ITSPP · Laboratorios</p>
        <h1 className="text-2xl font-bold">Entrar al sistema</h1>
      </header>
      <Tarjeta>
        <form onSubmit={enviar} className="space-y-4">
          <Campo
            etiqueta="Correo"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="username"
            autoFocus
            required
          />
          <Campo
            etiqueta="Contraseña"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            required
          />
          {entrar.isError && <Aviso tipo="error">{mensaje(entrar.error)}</Aviso>}
          <Boton type="submit" className="w-full" disabled={entrar.isPending}>
            {entrar.isPending ? "Entrando…" : "Entrar"}
          </Boton>
        </form>
      </Tarjeta>
      <p className="text-center text-sm text-slate-500">
        ¿Eres alumno?{" "}
        <Link to="/" className="font-semibold text-blue-800 hover:underline">
          Registra tu entrada
        </Link>
      </p>
    </main>
  );
}

import type { ReactNode } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { Aviso, Boton, Cargando } from "./componentes/ui";
import { mensaje } from "./formato";
import { Clase } from "./paginas/Clase";
import { Entrar } from "./paginas/Entrar";
import { Panel } from "./paginas/Panel";
import { Registro } from "./paginas/Registro";
import { NOMBRE_DE_ROL, useSalir, useSesion, type Usuario } from "./sesion";

// Las pantallas del personal: piden sesión y comparten el encabezado
function ConSesion({ children }: { children: (usuario: Usuario) => ReactNode }) {
  const sesion = useSesion();
  const salir = useSalir();

  if (sesion.isPending) return <Cargando />;
  if (sesion.isError) return <Aviso tipo="error">{mensaje(sesion.error)}</Aviso>;
  if (!sesion.data) return <Navigate to="/entrar" replace />;

  return (
    <div className="min-h-dvh">
      <header className="bg-blue-900 text-white">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-2 px-4 py-3">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-blue-200">
              ITSPP · Laboratorios
            </p>
            <h1 className="text-lg font-bold">Control de asistencias</h1>
          </div>
          <div className="flex items-center gap-3 text-sm">
            <span>
              {sesion.data.nombre} {sesion.data.apellido} · {NOMBRE_DE_ROL[sesion.data.rol]}
            </span>
            <Boton variante="secundario" className="py-1.5" onClick={() => salir.mutate()}>
              Salir
            </Boton>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-5xl p-4">{children(sesion.data)}</main>
    </div>
  );
}

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Registro />} />
        <Route path="/entrar" element={<Entrar />} />
        <Route path="/panel" element={<ConSesion>{(usuario) => <Panel usuario={usuario} />}</ConSesion>} />
        <Route
          path="/clases/:id"
          element={<ConSesion>{(usuario) => <Clase usuario={usuario} />}</ConSesion>}
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode } from "react";

// Componentes mínimos de la interfaz. Cuando se agregue shadcn/ui, estos se sustituyen por los suyos.

const ESTILO_DE_BOTON = {
  primario: "bg-blue-800 text-white hover:bg-blue-900",
  secundario: "bg-white text-slate-800 ring-1 ring-slate-300 hover:bg-slate-50",
  peligro: "bg-red-700 text-white hover:bg-red-800",
};

export function Boton({
  variante = "primario",
  className = "",
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variante?: keyof typeof ESTILO_DE_BOTON }) {
  return (
    <button
      className={`rounded-lg px-4 py-2.5 text-sm font-semibold transition focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 ${ESTILO_DE_BOTON[variante]} ${className}`}
      {...props}
    />
  );
}

export function Campo({
  etiqueta,
  ayuda,
  className = "",
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { etiqueta: string; ayuda?: string }) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium text-slate-700">{etiqueta}</span>
      <input
        className={`w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-base shadow-sm focus:border-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-200 ${className}`}
        {...props}
      />
      {ayuda && <span className="mt-1 block text-xs text-slate-500">{ayuda}</span>}
    </label>
  );
}

// sinRelleno es para tablas y listas, que llegan hasta el borde de la tarjeta
export function Tarjeta({
  children,
  className = "",
  sinRelleno = false,
}: {
  children: ReactNode;
  className?: string;
  sinRelleno?: boolean;
}) {
  return (
    <section
      className={`overflow-hidden rounded-xl bg-white shadow-sm ring-1 ring-slate-200 ${sinRelleno ? "" : "p-5"} ${className}`}
    >
      {children}
    </section>
  );
}

export function Aviso({ tipo, children }: { tipo: "error" | "exito"; children: ReactNode }) {
  const estilo =
    tipo === "error"
      ? "bg-red-50 text-red-800 ring-red-200"
      : "bg-emerald-50 text-emerald-800 ring-emerald-200";
  return (
    <p
      role={tipo === "error" ? "alert" : "status"}
      className={`rounded-lg px-3 py-2 text-sm ring-1 ${estilo}`}
    >
      {children}
    </p>
  );
}

export function Cargando() {
  return <p className="p-8 text-center text-slate-500">Cargando…</p>;
}

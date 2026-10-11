import { Campo } from "./ui";

export interface Equipo {
  etiqueta: string;
  propio: boolean;
}

export const SIN_EQUIPO: Equipo = { etiqueta: "", propio: false };

// Lo que la API espera: la etiqueta de una computadora del laboratorio, o que el alumno trae la suya
export function paraLaApi(equipo: Equipo) {
  return { equipo: equipo.propio ? null : equipo.etiqueta.trim(), equipoPropio: equipo.propio };
}

// Siempre hay que indicar una de las dos cosas: qué computadora se ocupa o que se trae equipo propio
export function CampoDeEquipo({ valor, alCambiar }: { valor: Equipo; alCambiar: (equipo: Equipo) => void }) {
  return (
    <div className="space-y-2">
      <Campo
        etiqueta="Computadora que ocupas"
        ayuda="La etiqueta que tiene la computadora, por ejemplo PC-12."
        value={valor.propio ? "" : valor.etiqueta}
        onChange={(e) => alCambiar({ ...valor, etiqueta: e.target.value })}
        autoComplete="off"
        maxLength={50}
        disabled={valor.propio}
        required={!valor.propio}
        className="disabled:bg-slate-100"
      />
      <label className="flex items-center gap-2 text-sm text-slate-700">
        <input
          type="checkbox"
          className="size-4 rounded border-slate-300"
          checked={valor.propio}
          onChange={(e) => alCambiar({ ...valor, propio: e.target.checked })}
        />
        Traigo mi propio equipo
      </label>
    </div>
  );
}

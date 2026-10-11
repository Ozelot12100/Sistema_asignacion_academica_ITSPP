// La API manda las fechas en UTC; aquí se muestran en la hora del dispositivo
export function hora(fecha: string | null | undefined): string {
  if (!fecha) return "—";
  return new Date(fecha).toLocaleTimeString("es-MX", { hour: "2-digit", minute: "2-digit" });
}

export function fechaYHora(fecha: string): string {
  return new Date(fecha).toLocaleString("es-MX", { dateStyle: "medium", timeStyle: "short" });
}

export function mensaje(error: unknown): string {
  return error instanceof Error ? error.message : "Ocurrió un error. Inténtalo de nuevo.";
}

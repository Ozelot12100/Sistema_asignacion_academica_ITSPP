import createClient from "openapi-fetch";
import type { components, paths } from "./esquema";

export type Esquemas = components["schemas"];

// Mismo origen que la página: la cookie de sesión viaja sola
export const api = createClient<paths>({ baseUrl: "/" });

export class ErrorDeApi extends Error {
  constructor(
    public readonly estado: number,
    mensaje: string,
  ) {
    super(mensaje);
  }
}

interface Problema {
  title?: string;
  errors?: Record<string, string[]>;
}

// La API responde los errores como "problem details"; de ahí sale el mensaje que ve la persona
export function mensajeDeError(estado: number, cuerpo: unknown): string {
  const problema = typeof cuerpo === "object" && cuerpo !== null ? (cuerpo as Problema) : {};
  // Un 401 con título propio es el de correo o contraseña incorrectos; el genérico es una sesión vencida
  if (estado === 401)
    return problema.title && problema.title !== "Unauthorized"
      ? problema.title
      : "Tu sesión terminó. Vuelve a entrar.";
  if (estado === 403) return "No tienes permiso para hacer esto.";
  if (estado === 429) return "Demasiados intentos. Espera un minuto y vuelve a intentar.";

  const deCampos = Object.values(problema.errors ?? {}).flat();
  if (deCampos.length > 0) return deCampos.join(" ");
  if (estado < 500 && problema.title) return problema.title;
  return "Ocurrió un error. Inténtalo de nuevo.";
}

interface Respuesta<T> {
  data?: T;
  error?: unknown;
  response: Response;
}

// Convierte la respuesta de openapi-fetch en el dato o en una excepción con un mensaje legible
export async function resultado<T>(llamada: Promise<Respuesta<T>>): Promise<T> {
  let respuesta: Respuesta<T>;
  try {
    respuesta = await llamada;
  } catch {
    throw new ErrorDeApi(0, "No se pudo conectar con el servidor. Revisa tu conexión.");
  }
  if (!respuesta.response.ok)
    throw new ErrorDeApi(
      respuesta.response.status,
      mensajeDeError(respuesta.response.status, respuesta.error),
    );
  return respuesta.data as T;
}

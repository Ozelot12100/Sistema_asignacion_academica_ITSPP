import { describe, expect, it } from "vitest";
import { mensajeDeError } from "./cliente";

describe("mensajeDeError", () => {
  it("usa el título que manda la API en los conflictos", () => {
    expect(mensajeDeError(409, { title: "Ese equipo ya está ocupado." })).toBe("Ese equipo ya está ocupado.");
  });

  it("junta los errores de cada campo en los datos inválidos", () => {
    const cuerpo = {
      title: "One or more validation errors occurred.",
      errors: { matricula: ["Es obligatorio."] },
    };
    expect(mensajeDeError(400, cuerpo)).toBe("Es obligatorio.");
  });

  it("no muestra los títulos en inglés de sesión, permiso y límite de intentos", () => {
    expect(mensajeDeError(401, { title: "Unauthorized" })).toBe("Tu sesión terminó. Vuelve a entrar.");
    expect(mensajeDeError(403, { title: "Forbidden" })).toBe("No tienes permiso para hacer esto.");
    expect(mensajeDeError(429, undefined)).toMatch(/Demasiados intentos/);
  });

  it("en el inicio de sesión muestra el motivo que da la API", () => {
    expect(mensajeDeError(401, { title: "Correo o contraseña incorrectos." })).toBe(
      "Correo o contraseña incorrectos.",
    );
  });

  it("no revela el detalle de un error interno", () => {
    expect(mensajeDeError(500, { title: "NullReferenceException" })).toBe(
      "Ocurrió un error. Inténtalo de nuevo.",
    );
  });
});

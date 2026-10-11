# Frontend

Aplicación web del sistema de asistencias, en React, TypeScript y Vite. Las herramientas elegidas y su justificación están en [docs/stack-tecnologico.md](../docs/stack-tecnologico.md).

## Qué hay hoy

| Ruta | Para quién | Qué hace |
|---|---|---|
| `/` | Alumno, sin sesión | Registra su entrada con su número de control, e indica la computadora que ocupa o que trae su propio equipo |
| `/entrar` | Personal | Inicio de sesión |
| `/panel` | Personal | Clases abiertas, cursos (con el botón para abrir clase) y clases anteriores. El docente ve solo lo suyo |
| `/clases/:id` | Personal | Lista del grupo en vivo, registrar a un alumno a mano, quitar una entrada y cerrar la clase |

Todavía no hay pantallas para administrar catálogos, alumnos, cursos ni usuarios: eso se hace por ahora desde la documentación interactiva de la API (`/scalar/v1`).

## Cómo ejecutarlo

Requisitos: Node.js 24 y la API corriendo en `http://localhost:5270` (ver el [README principal](../README.md)).

```
cd web
npm install
npm run dev
```

Queda en `http://localhost:5173`. Las llamadas a `/api` se reenvían a la API, así que la sesión funciona igual que en producción. También responde en la red local, para probarlo desde un teléfono.

## Comandos

| Comando | Qué hace |
|---|---|
| `npm run dev` | Servidor de desarrollo |
| `npm run build` | Revisa los tipos y compila a `dist/` |
| `npm run lint` | ESLint |
| `npm test` | Pruebas con Vitest |
| `npm run formato` | Aplica Prettier |
| `npm run generar-api` | Vuelve a generar `src/api/esquema.d.ts` a partir de la API en marcha |

## Tipos de la API

`src/api/esquema.d.ts` se genera con `npm run generar-api`; no se edita a mano. Hay que volver a generarlo cada vez que cambia una ruta o un campo en la API. Para que una respuesta salga con su tipo, la ruta en C# debe declararla con `.Produces<T>()` o devolver el objeto directamente.

## Pendiente respecto al stack acordado

- shadcn/ui: los componentes de `src/componentes/ui.tsx` son provisionales.
- React Hook Form y Zod: los formularios actuales son de uno o dos campos y no los necesitan aún.
- vite-plugin-pwa: para poder instalar la aplicación en el celular.
- Que la API sirva los archivos de `dist/` en producción (decisión D6).

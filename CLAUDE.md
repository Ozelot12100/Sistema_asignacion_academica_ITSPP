# Sistema de Control de Asistencias a Laboratorios — ITSPP

Guía de contexto y trabajo para asistentes de desarrollo (Claude Code / Antigravity) en este proyecto.

## 1. Contexto y Propósito

* **Proyecto real:** Sistema de control de acceso y asistencias a laboratorios de cómputo del Instituto Tecnológico Superior de Puerto Peñasco (ITSPP). Registra las entradas de los alumnos a clase y el equipo que ocupan, lleva las listas de asistencia, las prácticas por materia y una bitácora de auditoría. Están planeados, pero aún no construidos, el uso del laboratorio fuera de clase (solicitudes de ingreso), la entrada de docentes e invitados y los avisos al administrador.
* **Cómo se usa:** el docente abre la clase de su curso; cada alumno inscrito registra su entrada con su número de control (la matrícula, por ejemplo `23303080`), sin iniciar sesión, desde una computadora o su teléfono, e indica qué computadora ocupa o que trae su propio equipo; al cerrar la clase el docente, se registra la salida de todos.
* **Nombre del repositorio:** Se llama `Sistema_asignacion_academica_ITSPP` por el prototipo inicial de horarios con el que se recibió el repo. Ese código inicial se retiró y fue sustituido por la API actual de asistencias en `api/`, que ya está en `main`.
* **Equipo:** David Alberto Ramírez Vargas (usuario `Ozelot12100`) desarrolla la mayor parte del sistema, backend y frontend, y decide lo técnico. Andrés, Jorge y Tadeo apoyan. Andrés es el contacto con el personal del instituto: lleva las preguntas y trae las respuestas.

## 2. Stack Tecnológico

* **Backend:** .NET 9 (C# 13), ASP.NET Core Minimal APIs, EF Core 9 con Pomelo (`Pomelo.EntityFrameworkCore.MySql`), Scalar para documentación OpenAPI interactiva (`/scalar/v1`).
* **Base de datos:** MariaDB (11.8 en la integración continua). **No sirve MySQL:** la API usa `RETURNING`. Esquema relacional de 15 tablas en `db/esquema.sql` (utf8mb4, snake_case) y datos iniciales en `db/datos-iniciales.sql`. El esquema se define en SQL; EF Core solo lo lee, no hay migraciones de EF.
* **Pruebas:** xUnit en `api/Asistencias.Tests` (112 pruebas, contra una base MariaDB real que cada corrida crea y borra) y Vitest en `web/` (5 pruebas).
* **Frontend:** Carpeta `web/`, en React 19 + TypeScript 5.9 + Vite + Tailwind 4, con React Router, TanStack Query y tipos generados con openapi-typescript. Ver `web/README.md`.
* **Integración continua:** GitHub Actions (`.github/workflows/ci.yml`) compila y prueba la API y el frontend en cada cambio a `main` o a un pull request.

## 3. Comandos Frecuentes

```bash
# API
dotnet build api/
dotnet test api/
dotnet run --project api/Asistencias.Api     # http://localhost:5270, documentación en /scalar/v1

# Frontend (desde web/)
npm run dev          # http://localhost:5173, reenvía /api a la API
npm test
npm run lint
npm run build
npm run generar-api  # vuelve a generar src/api/esquema.d.ts; necesita la API corriendo
```

* Las pruebas de la API se conectan por defecto a `Server=localhost;Port=3306;Uid=root;Pwd=local;` (o al valor de la variable de entorno `ASISTENCIAS_TEST_DB`).
* Sin Docker sirve cualquier MariaDB instalado en la máquina; ver `db/README.md`.

## 4. Manejo de Configuración y Secretos

* **Nunca escribir contraseñas reales ni cadenas de conexión en archivos versionados (`appsettings.json`, etc.).**
* La configuración local se gestiona mediante `dotnet user-secrets` en `api/Asistencias.Api/`:
  ```bash
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=asistencias;Uid=root;Pwd=tu_password;"
  dotnet user-secrets set "AdminInicial:Email" "admin@itspp.edu.mx"
  dotnet user-secrets set "AdminInicial:Password" "tu_password_segura"
  ```
* En producción o servidores se usan variables de entorno (`ConnectionStrings__DefaultConnection`, `AdminInicial__Email`, `AdminInicial__Password`).

## 5. Dónde está cada cosa

Cada carpeta de `api/Asistencias.Api/` es un módulo. Las rutas van en `*Endpoints.cs`; las reglas de negocio, en clases aparte sin HTTP ni base de datos (`Reglas*.cs`, `ListaDeAsistencia.cs`), que se prueban solas.

| Carpeta | Contenido |
|---|---|
| `Autenticacion/` | Inicio de sesión, cookie, primer administrador, reglas de contraseña |
| `Usuarios/` | Cuentas del personal |
| `Laboratorios/` | Laboratorios, equipos y su estado |
| `Catalogos/` | Carreras, periodos y materias |
| `Alumnos/` | Alumnos y su carga en lote |
| `Cursos/` | Cursos, inscripciones y prácticas |
| `Clases/` | Abrir y cerrar clases, registro de entrada, listas de asistencia y correcciones |
| `Auditoria/` | Consulta de la bitácora |
| `Datos/` | Entidades y `AsistenciasDbContext`, que además llena la bitácora en cada guardado |
| `Comun/` | Validación, traducción de errores de la base a respuestas, políticas de permisos |

El índice de todas las rutas, con quién puede usar cada una, está en `docs/rutas-de-la-api.md`.

## 6. Forma de trabajo

* **Ramas:** David sube sus cambios directo a `main`, sin pull request. Los demás integrantes entran por pull request.
* **Antes de subir:** compilar sin advertencias y pasar `dotnet test api/`; si se tocó `web/`, también `npm test`, `npm run lint` y `npm run build`. Después de subir, revisar que la integración continua pase.
* **Commits:** en español, en infinitivo, describiendo el cambio y no el archivo (`Detectar traslapes parciales entre clases`).
* **Código:** vocabulario del dominio en español, igual que lo nombra el instituto. Los comentarios explican el porqué.
* **Cambios al esquema:** se editan en `db/esquema.sql`, junto con las entidades en C# y sus pruebas, y se anotan en `db/README.md`.
* **Cambios a una ruta o a un campo de la API:** incluyen volver a generar los tipos del frontend y actualizar `docs/rutas-de-la-api.md`. Para que la respuesta de una ruta salga con su tipo, debe devolver el objeto directamente o declarar `.Produces<T>()`.
* **Permisos y reglas nuevas:** llevan pruebas. Los permisos que no ha definido el instituto se deciden con el criterio de D18 (lo más restrictivo que deja operar) y se anotan como provisionales en `docs/decisiones.md`.
* **Documentación:** cada cambio actualiza lo que le toque de `docs/estado-actual.md`, `docs/decisiones.md` y este archivo. Lo que no se verificó se dice como no verificado.
* **Preguntas al instituto:** se agregan a `docs/preguntas-para-el-instituto.md`, y ahí mismo se anota la respuesta con su fecha cuando llega.

## 7. Estado Actual del Proyecto

* **Completado en la API:**
  * Esquema de base de datos y datos iniciales (`db/`).
  * Autenticación con cookies de sesión y roles (`admin`, `laboratorista`, `docente`); usuarios, cambio y restablecimiento de contraseña.
  * Laboratorios, equipos (libre, en uso, fuera de servicio con notas), carreras, periodos y materias.
  * Alumnos con búsqueda paginada y carga en lote (`POST /api/alumnos/lote`).
  * Cursos, inscripciones (una por una o con una lista de números de control) y prácticas por materia.
  * Clases: abrir, cerrar, registro de entrada del alumno sin sesión (`POST /api/registro/entrada`), registro a mano y quitar una entrada equivocada.
  * Lista de asistencia de una clase con faltas, y resumen de asistencia por curso.
  * Bitácora automática de altas, cambios y bajas, y su consulta (`GET /api/bitacora`, solo administrador).
* **Completado en el frontend:** registro de entrada del alumno (`/`), inicio de sesión (`/entrar`), panel del docente (`/panel`) y pantalla de la clase con la lista en vivo (`/clases/:id`).
* **Siguientes tareas por construir (en orden de prioridad):**
  1. Pantallas de administración en el frontend: alumnos (con carga desde archivo), cursos e inscripciones, catálogos, usuarios y bitácora. La API ya lo permite todo; hoy se hace desde `/scalar/v1`.
  2. Reportes: historial de un alumno, uso de cada laboratorio y de los equipos, y exportación a Excel y PDF.
  3. Que la API sirva el frontend compilado en producción (D6) y el despliegue.
  4. Completar el frontend según el stack acordado (shadcn/ui, PWA) y agregar pruebas del flujo en navegador.
* **En espera de respuestas del instituto** (`docs/preguntas-para-el-instituto.md`, entregadas a Andrés el 10 de octubre de 2026): dónde se aloja el sistema y, con ello, si el registro necesita un código de clase; uso del laboratorio fuera de clase y solicitudes de ingreso; permisos definitivos por rol; si las prácticas son por materia o por docente; de dónde sale la lista de alumnos.
* **Limitaciones conocidas:** el resumen por curso cuenta como faltas las clases anteriores a la inscripción del alumno; el registro de entrada no impide que alguien registre a un compañero con su número de control; nada se ha desplegado en un servidor.

## 8. Documentación Detallada

Toda la documentación está en la carpeta `docs/`:
* `docs/estado-actual.md`: Qué funciona, qué se verificó y qué falta.
* `docs/rutas-de-la-api.md`: Todas las rutas, quién puede usar cada una, y cómo preparar una clase desde cero.
* `docs/modelo-de-datos.md`: Modelo relacional y reglas de negocio.
* `docs/preguntas-para-el-instituto.md`: Preguntas pendientes para el personal del ITSPP; ahí se anotan las respuestas.
* `docs/decisiones.md`: Registro de decisiones (D1 a D22) y sus justificaciones.
* `docs/stack-tecnologico.md`: Justificación del stack elegido, qué está en uso y qué falta.
* `docs/metodologia-de-trabajo.md`: Responsables, convenciones, ramas y flujo de trabajo.

Además: `README.md` (cómo ejecutarlo), `db/README.md` (crear la base, con o sin Docker) y `web/README.md` (el frontend).

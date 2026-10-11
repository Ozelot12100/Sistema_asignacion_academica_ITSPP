# Sistema de Control de Asistencias a Laboratorios — ITSPP

Guía de contexto y trabajo para asistentes de desarrollo (Claude Code / Antigravity) en este proyecto.

## 1. Contexto y Propósito

* **Proyecto real:** Sistema de control de acceso y asistencias a laboratorios de cómputo del Instituto Tecnológico Superior de Puerto Peñasco (ITSPP). Registra entradas/salidas de alumnos, docentes e invitados, asignación de equipos de cómputo, solicitudes de ingreso, prácticas por materia y bitácora de auditoría.
* **Nombre del repositorio:** Se llama `Sistema_asignacion_academica_ITSPP` por el prototipo inicial de horarios con el que se recibió el repo. Ese código inicial se retiró y fue sustituido por la API actual de asistencias en `api/`, que ya está en `main`.
* **Equipo:** David Alberto Ramírez Vargas (backend, C#), Andrés, Jorge y Tadeo.

## 2. Stack Tecnológico

* **Backend:** .NET 9 (C# 13), ASP.NET Core Minimal APIs, EF Core 9 con Pomelo (`Pomelo.EntityFrameworkCore.MySql`), Scalar para documentación OpenAPI interactiva (`/scalar/v1`).
* **Base de datos:** MariaDB 11.8 / MySQL 9.4. Esquema relacional de 15 tablas en `db/esquema.sql` (utf8mb4, snake_case) y datos iniciales en `db/datos-iniciales.sql`.
* **Pruebas:** xUnit en `api/Asistencias.Tests` (112 pruebas automáticas que cubren autenticación, roles, catálogos, alumnos, cursos, inscripciones, prácticas, clases, registro de entrada, listas de asistencia y bitácora).
* **Frontend:** Carpeta `web/`, en React 19 + TypeScript + Vite + Tailwind, con React Router, TanStack Query y tipos generados con openapi-typescript (`npm run generar-api`). Ver `web/README.md`.

## 3. Comandos Frecuentes

```bash
# Compilar la solución
dotnet build api/

# Ejecutar las pruebas automáticas
dotnet test api/

# Ejecutar la API en desarrollo
dotnet run --project api/Asistencias.Api
```

* La documentación interactiva de la API queda disponible en `http://localhost:5270/scalar/v1`.
* Las pruebas de integración se conectan por defecto a `Server=localhost;Port=3306;Uid=root;Pwd=local;` (o al valor definido en la variable de entorno `ASISTENCIAS_TEST_DB`). El servidor debe ser MariaDB: la API usa `RETURNING`, que MySQL no soporta.

## 4. Manejo de Configuración y Secretos

* **Nunca escribir contraseñas reales ni cadenas de conexión en archivos versionados (`appsettings.json`, etc.).**
* La configuración local se gestiona mediante `dotnet user-secrets` en `api/Asistencias.Api/`:
  ```bash
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=asistencias;Uid=root;Pwd=tu_password;"
  dotnet user-secrets set "AdminInicial:Email" "admin@itspp.edu.mx"
  dotnet user-secrets set "AdminInicial:Password" "tu_password_segura"
  ```
* En producción o servidores se usan variables de entorno (`ConnectionStrings__DefaultConnection`, `AdminInicial__Email`, `AdminInicial__Password`).

## 5. Estado Actual del Proyecto

* **Completado:**
  * Esquema de base de datos relacional y datos iniciales (`db/`).
  * Autenticación con cookies de sesión y roles (`admin`, `laboratorista`, `docente`).
  * Gestión de usuarios, cambio y restablecimiento de contraseña.
  * Catálogos completos: laboratorios, equipos (libre, en uso, fuera de servicio con notas), carreras, periodos y materias.
  * Gestión de alumnos con búsqueda paginada por matrícula o nombre.
  * Cursos, inscripciones de alumnos y prácticas por materia (`api/Asistencias.Api/Cursos/`).
  * Registro de entrada por clase (`api/Asistencias.Api/Clases/`): el docente abre la clase, el alumno inscrito registra su entrada con su número de control sin iniciar sesión (`POST /api/registro/entrada`), y al cerrar la clase se registra la salida de todos.
  * Lista de asistencia de una clase con faltas, resumen de asistencia por curso, y correcciones (registrar a mano, quitar una entrada).
  * Carga de alumnos en lote (`POST /api/alumnos/lote`) e inscripción de una lista de números de control.
  * Bitácora de auditoría automática ante altas, bajas y modificaciones, y su consulta (`GET /api/bitacora`, solo administrador).
  * Frontend: registro de entrada del alumno, inicio de sesión, panel del docente y pantalla de la clase.
  * 112 pruebas automáticas pasando en la API y 5 en el frontend.
* **Siguientes tareas por construir (en orden de prioridad):**
  1. Pantallas de administración en el frontend: alumnos (con carga desde archivo), cursos e inscripciones, catálogos y usuarios.
  2. Reportes: historial de un alumno, uso de cada laboratorio y de los equipos, y exportación a Excel y PDF.
  3. Que la API sirva el frontend compilado en producción (D6) y el despliegue.
* **En espera de respuestas del instituto** (`docs/preguntas-para-el-instituto.md`): código de clase para el registro, uso del laboratorio fuera de clase y solicitudes de ingreso, permisos definitivos por rol.

## 6. Documentación Detallada

Toda la documentación arquitectónica está en la carpeta `docs/`:
* `docs/estado-actual.md`: Qué funciona y qué falta.
* `docs/modelo-de-datos.md`: Modelo relacional y reglas de negocio.
* `docs/preguntas-para-el-instituto.md`: Preguntas pendientes para el personal del ITSPP; ahí se anotan las respuestas.
* `docs/decisiones.md`: Registro de decisiones técnicas (ADR) y justificaciones.
* `docs/stack-tecnologico.md`: Justificación del stack elegido vs descartados.
* `docs/metodologia-de-trabajo.md`: Convenciones, ramas y flujo de trabajo.

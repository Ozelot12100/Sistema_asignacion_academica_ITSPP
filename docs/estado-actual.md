# Estado actual del proyecto

Qué hay, qué se ha hecho y qué falta. Este documento se actualiza cada vez que cambia algo de la lista.

Última actualización: 4 de octubre de 2026.

## Cambio de rumbo del 4 de octubre de 2026

El proyecto es el **sistema de control de asistencias a laboratorios**, no un sistema de horarios.

El repositorio se recibió con una API de horarios (profesores, aulas, grupos, clases). Al llegar la base de datos se vio que no tenía ninguna de esas tablas: es la base de un sistema de asistencias. El equipo confirmó que ese es el proyecto real.

Consecuencias:

- **El código de `api/` no corresponde al sistema.** Sus rutas y sus reglas de horario no se van a usar.
- **Sí se aprovecha la base del proyecto**: la estructura del repositorio, la configuración por secretos, el manejo de errores, la documentación automática de la API, el proyecto de pruebas y la integración continua.
- **Las decisiones de arquitectura, el stack y la metodología siguen vigentes.** No dependían del tema del sistema.

## Qué se ha hecho

| Trabajo | Estado |
|---|---|
| Limpieza y reorganización del repositorio (`api/`, `web/`, `db/`, `docs/`) | Hecho |
| Base del backend: configuración sin secretos en archivos, errores uniformes, OpenAPI, pruebas, integración continua | Hecho; reutilizable |
| Análisis de la base de datos recibida | Hecho; ver [modelo-de-datos.md](modelo-de-datos.md) |
| Esquema nuevo de la base de datos | Propuesto y probado; pendiente de revisión del equipo |
| Correcciones a la API de horarios (traslapes, disponibilidad, validación de usuarios) | Hecho, pero ya no aplica a este sistema |

## Qué se verificó y qué no

- **Verificado.** El esquema nuevo se cargó en MariaDB 11.8 (la misma versión del servidor actual) junto con los datos iniciales, y se probó que sus restricciones rechazan los casos inválidos: dos personas en un mismo equipo, un alumno dentro dos veces, un equipo de otro laboratorio, una salida anterior a la entrada y una solicitud resuelta sin fecha.
- **Sin verificar.** No existe todavía código de la API para asistencias, así que nada del sistema nuevo se ha ejecutado de punta a punta.

## Qué falta

### Por decidir

- **Qué hacer con el código de horarios en `api/`.** La recomendación es retirarlo y conservar solo la base del proyecto; queda en el historial de git por si se necesita.
- **Renombrar el proyecto y el repositorio**, que aún se llaman "Asignación Académica".
- **Aprobar el esquema** de la base de datos.

### Por conseguir

- El código del sistema que está en producción, que no está en este repositorio.
- Capturas de la interfaz actual.
- Confirmar si la base recibida es la de producción. Todos sus registros son del 3 de octubre de 2026 y no tiene asistencias ni alumnos.
- Las respuestas del instituto a las preguntas de [modelo-de-datos.md](modelo-de-datos.md).

### Por construir

En este orden:

1. API: modelo con EF Core sobre el esquema aprobado.
2. API: inicio de sesión con roles (administrador, laboratorista, docente).
3. API: catálogos (laboratorios, equipos, carreras, periodos, materias, alumnos).
4. API: registro de entrada y salida, y estado de los equipos.
5. API: solicitudes de ingreso, prácticas, avisos y reportes.
6. Frontend: iniciar el proyecto en `web/` y construir las pantallas.
7. Despliegue en el servidor de la universidad y respaldos.

Además: subir a .NET 10, que requiere instalar el SDK.

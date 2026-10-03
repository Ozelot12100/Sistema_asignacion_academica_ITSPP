# Estado actual del proyecto

Qué se recibió, qué se ha corregido y qué falta. Este documento se actualiza cada vez que cambia algo de la lista.

Última actualización: 3 de octubre de 2026.

## Qué se recibió

Una API REST en C# (.NET 9, Minimal API, Dapper, MySQL) de unas 500 líneas, con un solo commit. Tenía CRUD de usuarios, profesores, asignaturas, grupos, aulas y clases, más la disponibilidad por profesor. La única lógica de negocio era la validación de una clase al guardarla.

No venían en el repositorio: el esquema de la base de datos, el frontend, el inicio de sesión, pruebas ni documentación.

## Qué se ha hecho

| Cambio | Detalle |
|---|---|
| Limpieza del repositorio | Se dejaron de versionar `bin/` y `obj/` (53 archivos) y se agregó `.gitignore` |
| Reorganización | El código pasó a `api/`; se crearon `web/`, `db/` y `docs/`, una solución y un proyecto de pruebas |
| Configuración | La cadena de conexión se lee de secretos de usuario o variables de entorno, ya no de un archivo versionado. El puerto dejó de estar fijo en el código |
| CORS | Antes aceptaba cualquier origen; ahora solo los que se configuren |
| Errores | Los errores inesperados responden con un formato estándar |
| Documentación de la API | Se genera sola en `/openapi/v1.json` y `/scalar/v1` (solo en desarrollo) |
| Registro de usuarios | La expresión de validación de correo era inválida y hacía fallar toda alta de usuario; corregida |
| Edición de usuarios | No validaba nada; ahora valida igual que el alta |
| Lista de usuarios | Ya no devuelve las contraseñas |
| Traslapes | Solo detectaba clases con horario idéntico; ahora detecta cualquier traslape parcial |
| Disponibilidad del profesor | Exigía un bloque idéntico al de la clase; ahora acepta que la clase quepa dentro de uno o varios bloques contiguos |
| Jornada | Ignoraba los minutos (aceptaba terminar a las 20:59); ahora los considera |
| Datos incompletos | Una hora o un correo ausente provocaba un error 500; ahora responde 400 |
| Guardar disponibilidad | Ocultaba cualquier error y no comprobaba que el profesor existiera; corregido |
| Integración continua | Cada cambio se compila y se prueba automáticamente en GitHub |

## Qué se verificó y qué no

- **Verificado.** La solución compila sin advertencias. Las 29 pruebas de las reglas de horario pasan. La API arranca, publica su documentación y rechaza con 400 los datos inválidos.
- **Sin verificar.** Nada de lo que toca la base de datos se ha ejecutado, porque aún no se tiene la base. Esto incluye las consultas nuevas de traslape y disponibilidad: su lógica está probada, pero no su ejecución contra MySQL.

## Qué falta

### Bloqueado por la base de datos

- Confirmar que las tablas y columnas que el código supone existen en la base real.
- Decidir los cambios al esquema (decisión D8 en [decisiones.md](decisiones.md)).
- Adoptar EF Core y generar las migraciones.
- Contraseñas con hash e inicio de sesión con roles. **Hoy las contraseñas se guardan en texto plano y ninguna ruta pide autenticación; la API no debe exponerse en una red hasta resolver esto.**

### Sin bloqueo

- Subir a .NET 10 (requiere instalar el SDK).
- Que editar o borrar un registro inexistente responda 404; hoy responde "actualizado".
- Unificar el formato de las respuestas de validación.
- Validar `dia`, `turno`, `rol` y `tipo` contra listas de valores; hoy son texto libre.
- Iniciar el proyecto del frontend en `web/`.
- Preparar el despliegue y los respaldos.

## Preguntas abiertas para el instituto

Ya se enviaron a quien lleva el contacto. Las que más afectan al diseño:

- ¿Cuántos periodos hay al año y hay que conservar horarios pasados?
- ¿Cuántos semestres tiene cada carrera? El código limita el semestre a 1–8.
- ¿Cuánto dura una hora de clase y en qué horario hay clases? El código supone de 07:00 a 20:00.
- ¿Quién arma los horarios y quién solo consulta?
- ¿Los datos capturados en el sistema actual se tienen que conservar?
- ¿Qué sistema operativo tiene el servidor y quién da el acceso?

## Material por recibir

- La base de datos actual (estructura y datos).
- Capturas de la interfaz que está en producción.
- Un horario real de un semestre y las retículas de las carreras.

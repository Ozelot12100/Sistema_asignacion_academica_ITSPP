# Estado actual del proyecto

Qué funciona, qué se ha hecho y qué falta. Este documento se actualiza cada vez que cambia algo de la lista.

Última actualización: 4 de octubre de 2026.

## Antecedente

El repositorio se recibió con una API de horarios (profesores, aulas, grupos, clases). Al llegar la base de datos se vio que el proyecto real es el **control de asistencias a laboratorios**, y el equipo lo confirmó. El código de horarios se retiró el 4 de octubre de 2026; sigue disponible en el historial de git.

## Qué funciona hoy

| Parte | Detalle |
|---|---|
| Base de datos | Esquema de 14 tablas en [db/esquema.sql](../db/esquema.sql), con datos iniciales |
| Inicio de sesión | `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/yo`. Cookie de sesión, contraseñas con hash, tres roles |
| Primer administrador | Se crea al arrancar a partir de la configuración, solo si no existe ninguno |
| Laboratorios | `GET /api/laboratorios`: lista con el conteo de equipos en uso y fuera de servicio |
| Equipos | `GET /api/laboratorios/{id}/equipos`: cada equipo con su estado (libre, en uso, fuera de servicio) |
| Documentación de la API | `/openapi/v1.json` y `/scalar/v1`, solo en desarrollo |
| Integración continua | Compila y corre las pruebas contra MariaDB en cada cambio |

Medidas de seguridad ya incluidas:

- Todas las rutas piden sesión, salvo el inicio de sesión y `/api/salud`.
- El inicio de sesión acepta 10 intentos por minuto por dirección; después responde 429.
- La respuesta es la misma si el correo no existe o si la contraseña es incorrecta.
- Desactivar una cuenta o cambiarle el rol invalida su sesión abierta en la siguiente petición.

## Qué se verificó y qué no

- **Verificado.** 23 pruebas automáticas pasan contra MariaDB 11.8, la misma versión del servidor actual: inicio y cierre de sesión, rechazo de credenciales inválidas, invalidación de sesiones, y el estado de los equipos con asistencias abiertas y cerradas. También se probó a mano la API en marcha, incluido el límite de intentos.
- **Verificado en la base.** Las restricciones del esquema rechazan dos personas en un mismo equipo, un alumno dentro dos veces, un equipo de otro laboratorio, una salida anterior a la entrada y una solicitud resuelta sin fecha.
- **Sin verificar.** La integración continua aún no ha corrido en GitHub, porque la rama no se ha subido. Nada se ha desplegado en un servidor.

## Qué falta

### Por construir

En este orden:

1. Administración de usuarios (altas, bajas, cambio de contraseña) y permisos por rol.
2. Catálogos: laboratorios, equipos, carreras, periodos, materias, cursos y alumnos.
3. Registro de entrada y salida.
4. Solicitudes de ingreso, prácticas, avisos y bitácora.
5. Reportes.
6. Frontend: iniciar el proyecto en `web/` y construir las pantallas.
7. Despliegue en el servidor y respaldos.

Además: subir a .NET 10, que requiere instalar el SDK.

### Bloqueado por respuestas

- **Registro de entrada.** Depende de cómo se identifica el alumno (teclea su matrícula, escanea su credencial o lo registra el laboratorista).
- **Permisos por rol.** Depende de qué puede hacer cada quien según el instituto.
- **Carga de alumnos.** Depende de si se pueden importar del sistema escolar.

Las preguntas completas están en [modelo-de-datos.md](modelo-de-datos.md).

### Por decidir en el equipo

- Aprobar el esquema de la base de datos.
- Renombrar el repositorio en GitHub.
- Quién lleva el frontend.

### Por conseguir

- El código y capturas del sistema que está en producción.
- Confirmar si la base recibida es la de producción; todos sus registros son del 3 de octubre de 2026 y no tiene asistencias ni alumnos.

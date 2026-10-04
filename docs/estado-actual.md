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
| Cambio de contraseña | Cada quien cambia la suya si conoce la actual; el administrador restablece la de otros |
| Usuarios | Alta, edición, desactivación y listado. No se puede dejar al sistema sin un administrador activo |
| Laboratorios | Alta, edición, baja y listado con el conteo de equipos en uso y fuera de servicio |
| Equipos | Alta, edición, baja, y estado de cada uno (libre, en uso, fuera de servicio). Marcar un equipo fuera de servicio con una nota |
| Carreras, periodos y materias | Alta, edición, baja y listado |
| Alumnos | Alta, edición, baja, consulta y búsqueda paginada por matrícula o nombre |
| Bitácora | Cada alta, cambio o baja queda anotado automáticamente con su autor |
| Documentación de la API | `/openapi/v1.json` y `/scalar/v1`, solo en desarrollo |
| Integración continua | Compila y corre las pruebas contra MariaDB en cada cambio |

### Quién puede hacer qué

| Acción | Administrador | Laboratorista | Docente |
|---|---|---|---|
| Consultar laboratorios, equipos, catálogos y alumnos | Sí | Sí | Sí |
| Marcar un equipo fuera de servicio | Sí | Sí | No |
| Dar de alta y editar alumnos | Sí | Sí | No |
| Borrar alumnos | Sí | No | No |
| Modificar laboratorios, equipos, carreras, periodos y materias | Sí | No | No |
| Gestionar usuarios | Sí | No | No |

Este reparto es una suposición razonable, no algo que haya definido el instituto. Ver la decisión D18 en [decisiones.md](decisiones.md).

### Reglas que conviene conocer

- Lo que tiene historial no se borra, se desactiva: la API responde 409 si se intenta borrar un alumno, equipo, laboratorio o carrera del que dependen otros registros.
- Los datos inválidos responden 400 indicando cada campo con error; los duplicados (correo, matrícula, clave, etiqueta) responden 409.
- Las contraseñas deben tener al menos 12 caracteres.

Medidas de seguridad ya incluidas:

- Todas las rutas piden sesión, salvo el inicio de sesión y `/api/salud`.
- El inicio de sesión acepta 10 intentos por minuto por dirección; después responde 429.
- La respuesta es la misma si el correo no existe o si la contraseña es incorrecta.
- Desactivar una cuenta o cambiarle el rol invalida su sesión abierta en la siguiente petición.

## Qué se verificó y qué no

- **Verificado.** 52 pruebas automáticas pasan contra MariaDB 11.8, la misma versión del servidor actual. Cubren inicio y cierre de sesión, invalidación de sesiones, permisos de cada rol, validación de datos, duplicados, borrados bloqueados, búsqueda de alumnos, estado de los equipos y la bitácora. El inicio de sesión y el límite de intentos también se probaron a mano con la API en marcha.
- **Verificado en la base.** Las restricciones del esquema rechazan dos personas en un mismo equipo, un alumno dentro dos veces, un equipo de otro laboratorio, una salida anterior a la entrada y una solicitud resuelta sin fecha.
- **Sin verificar.** La integración continua aún no ha corrido en GitHub, porque la rama no se ha subido. Nada se ha desplegado en un servidor.

## Qué falta

### Por construir

En este orden:

1. Cursos, inscripciones y prácticas.
2. Registro de entrada y salida.
3. Solicitudes de ingreso y avisos.
4. Consulta de la bitácora y reportes.
5. Carga masiva de alumnos desde un archivo.
6. Frontend: iniciar el proyecto en `web/` y construir las pantallas.
7. Despliegue en el servidor y respaldos.

Además: subir a .NET 10, que requiere instalar el SDK.

### Bloqueado por respuestas

- **Registro de entrada.** Depende de cómo se identifica el alumno (teclea su matrícula, escanea su credencial o lo registra el laboratorista).
- **Permisos por rol.** Hay un reparto provisional; el definitivo depende de qué puede hacer cada quien según el instituto.
- **Carga de alumnos.** Depende de si se pueden importar del sistema escolar.

Las preguntas completas están en [modelo-de-datos.md](modelo-de-datos.md).

### Por decidir en el equipo

- Aprobar el esquema de la base de datos.
- Renombrar el repositorio en GitHub.
- Quién lleva el frontend.

### Por conseguir

- El código y capturas del sistema que está en producción.
- Confirmar si la base recibida es la de producción; todos sus registros son del 3 de octubre de 2026 y no tiene asistencias ni alumnos.

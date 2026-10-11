# Estado actual del proyecto

Qué funciona, qué se ha hecho y qué falta. Este documento se actualiza cada vez que cambia algo de la lista.

Última actualización: 10 de octubre de 2026.

## Antecedente

El repositorio se recibió con una API de horarios (profesores, aulas, grupos, clases). Al llegar la base de datos se vio que el proyecto real es el **control de asistencias a laboratorios**, y el equipo lo confirmó. El código de horarios se retiró el 4 de octubre de 2026; sigue disponible en el historial de git.

## Qué funciona hoy

| Parte | Detalle |
|---|---|
| Base de datos | Esquema de 15 tablas en [db/esquema.sql](../db/esquema.sql), con datos iniciales |
| Inicio de sesión | `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/yo`. Cookie de sesión, contraseñas con hash, tres roles |
| Primer administrador | Se crea al arrancar a partir de la configuración, solo si no existe ninguno |
| Cambio de contraseña | Cada quien cambia la suya si conoce la actual; el administrador restablece la de otros |
| Usuarios | Alta, edición, desactivación y listado. No se puede dejar al sistema sin un administrador activo |
| Laboratorios | Alta, edición, baja y listado con el conteo de equipos en uso y fuera de servicio |
| Equipos | Alta, edición, baja, y estado de cada uno (libre, en uso, fuera de servicio). Marcar un equipo fuera de servicio con una nota |
| Carreras, periodos y materias | Alta, edición, baja y listado |
| Alumnos | Alta, edición, baja, consulta y búsqueda paginada por matrícula o nombre |
| Cursos | Alta, edición, baja, consulta y listado por periodo, materia, docente o laboratorio, con el número de inscritos |
| Inscripciones | Inscribir y dar de baja alumnos de un curso, lista de un curso y cursos de un alumno |
| Prácticas | Alta, edición, baja y listado de las prácticas de cada materia, ordenadas por número |
| Clases | El docente abre la clase de su curso en un laboratorio y la cierra; listado de clases y de quién asistió a cada una |
| Registro de entrada | `POST /api/registro/entrada`, sin sesión: el alumno inscrito registra su entrada con su número de control mientras la clase está abierta, y puede indicar su equipo. Al cerrar la clase se registra la salida de todos |
| Bitácora | Cada alta, cambio o baja queda anotado automáticamente con su autor |
| Documentación de la API | `/openapi/v1.json` y `/scalar/v1`, solo en desarrollo |
| Integración continua | Compila y corre las pruebas contra MariaDB en cada cambio |

### Quién puede hacer qué

| Acción | Administrador | Laboratorista | Docente |
|---|---|---|---|
| Consultar laboratorios, equipos, catálogos, alumnos, cursos y prácticas | Sí | Sí | Sí |
| Marcar un equipo fuera de servicio | Sí | Sí | No |
| Dar de alta y editar alumnos | Sí | Sí | No |
| Borrar alumnos | Sí | No | No |
| Modificar laboratorios, equipos, carreras, periodos y materias | Sí | No | No |
| Crear, editar y borrar cursos | Sí | No | No |
| Inscribir y dar de baja alumnos de un curso | Sí | Sí | Solo en los cursos que imparte |
| Crear, editar y borrar prácticas | Sí | No | Solo de las materias que imparte |
| Abrir una clase | Sí | No | Solo de los cursos que imparte |
| Cerrar una clase | Sí | Sí | Solo de los cursos que imparte |
| Gestionar usuarios | Sí | No | No |

Los alumnos no tienen cuenta: lo único que hacen es registrar su entrada con su número de control.

Este reparto es una suposición razonable, no algo que haya definido el instituto. Ver las decisiones D18, D21 y D22 en [decisiones.md](decisiones.md).

### Reglas que conviene conocer

- Lo que tiene historial no se borra, se desactiva: la API responde 409 si se intenta borrar un alumno, equipo, laboratorio o carrera del que dependen otros registros.
- Los datos inválidos responden 400 indicando cada campo con error; los duplicados (correo, matrícula, clave, etiqueta) responden 409.
- Las contraseñas deben tener al menos 12 caracteres.
- Un curso solo se asigna a un docente con la cuenta activa. Si la cuenta se desactiva después, el curso lo conserva.
- No hay dos cursos de la misma materia, periodo y grupo, ni dos prácticas con el mismo número en una materia.
- Inscribir a un alumno ya inscrito no es un error. A un alumno desactivado no se le inscribe (409).
- Borrar un curso borra sus inscripciones. Un curso o una práctica con asistencias no se puede borrar.
- Un laboratorio tiene a lo más una clase abierta, y un curso también.
- Solo registra su entrada un alumno activo, inscrito en un curso con la clase abierta, y que no esté ya dentro. Si indica un equipo, debe ser del laboratorio de la clase, estar libre y en servicio.

Medidas de seguridad ya incluidas:

- Todas las rutas piden sesión, salvo el inicio de sesión, `/api/salud` y el registro de entrada del alumno.
- El registro de entrada acepta 120 intentos por minuto por dirección; después responde 429.
- El inicio de sesión acepta 10 intentos por minuto por dirección; después responde 429.
- La respuesta es la misma si el correo no existe o si la contraseña es incorrecta.
- Desactivar una cuenta o cambiarle el rol invalida su sesión abierta en la siguiente petición.

## Qué se verificó y qué no

- **Verificado.** 94 pruebas automáticas pasan. Cubren inicio y cierre de sesión, invalidación de sesiones, permisos de cada rol, validación de datos, duplicados, borrados bloqueados, búsqueda de alumnos, estado de los equipos, cursos, inscripciones, prácticas, clases, registro de entrada y la bitácora. El inicio de sesión, el límite de intentos y el flujo de cursos, inscripciones y prácticas también se probaron a mano con la API en marcha.
- **Versión de la base en las pruebas.** La integración continua las corre contra MariaDB 11.8, la misma versión del servidor actual. En la máquina de desarrollo se corren contra MariaDB 11.5.
- **Sin probar.** El límite de intentos del registro de entrada no tiene prueba automática. Un laboratorio completo que comparta una sola dirección de salida a internet podría alcanzarlo si el sistema se aloja fuera del instituto.
- **Verificado en la base.** Las restricciones del esquema rechazan dos personas en un mismo equipo, un alumno dentro dos veces, un equipo de otro laboratorio, una salida anterior a la entrada y una solicitud resuelta sin fecha.
- **Sin verificar.** Nada se ha desplegado en un servidor.

## Qué falta

### Por construir

En este orden:

1. Reportes de asistencia (por clase, curso, alumno y laboratorio, incluidas las faltas) y consulta de la bitácora.
2. Uso del laboratorio fuera de clase: solicitudes de ingreso, avisos, y entrada de docentes e invitados.
3. Corregir un registro: que el docente pueda quitar una entrada equivocada o registrar a un alumno a mano.
4. Carga masiva de alumnos desde un archivo, incluida la inscripción de una lista completa a un curso.
5. Frontend: iniciar el proyecto en `web/` y construir las pantallas.
6. Despliegue en el servidor y respaldos.

Además: subir a .NET 10, que requiere instalar el SDK.

### Bloqueado por respuestas

- **Uso fuera de clase.** Depende de quién autoriza a un alumno a usar un laboratorio cuando no tiene clase.
- **Alojamiento.** Se dijo a la vez que el servidor será local y que se instalará en Hostinger. De eso depende si hace falta proteger el registro de entrada con un código de clase.
- **Permisos por rol.** Hay un reparto provisional; el definitivo depende de qué puede hacer cada quien según el instituto.
- **Carga de alumnos.** Depende de si se pueden importar del sistema escolar.
- **Prácticas.** Hoy pertenecen a la materia. Si cada docente define las suyas, hay que moverlas al curso.

Las preguntas completas están en [modelo-de-datos.md](modelo-de-datos.md).

### Por decidir en el equipo

- Aprobar el esquema de la base de datos.
- Renombrar el repositorio en GitHub.
- Quién lleva el frontend.

### Por conseguir

- El código y capturas del sistema que está en producción.
- Confirmar si la base recibida es la de producción; todos sus registros son del 3 de octubre de 2026 y no tiene asistencias ni alumnos.

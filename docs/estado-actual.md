# Estado actual del proyecto

Qué funciona, qué se ha hecho y qué falta. Este documento se actualiza cada vez que cambia algo de la lista.

Última actualización: 11 de octubre de 2026.

El índice de rutas de la API, con quién puede usar cada una, está en [rutas-de-la-api.md](rutas-de-la-api.md).

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
| Alumnos | Alta, edición, baja, consulta y búsqueda paginada por matrícula o nombre. Carga de una lista completa de una vez, que crea a los nuevos y actualiza a los existentes |
| Cursos | Alta, edición, baja, consulta y listado por periodo, materia, docente o laboratorio, con el número de inscritos |
| Inscripciones | Inscribir y dar de baja alumnos de un curso, uno por uno o con una lista de números de control; lista de un curso y cursos de un alumno |
| Prácticas | Alta, edición, baja y listado de las prácticas de cada materia, ordenadas por número |
| Clases | El docente abre la clase de su curso en un laboratorio y la cierra; listado de clases y de quién asistió a cada una |
| Registro de entrada | `POST /api/registro/entrada`, sin sesión: el alumno inscrito registra su entrada con su número de control mientras la clase está abierta, e indica la computadora que ocupa o que trae su propio equipo. Al cerrar la clase se registra la salida de todos |
| Lista de asistencia | De cada clase: quién asistió, con qué equipo y a qué hora, y quién faltó. De cada curso: asistencias, faltas y porcentaje de cada alumno |
| Correcciones | Quien lleva la clase registra a mano a un alumno que no pudo hacerlo y quita una entrada registrada por error |
| Bitácora | Cada alta, cambio o baja queda anotado automáticamente con su autor, incluido el borrado de una asistencia. El administrador la consulta filtrada por usuario, acción, registro y fechas |
| Aplicación web | Registro de entrada del alumno, inicio de sesión, panel del docente y pantalla de la clase con la lista en vivo. Ver [web/README.md](../web/README.md) |
| Documentación de la API | `/openapi/v1.json` y `/scalar/v1`, solo en desarrollo |
| Integración continua | Compila y corre las pruebas de la API contra MariaDB, y revisa, prueba y compila la aplicación web, en cada cambio |

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
| Cerrar una clase, registrar a mano y quitar entradas | Sí | Sí | Solo de los cursos que imparte |
| Cargar una lista de alumnos | Sí | Sí | No |
| Consultar la bitácora | Sí | No | No |
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
- Solo registra su entrada un alumno activo, inscrito en un curso con la clase abierta, y que no esté ya dentro.
- Al registrarse hay que indicar la computadora que se ocupa o que se trae equipo propio; no se acepta ninguna de las dos ni las dos. La computadora debe ser del laboratorio de la clase, estar libre y en servicio.

Medidas de seguridad ya incluidas:

- Todas las rutas piden sesión, salvo el inicio de sesión, `/api/salud` y el registro de entrada del alumno.
- El registro de entrada acepta 120 intentos por minuto por dirección; después responde 429.
- El inicio de sesión acepta 10 intentos por minuto por dirección; después responde 429.
- La respuesta es la misma si el correo no existe o si la contraseña es incorrecta.
- Desactivar una cuenta o cambiarle el rol invalida su sesión abierta en la siguiente petición.

## Qué se verificó y qué no

- **Verificado.** 112 pruebas automáticas de la API pasan. Cubren inicio y cierre de sesión, invalidación de sesiones, permisos de cada rol, validación de datos, duplicados, borrados bloqueados, búsqueda de alumnos, estado de los equipos, cursos, inscripciones, prácticas, clases, registro de entrada, listas de asistencia, correcciones, cargas en lote y la bitácora.
- **Verificado en un navegador.** El 10 de octubre de 2026 se recorrió el flujo completo en la aplicación web, con la API y la base reales: el docente entra y abre su clase, el alumno se registra desde una pantalla de teléfono, la lista del docente se actualiza sola, el docente registra a mano y quita una entrada, y cierra la clase. También los rechazos: contraseña incorrecta, número de control inexistente, doble registro y equipo ocupado. Esa prueba se hizo con un guion suelto; todavía no forma parte de las pruebas automáticas.
- **Limitación conocida.** El resumen por curso cuenta como falta toda clase a la que el alumno no entró, incluidas las anteriores a su inscripción, porque las inscripciones no guardan fecha.
- **Versión de la base en las pruebas.** La integración continua las corre contra MariaDB 11.8, la misma versión del servidor actual. En la máquina de desarrollo se corren contra MariaDB 11.5.
- **Sin probar.** El límite de intentos del registro de entrada no tiene prueba automática. Un laboratorio completo que comparta una sola dirección de salida a internet podría alcanzarlo si el sistema se aloja fuera del instituto.
- **Verificado en la base.** Las restricciones del esquema rechazan dos personas en un mismo equipo, un alumno dentro dos veces, un equipo de otro laboratorio, una salida anterior a la entrada y una solicitud resuelta sin fecha.
- **Sin verificar.** Nada se ha desplegado en un servidor. Nadie más del equipo ha levantado el proyecto en su máquina siguiendo el README. La aplicación web no se ha probado en un teléfono real, solo en un navegador de escritorio con el tamaño de pantalla de un teléfono.

## Qué falta

### Por construir

En este orden:

1. Pantallas de administración en la aplicación web: alumnos (con carga desde un archivo), cursos e inscripciones, catálogos, usuarios y bitácora. La API ya lo permite todo; hoy se hace desde `/scalar/v1`.
2. Reportes que faltan: historial de un alumno, uso de cada laboratorio y de los equipos, y exportar a Excel y PDF.
3. Uso del laboratorio fuera de clase: solicitudes de ingreso, avisos, y entrada de docentes e invitados.
4. Que la API sirva la aplicación web compilada, despliegue en el servidor y respaldos.
5. Completar el frontend según el stack acordado: shadcn/ui, instalación en el celular (PWA) y pruebas automáticas del flujo en navegador.

Además:

- Subir a .NET 10, que requiere instalar el SDK. .NET 9 pierde soporte en noviembre de 2026.
- Guardar la fecha de cada inscripción, para que el resumen por curso no cuente como faltas las clases anteriores a ella.
- Datos de demostración que se puedan cargar con un comando. Hoy cada quien los crea a mano siguiendo [rutas-de-la-api.md](rutas-de-la-api.md).

### Bloqueado por respuestas

- **Uso fuera de clase.** Depende de quién autoriza a un alumno a usar un laboratorio cuando no tiene clase. Hoy el laboratorista solo pregunta si tienen clase y los deja quedarse.
- **Alojamiento.** Se dijo a la vez que el servidor será local y que se instalará en Hostinger. De eso depende si hace falta proteger el registro de entrada con un código de clase.
- **Permisos por rol.** Hay un reparto provisional; el definitivo depende de qué puede hacer cada quien según el instituto.
- **Carga de alumnos.** Depende de si se pueden importar del sistema escolar.
- **Prácticas.** Hoy pertenecen a la materia. Si cada docente define las suyas, hay que moverlas al curso.

Las preguntas completas están en [preguntas-para-el-instituto.md](preguntas-para-el-instituto.md). Se le entregaron a Andrés el 10 de octubre de 2026, en Word y PDF, para que las lleve al personal; aún no hay respuestas.

### Por decidir en el equipo

- Aprobar el esquema de la base de datos.
- Renombrar el repositorio en GitHub.
- Quién lleva el despliegue y el servidor.

### Por conseguir

- El código del sistema que está en producción. No está en este repositorio.
- Capturas de sus pantallas. Solo se tiene acceso a la vista de alumnos; David quedó de pasar las de esa vista.
- Confirmar si la base recibida es la de producción; todos sus registros son del 3 de octubre de 2026 y no tiene asistencias ni alumnos.

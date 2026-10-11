# Rutas de la API

Resumen de todas las rutas, quién puede usar cada una y en qué carpeta del código está. Sirve para ubicarse; el detalle de cada campo está en la documentación interactiva (`http://localhost:5270/scalar/v1`, con la API corriendo en desarrollo), que se genera sola y nunca queda desactualizada.

Si se agrega o cambia una ruta, se actualiza esta tabla en el mismo cambio.

Última actualización: 11 de octubre de 2026.

## Cómo leer la columna "Quién"

| Valor | Significa |
|---|---|
| Nadie (pública) | No pide sesión |
| Sesión | Cualquier usuario con sesión: administrador, laboratorista o docente |
| Personal | Administrador o laboratorista |
| Admin | Solo el administrador |
| Admin o su docente | El administrador, o el docente que imparte ese curso |
| Personal o su docente | El administrador, el laboratorista, o el docente que imparte ese curso |
| Quien lleva la clase | Los mismos: el administrador, el laboratorista, o el docente que imparte ese curso |

Sin sesión la API responde 401; con sesión pero sin permiso, 403.

## Sistema y sesión — `Autenticacion/`

| Ruta | Quién | Qué hace |
|---|---|---|
| `GET /api/salud` | Nadie (pública) | Responde que la API está viva |
| `POST /api/auth/login` | Nadie (pública) | Inicia sesión con correo y contraseña. 10 intentos por minuto por dirección |
| `POST /api/auth/logout` | Sesión | Cierra la sesión |
| `GET /api/auth/yo` | Sesión | Datos del usuario de la sesión |
| `PUT /api/auth/password` | Sesión | Cambia la contraseña propia, dando la actual |

## Usuarios — `Usuarios/`

| Ruta | Quién | Qué hace |
|---|---|---|
| `GET /api/usuarios` | Admin | Lista, filtrable por rol y por activo |
| `POST /api/usuarios` | Admin | Crea una cuenta |
| `PUT /api/usuarios/{id}` | Admin | Edita o desactiva una cuenta. No cambia la contraseña |
| `PUT /api/usuarios/{id}/password` | Admin | Restablece la contraseña de otro |

Las cuentas no se borran: se desactivan.

## Laboratorios y equipos — `Laboratorios/`

| Ruta | Quién | Qué hace |
|---|---|---|
| `GET /api/laboratorios` | Sesión | Lista con el conteo de equipos en uso y fuera de servicio |
| `POST /api/laboratorios` | Admin | Crea |
| `PUT /api/laboratorios/{id}` | Admin | Edita o desactiva |
| `DELETE /api/laboratorios/{id}` | Admin | Borra, si nada depende de él |
| `GET /api/laboratorios/{id}/equipos` | Sesión | Equipos del laboratorio con su estado: libre, en uso o fuera de servicio |
| `POST /api/laboratorios/{id}/equipos` | Admin | Agrega un equipo |
| `PUT /api/equipos/{id}` | Admin | Edita un equipo |
| `PUT /api/equipos/{id}/servicio` | Personal | Marca un equipo fuera de servicio (o de vuelta en servicio) con una nota |
| `DELETE /api/equipos/{id}` | Admin | Borra, si no tiene asistencias |

## Catálogos — `Catalogos/`

Carreras (`/api/carreras`), periodos (`/api/periodos`) y materias (`/api/materias`) tienen las mismas cuatro rutas:

| Ruta | Quién | Qué hace |
|---|---|---|
| `GET /api/{catálogo}` | Sesión | Lista. Las materias se filtran por carrera |
| `POST /api/{catálogo}` | Admin | Crea |
| `PUT /api/{catálogo}/{id}` | Admin | Edita |
| `DELETE /api/{catálogo}/{id}` | Admin | Borra, si nada depende de él |

## Alumnos — `Alumnos/`

| Ruta | Quién | Qué hace |
|---|---|---|
| `GET /api/alumnos` | Sesión | Búsqueda paginada por matrícula o nombre, filtrable por carrera y por activo |
| `GET /api/alumnos/{id}` | Sesión | Un alumno |
| `POST /api/alumnos` | Personal | Da de alta |
| `POST /api/alumnos/lote` | Personal | Carga una lista de hasta 2000: crea a los nuevos y actualiza a los existentes según la matrícula. Si un renglón tiene error, no guarda ninguno |
| `PUT /api/alumnos/{id}` | Personal | Edita o desactiva |
| `DELETE /api/alumnos/{id}` | Admin | Borra, si no tiene asistencias |
| `GET /api/alumnos/{id}/cursos` | Sesión | Cursos en los que está inscrito (la ruta vive en `Cursos/`) |

## Cursos, inscripciones y prácticas — `Cursos/`

| Ruta | Quién | Qué hace |
|---|---|---|
| `GET /api/cursos` | Sesión | Lista, filtrable por periodo, materia, docente y laboratorio |
| `GET /api/cursos/{id}` | Sesión | Un curso |
| `POST /api/cursos` | Admin | Crea |
| `PUT /api/cursos/{id}` | Admin | Edita |
| `DELETE /api/cursos/{id}` | Admin | Borra el curso y sus inscripciones, si no tiene asistencias |
| `GET /api/cursos/{id}/alumnos` | Sesión | Alumnos inscritos |
| `PUT /api/cursos/{id}/alumnos/{alumnoId}` | Personal o su docente | Inscribe a un alumno. Repetirlo no es un error |
| `POST /api/cursos/{id}/alumnos` | Personal o su docente | Inscribe una lista de hasta 500 números de control y dice cuáles no pudo y por qué |
| `DELETE /api/cursos/{id}/alumnos/{alumnoId}` | Personal o su docente | Da de baja a un alumno del curso |
| `GET /api/cursos/{id}/asistencia` | Sesión | Asistencias, faltas y porcentaje de cada alumno inscrito (la ruta vive en `Clases/`) |
| `GET /api/materias/{id}/practicas` | Sesión | Prácticas de la materia, por número |
| `POST /api/materias/{id}/practicas` | Admin, o un docente que imparta la materia | Crea una práctica |
| `PUT /api/practicas/{id}` | Admin, o un docente que imparta la materia | Edita |
| `DELETE /api/practicas/{id}` | Admin, o un docente que imparta la materia | Borra, si no tiene asistencias |

## Clases y registro de entrada — `Clases/`

| Ruta | Quién | Qué hace |
|---|---|---|
| `GET /api/clases` | Sesión | Las 100 más recientes, filtrables por abiertas o cerradas, curso, laboratorio y docente |
| `GET /api/clases/{id}` | Sesión | Una clase |
| `POST /api/clases` | Admin o su docente | Abre la clase de un curso. Si no se indica laboratorio, usa el del curso |
| `POST /api/clases/{id}/cerrar` | Quien lleva la clase | Cierra la clase y registra la salida de todos los que seguían dentro |
| `GET /api/clases/{id}/asistencias` | Sesión | Solo quienes registraron entrada |
| `GET /api/clases/{id}/lista` | Sesión | Todos los inscritos, con quién asistió y quién faltó, y los totales |
| `POST /api/clases/{id}/asistencias` | Quien lleva la clase | Registra a mano a un alumno inscrito, mientras la clase está abierta |
| `DELETE /api/clases/{id}/asistencias/{asistenciaId}` | Quien lleva la clase | Quita una entrada registrada por error. Queda en la bitácora |
| `POST /api/registro/entrada` | Nadie (pública) | El alumno registra su entrada con su número de control. 120 intentos por minuto por dirección |

En los dos registros de entrada hay que mandar `equipo` (la etiqueta de la computadora) o `equipoPropio: true`; ni ninguno ni los dos.

## Bitácora — `Auditoria/`

| Ruta | Quién | Qué hace |
|---|---|---|
| `GET /api/bitacora` | Admin | Consulta paginada, filtrable por usuario, acción, tabla, registro y fechas. Lo más reciente primero |

## Códigos de respuesta

| Código | Cuándo |
|---|---|
| 200, 201, 204 | Todo bien: con datos, creado, o sin nada que devolver |
| 400 | Datos inválidos. El cuerpo trae `errors` con el mensaje de cada campo |
| 401 | Sin sesión, o correo y contraseña incorrectos |
| 403 | Con sesión, pero sin permiso |
| 404 | El registro no existe |
| 409 | Conflicto: duplicado, equipo ocupado, clase ya cerrada, alumno desactivado, o un borrado del que dependen otros registros |
| 429 | Demasiados intentos en un minuto |

Los errores salen en formato *problem details*; el campo `title` trae el mensaje en español que la aplicación web le muestra a la persona.

## Preparar una clase desde cero

Mientras no existan las pantallas de administración, esto se hace desde `/scalar/v1` con la sesión del administrador. En este orden:

1. Crear la cuenta del docente: `POST /api/usuarios` con `rol: "docente"`.
2. Crear la materia y el periodo: `POST /api/materias`, `POST /api/periodos`.
3. Crear el curso, con su docente y su laboratorio: `POST /api/cursos`.
4. Cargar a los alumnos: `POST /api/alumnos/lote`.
5. Inscribirlos al curso: `POST /api/cursos/{id}/alumnos` con la lista de números de control.

A partir de ahí el docente entra a la aplicación web, abre su clase desde el panel, y los alumnos se registran en la página principal.

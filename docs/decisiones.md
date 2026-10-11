# Decisiones del proyecto

Registro de las decisiones que dan forma al sistema. Cada una dice qué se decidió, por qué y qué se descartó. Si una decisión cambia, no se borra: se marca como reemplazada y se agrega la nueva.

Las decisiones sobre herramientas concretas están en [stack-tecnologico.md](stack-tecnologico.md).

Última actualización: 10 de octubre de 2026.

## Resumen

| N.º | Decisión | Estado |
|---|---|---|
| D1 | Continuar la API de horarios existente | Reemplazada por D12 |
| D2 | Un solo servidor, una API y una base de datos | Vigente |
| D3 | Un solo repositorio | Vigente |
| D4 | El frontend se hace de nuevo | Vigente |
| D5 | Aplicación web adaptable, no nativa | Vigente |
| D6 | El backend entrega también el frontend | Vigente |
| D7 | Autenticación con cookie de sesión y roles | Vigente, actualizada |
| D8 | Partir de la base existente y revisarla | Reemplazada por D13 |
| D9 | Reglas de horario separadas de rutas y base | Reemplazada por D15 |
| D10 | Vocabulario del dominio en español | Vigente, actualizada |
| D11 | Despliegue con Docker Compose | Provisional |
| D12 | El proyecto es el control de asistencias a laboratorios | Vigente |
| D13 | Esquema de base de datos rediseñado | Propuesta |
| D14 | Se conserva MariaDB | Vigente |
| D15 | Las reglas de negocio viven separadas de las rutas | Vigente |
| D16 | El esquema se define en SQL; EF Core solo lo lee | Vigente |
| D17 | Las pruebas corren contra una base MariaDB real | Vigente |
| D18 | Reparto provisional de permisos por rol | Provisional |
| D19 | La bitácora se llena sola | Vigente |
| D20 | Lo que tiene historial se desactiva, no se borra | Vigente |
| D21 | El docente lleva la lista de sus cursos y las prácticas de sus materias | Provisional |
| D22 | La entrada se registra por clase: el docente la abre y el alumno se registra solo | Vigente |

## D1. Se continúa el sistema existente en lugar de empezar de cero

**Reemplazada por D12 el 4 de octubre de 2026.** Se había decidido conservar y corregir la API de horarios que venía en el repositorio. Esa API resultó no ser el sistema del proyecto.

## D2. Escala: un solo servidor, una API y una base de datos

**Decisión.** El sistema es una sola aplicación (monolito) que corre en un servidor de la universidad.

**Por qué.** El uso esperado son unos cuantos laboratorios, decenas de usuarios a la vez y algunos miles de asistencias por semestre. Un servidor lo soporta con margen durante años. El riesgo real a largo plazo no es la carga, sino que otro equipo herede el sistema y no lo entienda; por eso se prioriza que sea simple, documentado y probado.

**Descartado.** Microservicios, colas de mensajes y caché distribuida: agregan complejidad de operación sin beneficio a esta escala.

## D3. Un solo repositorio para API, frontend y base de datos

**Decisión.** Todo vive en este repositorio, en las carpetas `api/`, `web/` y `db/`.

**Por qué.** El equipo es de cuatro personas. Un cambio que toca la API y una pantalla viaja en una sola revisión, y la documentación queda en un solo lugar.

**Descartado.** Un repositorio por parte: obliga a coordinar versiones entre repositorios sin necesidad.

## D4. El frontend se hace de nuevo

**Decisión.** No se reutiliza el frontend que está en producción. Sus pantallas sirven solo como referencia de qué flujos existen.

**Por qué.** Lo decidió el equipo.

**Consecuencia.** La API se diseña libremente, sin tener que imitar la actual.

## D5. Aplicación web adaptable, no aplicaciones nativas

**Decisión.** Una sola aplicación web que se adapta a computadora, tableta y celular, instalable desde el navegador (PWA).

**Por qué.** Funciona en cualquier dispositivo sin instalar nada ni publicar en tiendas, y solo hay un código que mantener. Encaja con el uso: el registro de entrada puede hacerse desde la computadora del laboratorio o desde el celular, y el laboratorista puede vigilar los equipos desde una tableta.

**Descartado.** Aplicaciones nativas (Flutter, React Native): solo se justifican con notificaciones avanzadas, uso sin conexión o acceso al hardware del teléfono.

**Registro de entrada.** El alumno la registra desde cualquier computadora o teléfono con su número de control. Ver D22.

## D6. El backend entrega también el frontend

**Decisión.** En producción, la API sirve los archivos compilados del frontend. Solo corre un programa en el servidor.

**Por qué.** No hace falta Node en producción (el código del frontend lo ejecuta el navegador de cada usuario), no hay problemas de CORS y el inicio de sesión por cookie funciona directo porque todo sale del mismo origen.

**En desarrollo.** Corren dos procesos: la API y el servidor de Vite, que reenvía las llamadas a la API.

## D7. Autenticación con cookie de sesión y roles

**Decisión.** Inicio de sesión con cookie segura, contraseñas guardadas con hash y permisos por rol en cada ruta.

**Roles.** Administrador, laboratorista y docente, que son los que existen en la base recibida. Los alumnos no inician sesión: se identifican por matrícula al registrar su entrada.

**Por qué.** Con frontend y API en el mismo origen, la cookie es más simple y segura que manejar tokens en el navegador.

**Descartado por ahora.** Tokens JWT: serían necesarios si otra aplicación externa consumiera la API.

**Primer administrador.** Se crea al arrancar la API a partir de la configuración (correo y contraseña), solo si no existe ninguno. Así no hay contraseñas en los scripts de la base ni cuentas por defecto.

**Protecciones.** Límite de intentos de inicio de sesión, misma respuesta para correo inexistente y contraseña incorrecta, y revisión en cada petición de que la cuenta siga activa y con el mismo rol.

**Estado.** Implementado.

**Pendiente.** Qué puede hacer exactamente cada rol, y si los alumnos deben tener cuenta.

## D8. La base de datos: se parte de la existente y se revisa al recibirla

**Reemplazada por D13 el 4 de octubre de 2026.** La base llegó, se revisó y se decidió rediseñarla.

## D9. Las reglas de horario viven separadas de las rutas y de la base

**Reemplazada por D15 el 4 de octubre de 2026.** El principio se mantiene, pero las reglas de horario ya no aplican a este sistema.

## D10. Vocabulario del dominio en español

**Decisión.** Las entidades se nombran como las nombra el instituto: alumno, docente, laboratorista, laboratorio, equipo, asistencia, práctica, materia, curso, periodo.

**Por qué.** Evita traducir entre lo que dice el personal y lo que dice el código.

**Convención.** En la base de datos, tablas en plural y todo en minúsculas con guion bajo.

## D11. Despliegue

**Decisión provisional.** Docker Compose con la API, la base de datos y un proxy con HTTPS, más un respaldo automático diario de la base.

**Por qué.** Reinstalar o mover el servidor se vuelve un solo comando, y la configuración queda escrita.

**Depende de.** El sistema operativo del servidor. Si es Windows Server, se usaría IIS en lugar de Docker.

**Nota.** La base recibida está alojada en Hostinger. Falta definir si el sistema nuevo se queda ahí o pasa al servidor de la universidad, que es lo planeado.

## D12. El proyecto es el control de asistencias a laboratorios

**Decisión.** El sistema a construir registra el uso de los laboratorios de cómputo. La API de horarios que vino en el repositorio se sustituye.

**Por qué.** La base de datos entregada por el equipo es de asistencias a laboratorios y el equipo confirmó que ese es el proyecto.

**Qué se conserva del trabajo previo.** La estructura del repositorio, la configuración por secretos, el manejo de errores, la documentación automática de la API, el proyecto de pruebas y la integración continua.

**Hecho.** El código de horarios se retiró (queda en el historial de git) y el proyecto .NET se renombró a `Asistencias`.

**Por qué retirarlo.** Código que no se usa confunde a quien llega, sigue apareciendo en búsquedas y hay que mantenerlo compilando. Recuperarlo desde git cuesta un comando.

**Pendiente.** Renombrar el repositorio en GitHub. No se hizo porque cambia la dirección para todo el equipo y debe acordarse.

## D13. El esquema de la base de datos se rediseña

**Decisión.** Se crea un esquema nuevo de 15 tablas en lugar de usar el recibido. Está en [db/esquema.sql](../db/esquema.sql) y explicado en [modelo-de-datos.md](modelo-de-datos.md).

**Por qué.** La base recibida duplicaba los datos del alumno en cada asistencia, casi no tenía relaciones entre tablas, usaba texto libre para laboratorios y carreras, separaba las asistencias en dos tablas y repartía las cuentas en cuatro. Eso produce datos contradictorios y reportes poco confiables conforme pasa el tiempo.

**Por qué ahora.** La base recibida está casi vacía, así que no hay datos que migrar, y todavía no hay código nuevo que dependa de ella.

**Descartado.** Usarla tal cual y corregirla sobre la marcha: cada corrección posterior exigiría migrar datos reales.

**Estado.** Propuesta, pendiente de revisión del equipo.

## D14. Se conserva MariaDB

**Decisión.** La base de datos sigue en MariaDB 11.8.

**Por qué.** Es el motor de la base recibida y sirve bien para este sistema. Cambiarlo no aporta nada.

## D15. Las reglas de negocio viven separadas de las rutas

**Decisión.** La lógica propia del sistema (quién puede entrar, cuándo un equipo está disponible, cómo se resuelve una solicitud) va en clases que no conocen HTTP, para poder probarla sin levantar la API.

**Por qué.** Es donde un error cuesta más, y separada se prueba rápido.

**Complemento.** Las reglas que protegen los datos también se hacen cumplir en la base (por ejemplo, que un equipo no tenga dos ocupantes), para que ningún error de la aplicación pueda romperlas.

## D16. El esquema se define en SQL; EF Core solo lo lee

**Decisión.** La fuente única del esquema son los archivos de `db/`. EF Core se usa para consultar y guardar, no para crear ni modificar tablas.

**Por qué.** El esquema usa columnas calculadas y restricciones que ya están escritas y probadas en SQL. Quien administra la base trabaja en SQL y puede revisarlo directamente. Y con una sola fuente no hay riesgo de que el modelo en C# y la base real se separen sin que nadie lo note.

**Cómo se detecta un desajuste.** Las pruebas levantan la base desde esos mismos archivos; si el código espera una columna que no existe, fallan.

**Cambia respecto a lo planeado.** Antes se pensaba usar las migraciones de EF Core. Se descartan porque duplicarían el esquema en C#.

**Regla.** Mientras el esquema sea una propuesta se edita `db/esquema.sql`. Una vez en uso con datos reales, cada cambio es un archivo de migración nuevo y numerado.

## D17. Las pruebas corren contra una base MariaDB real

**Decisión.** Las pruebas de la API crean una base nueva en un servidor MariaDB, la usan y la borran.

**Por qué.** Buena parte de las reglas vive en la base (restricciones, llaves). Una base simulada en memoria no las aplicaría y daría pruebas que pasan con código que falla en producción.

**Costo.** Para correr las pruebas hace falta Docker. Se considera aceptable.

## D18. Reparto provisional de permisos por rol

**Decisión.** Todos los roles consultan. El laboratorista además marca equipos fuera de servicio y da de alta y edita alumnos. Solo el administrador modifica catálogos, borra y gestiona usuarios. La tabla completa está en [estado-actual.md](estado-actual.md).

**Por qué.** El instituto aún no define qué hace cada rol, pero sin algún reparto no se puede avanzar. Se eligió el más restrictivo que deja operar: quien atiende el laboratorio necesita registrar a un alumno que no está en la lista y reportar un equipo dañado, y nada más.

**Por qué es fácil de cambiar.** Cada ruta declara su permiso en una línea, y hay pruebas que fijan el comportamiento actual.

## D19. La bitácora se llena sola

**Decisión.** Cada alta, cambio o baja se anota en la bitácora desde un solo punto del código, en la misma transacción que el cambio, con el usuario que lo hizo.

**Por qué.** Si cada ruta tuviera que acordarse de anotar, tarde o temprano alguna no lo haría. Así no se puede olvidar.

**Qué se anota.** La acción, la tabla, el registro y los nombres de los campos que cambiaron. No se guardan los valores, para que la bitácora nunca contenga contraseñas ni datos personales.

**Qué no se anota.** Las asistencias, porque cada una ya es su propio registro. La excepción es cuando se borra una, que sí se anota, porque ahí el registro desaparece.

**Consulta.** Solo el administrador la consulta. No se puede editar ni borrar desde la API.

## D20. Lo que tiene historial se desactiva, no se borra

**Decisión.** Usuarios, alumnos, laboratorios y carreras tienen un indicador de activo. La base impide borrar un registro del que dependen otros, y la API lo informa con un 409.

**Por qué.** Borrar a un alumno borraría o dejaría huérfanas sus asistencias, y los reportes de semestres pasados dejarían de cuadrar.

## D21. El docente lleva la lista de sus cursos y las prácticas de sus materias

**Decisión.** Los cursos los crea y edita solo el administrador. Inscribir y dar de baja alumnos lo hacen el administrador, el laboratorista y el docente que imparte ese curso. Las prácticas de una materia las edita el administrador o cualquier docente que imparta un curso de esa materia.

**Por qué.** Es una extensión de D18 con el mismo criterio: lo más restrictivo que deja operar. Quien conoce la lista de un grupo y sus prácticas es su docente; si todo pasara por el administrador, sería un cuello de botella cada inicio de semestre.

**Consecuencia.** Como las prácticas son de la materia y no del curso, dos docentes que imparten la misma materia editan la misma lista de prácticas.

**Depende de.** La respuesta del instituto sobre si las prácticas son las mismas para todos los docentes de una materia. Si no lo son, las prácticas pasan a `cursos` y cada docente edita solo las suyas.

**Regla relacionada.** Un curso solo se asigna a un docente con la cuenta activa, pero lo conserva si la cuenta se desactiva después, para no perder quién lo impartió.

## D22. La entrada se registra por clase: el docente la abre y el alumno se registra solo

**Decisión.** El docente abre una clase de su curso en un laboratorio. Mientras está abierta, cada alumno inscrito registra su entrada con su número de control, sin iniciar sesión, desde cualquier computadora o teléfono. Al cerrar la clase, el sistema registra la salida de todos. El alumno no registra su salida.

**Por qué.** Así lo definió el responsable del backend el 10 de octubre de 2026, a partir de cómo se usan los laboratorios. Evita que el laboratorista capture a cada alumno y que haya salidas sin registrar.

**Cómo se evita el registro indebido.** Solo se acepta a un alumno activo, inscrito en un curso que tenga la clase abierta en ese momento, y que no esté ya dentro. La ruta tiene un límite de intentos por dirección.

**Riesgo conocido.** Quien conozca el número de control de un compañero puede registrarlo aunque no esté presente, y si el sistema queda accesible desde internet puede hacerlo desde fuera del plantel. Si eso resulta un problema, la solución prevista es un código de clase que el docente muestra en el aula.

**Reglas.** Un laboratorio tiene a lo más una clase abierta, y un curso también; la base lo hace cumplir. La clase la abre el docente del curso o el administrador, y la cierra cualquiera de ellos o el laboratorista, para poder liberar el laboratorio si el docente se fue sin cerrarla.

**Pendiente.** Si el alumno debe indicar el equipo que ocupa (hoy es opcional, tecleando su etiqueta), y el uso del laboratorio fuera de clase, que se resolverá junto con las solicitudes de ingreso.

## Decisiones abiertas

| Tema | Qué falta para decidir |
|---|---|
| Aprobación del esquema | Revisión del equipo |
| Nombre del repositorio en GitHub | Acordarlo en el equipo |
| Uso del laboratorio fuera de clase | Respuesta del instituto sobre quién lo autoriza |
| Permisos definitivos de cada rol | Respuesta del instituto |
| Dónde se aloja el sistema | Se dijo a la vez que el servidor será local, solo dentro del instituto, y que se instalará en Hostinger; hay que confirmar cuál |
| Subir a .NET 10 | Instalar el SDK en los equipos de desarrollo |
| Quién lleva el frontend | Definirlo en el equipo |

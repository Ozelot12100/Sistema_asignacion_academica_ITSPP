# Decisiones del proyecto

Registro de las decisiones que dan forma al sistema. Cada una dice qué se decidió, por qué y qué se descartó. Si una decisión cambia, no se borra: se marca como reemplazada y se agrega la nueva.

Las decisiones sobre herramientas concretas están en [stack-tecnologico.md](stack-tecnologico.md).

Última actualización: 4 de octubre de 2026.

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

**Pendiente de diseño.** Desde qué dispositivo y cómo se identifica el alumno al entrar. Depende de la respuesta del instituto.

## D6. El backend entrega también el frontend

**Decisión.** En producción, la API sirve los archivos compilados del frontend. Solo corre un programa en el servidor.

**Por qué.** No hace falta Node en producción (el código del frontend lo ejecuta el navegador de cada usuario), no hay problemas de CORS y el inicio de sesión por cookie funciona directo porque todo sale del mismo origen.

**En desarrollo.** Corren dos procesos: la API y el servidor de Vite, que reenvía las llamadas a la API.

## D7. Autenticación con cookie de sesión y roles

**Decisión.** Inicio de sesión con cookie segura, contraseñas guardadas con hash y permisos por rol en cada ruta.

**Roles.** Administrador, laboratorista y docente, que son los que existen en la base recibida. Los alumnos no inician sesión: se identifican por matrícula al registrar su entrada.

**Por qué.** Con frontend y API en el mismo origen, la cookie es más simple y segura que manejar tokens en el navegador.

**Descartado por ahora.** Tokens JWT: serían necesarios si otra aplicación externa consumiera la API.

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

**Pendiente.** Retirar el código de horarios y renombrar el proyecto.

## D13. El esquema de la base de datos se rediseña

**Decisión.** Se crea un esquema nuevo de 14 tablas en lugar de usar el recibido. Está en [db/esquema.sql](../db/esquema.sql) y explicado en [modelo-de-datos.md](modelo-de-datos.md).

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

## Decisiones abiertas

| Tema | Qué falta para decidir |
|---|---|
| Aprobación del esquema | Revisión del equipo |
| Qué hacer con el código de horarios | Confirmar que se retira |
| Nombre del proyecto y del repositorio | Acordarlo en el equipo |
| Cómo se identifica el alumno al entrar | Respuesta del instituto |
| Permisos de cada rol | Respuesta del instituto |
| Dónde se aloja el sistema | Confirmar servidor de la universidad o Hostinger, y su sistema operativo |
| Subir a .NET 10 | Instalar el SDK en los equipos de desarrollo |
| Quién lleva el frontend | Definirlo en el equipo |

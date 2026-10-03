# Decisiones del proyecto

Registro de las decisiones que dan forma al sistema. Cada una dice qué se decidió, por qué y qué se descartó. Si una decisión cambia, no se borra: se marca como reemplazada y se agrega la nueva al final.

Las decisiones sobre herramientas concretas están en [stack-tecnologico.md](stack-tecnologico.md).

Última actualización: 3 de octubre de 2026.

## D1. Se continúa el sistema existente en lugar de empezar de cero

**Decisión.** Se conserva la API heredada como punto de partida, se corrige y se amplía.

**Por qué.** Las entidades (profesores, aulas, asignaturas, grupos, clases, disponibilidad) son las correctas y los CRUD funcionan con ajustes menores. Lo que está mal es acotado: seguridad, validación de horarios y manejo de configuración. El detalle está en [estado-actual.md](estado-actual.md).

**Descartado.** Reescribir todo: se perdería trabajo válido sin ganar nada.

## D2. Escala: un solo servidor, una API y una base de datos

**Decisión.** El sistema es una sola aplicación (monolito) que corre en un servidor de la universidad.

**Por qué.** El uso esperado son decenas de usuarios y unos pocos miles de registros por semestre. Un servidor lo soporta con margen durante años. El riesgo real a largo plazo no es la carga, sino que otro equipo herede el sistema y no lo entienda; por eso se prioriza que sea simple, documentado y probado.

**Descartado.** Microservicios, colas de mensajes y caché distribuida: agregan complejidad de operación sin beneficio a esta escala.

## D3. Un solo repositorio para API, frontend y base de datos

**Decisión.** Todo vive en este repositorio, en las carpetas `api/`, `web/` y `db/`.

**Por qué.** El equipo es de cuatro personas. Un cambio que toca la API y una pantalla viaja en una sola revisión, y la documentación queda en un solo lugar.

**Descartado.** Un repositorio por parte: obliga a coordinar versiones entre repositorios sin necesidad.

## D4. El frontend se hace de nuevo

**Decisión.** No se reutiliza el frontend que está en producción. Sus pantallas sirven solo como referencia de qué flujos existen.

**Por qué.** Lo decidió el equipo; el frontend actual no está en este repositorio.

**Consecuencia.** La API no tiene que conservar sus rutas ni nombres de campos actuales. Conviene corregir el contrato ahora, antes de que el frontend nuevo dependa de él.

## D5. Aplicación web adaptable, no aplicaciones nativas

**Decisión.** Una sola aplicación web que se adapta a computadora, tableta y celular, instalable desde el navegador (PWA).

**Por qué.** Funciona en cualquier dispositivo sin instalar nada ni publicar en tiendas, y solo hay un código que mantener.

**Descartado.** Aplicaciones nativas (Flutter, React Native): solo se justifican con notificaciones avanzadas, uso sin conexión o acceso al hardware del teléfono, y el sistema no necesita nada de eso.

**Pendiente de diseño.** La cuadrícula semanal no cabe en un celular. Armar horarios se diseñará para computadora o tableta; consultar horarios, para celular con vista por día.

## D6. El backend entrega también el frontend

**Decisión.** En producción, la API sirve los archivos compilados del frontend. Solo corre un programa en el servidor.

**Por qué.** No hace falta Node en producción (el código del frontend lo ejecuta el navegador de cada usuario), no hay problemas de CORS y el inicio de sesión por cookie funciona directo porque todo sale del mismo origen.

**En desarrollo.** Corren dos procesos: la API y el servidor de Vite, que reenvía las llamadas a la API.

## D7. Autenticación con cookie de sesión y roles

**Decisión.** Inicio de sesión con cookie segura, contraseñas guardadas con hash y permisos por rol en cada ruta.

**Por qué.** Hoy la API no pide autenticación y guarda las contraseñas en texto plano. Con frontend y API en el mismo origen, la cookie es más simple y segura que manejar tokens en el navegador.

**Descartado por ahora.** Tokens JWT: serían necesarios si otra aplicación externa consumiera la API.

**Estado.** Sin implementar; depende de conocer la tabla de usuarios de la base actual. Los roles concretos dependen de lo que responda el instituto.

## D8. La base de datos: se parte de la existente y se revisa al recibirla

**Decisión.** Se usará la base que ya está hecha. Cuando llegue, se compara contra lo que el sistema necesita a largo plazo y se decide qué cambios aplicar.

**Por qué.** El equipo informó que la base ya existe. Aún no se ha visto, así que no se puede decidir si alcanza.

**Qué se va a revisar.** Lo siguiente se deduce del código heredado y hay que confirmarlo contra la base real:

- Que las clases pertenezcan a un periodo escolar. Sin eso, cada semestre habría que borrar los horarios del anterior.
- Que existan carreras y plan de estudios, en lugar de texto libre.
- Qué asignaturas puede impartir cada profesor.
- Horas por semana de cada asignatura.
- Tipos de dato de días y horas, y llaves foráneas.
- Que las contraseñas puedan guardarse con hash.

**Regla.** Todo cambio al esquema se hace con una migración versionada en el repositorio, nunca a mano sobre la base.

## D9. Las reglas de horario viven separadas de las rutas y de la base

**Decisión.** La lógica de traslapes, jornada y disponibilidad está en una clase propia (`ReglasHorario`) que no conoce HTTP ni SQL.

**Por qué.** Es el corazón del sistema y donde estaban los errores más graves. Separada, se prueba en milisegundos sin levantar la API ni la base.

**Estado.** Implementado, con 29 pruebas.

## D10. Vocabulario del dominio en español

**Decisión.** Las entidades se nombran como las nombra el instituto: profesor, aula, grupo, asignatura, clase, periodo.

**Por qué.** Evita traducir entre lo que dice el personal y lo que dice el código.

## D11. Despliegue

**Decisión provisional.** Docker Compose con la API, la base de datos y un proxy con HTTPS, más un respaldo automático diario de la base.

**Por qué.** Reinstalar o mover el servidor se vuelve un solo comando, y la configuración queda escrita.

**Depende de.** El sistema operativo del servidor. Si es Windows Server, se usaría IIS en lugar de Docker.

## Decisiones abiertas

| Tema | Qué falta para decidir |
|---|---|
| Motor de base de datos | Recibir la base; solo se sabe que es SQL |
| Cambios al esquema | Recibir la base y las respuestas del instituto |
| Roles y permisos | Saber quién arma los horarios y quién solo consulta |
| Forma de despliegue | Sistema operativo y acceso al servidor |
| Subir a .NET 10 | Instalar el SDK en los equipos de desarrollo |
| Quién lleva el frontend | Definirlo en el equipo |

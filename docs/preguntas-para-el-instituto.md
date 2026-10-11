# Preguntas para el personal del instituto

Estamos construyendo el nuevo sistema de control de asistencias a los laboratorios de cómputo del ITSPP. Para terminarlo necesitamos que el personal que administra y usa los laboratorios nos responda las siguientes preguntas. Con respuestas cortas es suficiente.

En cada pregunta decimos qué vamos a suponer si no hay respuesta, para que solo haga falta corregirnos donde estemos equivocados.

Las entrega: Andrés. Las respuestas se anotan aquí mismo, debajo de cada pregunta.

**Estado.** Se le entregaron a Andrés el 10 de octubre de 2026, en Word y PDF, para que las pase por mensaje o las pregunte en persona. Aún no hay respuestas. Después de entregarlas se resolvió la pregunta 2, así que ya no hay que hacerla.

Última actualización: 11 de octubre de 2026.

## Cómo va a funcionar el sistema

Para dar contexto a las preguntas, así funciona hoy lo que ya está construido:

1. El docente abre su clase en el sistema al llegar al laboratorio.
2. Cada alumno registra su entrada con su número de control, desde una computadora del laboratorio o desde su teléfono, e indica qué computadora ocupa o que trae su propio equipo.
3. Al terminar, el docente cierra la clase y el sistema registra la salida de todos.

Si algo de esto no corresponde a cómo quieren trabajar, es lo primero que necesitamos saber.

## Urgentes: sin ellas no podemos avanzar

**1. ¿Dónde se va a instalar el sistema: en un servidor dentro del instituto o en un servicio en internet (Hostinger)?**
Si está en internet, un alumno podría registrar su asistencia desde su casa y tendríamos que agregar un código que el docente muestre en el aula.
*Si no hay respuesta:* supondremos que estará en internet y agregaremos el código.
*Lo que creemos, sin confirmar:* David entiende que se instalará en Hostinger, pero también que los alumnos solo podrán conectarse dentro del instituto. Las dos cosas no pueden ser ciertas a la vez.

Respuesta:

**2. ¿El alumno debe indicar qué computadora ocupa al registrar su entrada, o basta con registrar que entró al laboratorio?**

Respuesta: ya no hace falta contestarla. El responsable del backend definió el 10 de octubre de 2026 que sí debe indicarla, o marcar que trae su propio equipo. Se conserva el número para que coincida con el documento que ya se entregó.

**3. Cuando un alumno o un grupo usa un laboratorio sin tener clase, ¿alguien debe autorizarlo? ¿Quién? ¿Quieren que ese uso quede registrado en el sistema?**
Entendemos que hoy el laboratorista solo pregunta si tienen clase y los deja quedarse.
*Si no hay respuesta:* el laboratorista podrá registrar ese uso, sin pedir autorización a nadie más.

Respuesta:

**4. ¿De dónde sale la lista de alumnos y las materias que lleva cada uno? ¿El sistema escolar puede entregarla en un archivo (por ejemplo, Excel) al inicio de cada semestre?**
*Si no hay respuesta:* los alumnos y sus grupos se capturarán a mano.

Respuesta:

## Sobre los laboratorios

**5. ¿Cuántos laboratorios de cómputo hay, cómo se llaman y cuántas computadoras tiene cada uno?**
Hoy solo tenemos registrado el laboratorio de programación, con 25 computadoras.

Respuesta:

**6. ¿Cada laboratorista atiende siempre el mismo laboratorio, o cualquiera?**
*Si no hay respuesta:* cualquier laboratorista podrá atender cualquier laboratorio.
*Lo que creemos, sin confirmar:* que cada laboratorista atiende un laboratorio fijo.

Respuesta:

**7. ¿Entran a los laboratorios personas que no son alumnos ni personal del instituto (visitas, cursos externos)? Si es así, ¿necesitan autorización previa y de quién?**
*Si no hay respuesta:* no se registrarán visitas.

Respuesta:

## Sobre quién hace qué

**8. ¿Qué debe poder hacer cada tipo de usuario?** Hoy lo tenemos así; corrijan lo que no corresponda:

| Acción | Administrador | Laboratorista | Docente |
|---|---|---|---|
| Consultar laboratorios, equipos, alumnos, cursos y prácticas | Sí | Sí | Sí |
| Marcar una computadora como fuera de servicio | Sí | Sí | No |
| Dar de alta y corregir datos de alumnos | Sí | Sí | No |
| Armar la lista de alumnos de un grupo | Sí | Sí | Solo de sus grupos |
| Capturar las prácticas de una materia | Sí | No | Solo de sus materias |
| Abrir una clase | Sí | No | Solo las suyas |
| Cerrar una clase | Sí | Sí | Solo las suyas |
| Crear cursos, materias, periodos y laboratorios | Sí | No | No |
| Crear cuentas de usuario | Sí | No | No |

Respuesta:

**9. ¿Las prácticas de una materia son las mismas para todos los docentes que la imparten, o cada docente define las suyas?**
*Si no hay respuesta:* habrá una sola lista de prácticas por materia, compartida por sus docentes.

Respuesta:

## Sobre los datos

**10. ¿El número de control de los alumnos siempre tiene 8 dígitos, como 23303080?**
*Si no hay respuesta:* se aceptará cualquier número de control, sin revisar su formato.
*Lo que creemos, sin confirmar:* que sí, siempre son 8 dígitos.

Respuesta:

## Sobre los reportes

**11. ¿Qué reportes necesitan y en qué formato (Excel, PDF)?** Proponemos estos; digan cuáles sirven, cuáles sobran y cuáles faltan:

- Lista de asistencia de una clase: quién entró, a qué hora y quién faltó.
- Asistencia de un grupo en el semestre: porcentaje de asistencia de cada alumno.
- Historial de un alumno: todas sus entradas, por materia.
- Uso de cada laboratorio: horas ocupadas por semana y por semestre, y por materia.
- Uso de las computadoras: cuántas veces se usó cada una y cuánto tiempo estuvo fuera de servicio.
- Prácticas realizadas por cada grupo, contra las planeadas.

*Si no hay respuesta:* empezaremos por la lista de asistencia de una clase y el uso de cada laboratorio.

Respuesta:

## Para el equipo, no para el instituto

- ¿La base de datos que recibimos es la que usa el sistema que está en producción? Todos sus registros son del 3 de octubre de 2026 y no tiene alumnos ni asistencias.
- ¿Dónde está el código del sistema actual? ¿Se pueden conseguir capturas de sus pantallas? Por ahora solo hay acceso a la vista de alumnos.

## Ya respondidas

| Pregunta | Respuesta | Fecha |
|---|---|---|
| ¿Cómo se identifica el alumno al entrar? | Con su número de control. Él mismo registra su entrada, desde cualquier computadora o teléfono, cuando el docente abre la clase | 10 de octubre de 2026 |
| ¿Quién registra la salida? | El docente, al cerrar la clase. El alumno solo registra su entrada | 10 de octubre de 2026 |
| ¿Qué turnos existen? | Matutino y vespertino | 10 de octubre de 2026 |
| ¿El alumno debe indicar qué computadora ocupa? | Sí. Indica la computadora del laboratorio o marca que trae su propio equipo; una de las dos es obligatoria | 10 de octubre de 2026 |

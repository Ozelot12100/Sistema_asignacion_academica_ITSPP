# Metodología de trabajo

Cómo se organiza el equipo y cómo entra un cambio al repositorio.

**Estado: propuesta.** Las reglas técnicas (ramas, revisiones, secretos) están listas para usarse. La parte de organización (tablero, reuniones, reparto de responsabilidades) falta acordarla entre los cuatro integrantes.

Última actualización: 10 de octubre de 2026.

## Equipo y responsabilidades

| Área | Responsable |
|---|---|
| Backend (API y base de datos) | Ozelot12100 |
| Frontend | Por definir |
| Contacto con el instituto | Por definir |
| Despliegue y servidor | Por definir |

Cada área tiene un responsable, pero cualquiera puede proponer cambios en cualquier parte mediante una revisión.

## Organización del trabajo

Se propone un flujo tipo Kanban, sin sprints fijos, porque el equipo trabaja en horarios distintos.

- **Tareas.** Cada tarea es un *issue* de GitHub con una descripción de qué debe quedar funcionando.
- **Tablero.** Un tablero de GitHub Projects con las columnas Pendiente, En curso, En revisión y Hecho.
- **Límite.** Cada persona tiene como máximo dos tareas en curso.
- **Reunión.** Una reunión corta por semana para revisar el tablero, desbloquear y repartir lo siguiente.
- **Decisiones.** Lo que se decida y afecte al sistema se anota en [decisiones.md](decisiones.md), no solo en el chat.

## Flujo de un cambio

1. Crear una rama a partir de `main` con un nombre que diga qué hace, por ejemplo `login-con-roles` o `corregir-traslapes`.
2. Hacer commits pequeños, cada uno con un mensaje que explique el cambio.
3. Subir la rama y abrir un *pull request* que diga qué cambia, por qué y cómo se probó.
4. Esperar a que la compilación y las pruebas automáticas pasen.
5. Otra persona del equipo revisa y aprueba.
6. Se fusiona a `main` y se borra la rama.

Reglas:

- El responsable del backend (Ozelot12100) puede subir directo a `main`, porque es quien desarrolla la mayor parte del sistema. Los demás integrantes entran por *pull request*.
- `main` siempre debe compilar y pasar las pruebas.
- Un *pull request* resuelve una sola cosa. Si crece demasiado, se parte.

## Mensajes de commit

En español, en infinitivo, describiendo el cambio y no el archivo:

- Bien: `Detectar traslapes parciales entre clases`
- Mal: `cambios`, `fix`, `actualizar ClasesEndpoints.cs`

Si hace falta contexto, va en el cuerpo del mensaje, separado por una línea en blanco.

## Cuándo una tarea está terminada

- Compila sin advertencias nuevas.
- Las pruebas pasan, y hay pruebas nuevas si se tocaron reglas de negocio o permisos.
- Se probó a mano el caso principal.
- La documentación se actualizó si cambió una decisión, una ruta o la forma de ejecutar el proyecto.
- Otra persona lo revisó, si el cambio entró por *pull request*.

## Convenciones de código

### Generales

- Vocabulario del dominio en español, igual que lo nombra el instituto (alumno, docente, laboratorista, laboratorio, equipo, asistencia, práctica).
- Un solo estilo de nombres por capa: minúsculas con guion bajo en la base de datos, y el estilo propio de cada lenguaje en el código.
- Los comentarios explican el porqué, no lo que el código ya dice.

### Backend

- Las reglas de negocio van en clases propias, sin HTTP ni SQL, para poder probarlas.
- Las rutas validan la entrada, llaman a las reglas y responden. No contienen lógica de negocio.
- Códigos de respuesta: 400 para datos inválidos, 401 sin sesión, 403 sin permiso, 404 si no existe, 409 para conflictos como un equipo ya ocupado.
- Toda consulta SQL usa parámetros. Nunca se concatena texto del usuario.
- El esquema se cambia en los archivos SQL de `db/`, nunca a mano sobre una base. El mismo cambio incluye el ajuste del modelo en C# y sus pruebas.

### Frontend

- TypeScript en modo estricto, sin `any`.
- Los tipos de la API se generan; no se escriben a mano.
- El formato lo aplican ESLint y Prettier.

## Pruebas

Las pruebas de la API corren contra una base MariaDB real, que cada quien levanta con Docker (ver el [README](../README.md)).

El esfuerzo se concentra donde un error cuesta más:

- **Reglas de negocio** (registro de entrada y salida, disponibilidad de equipos, solicitudes de ingreso): cobertura completa, incluyendo casos límite.
- **Permisos**: que cada rol pueda hacer solo lo que le corresponde.
- **CRUD simples**: una prueba del caso normal basta.

## Secretos y configuración

- Ninguna contraseña, cadena de conexión o llave se escribe en un archivo versionado.
- En desarrollo se usan los secretos de usuario de .NET (`dotnet user-secrets`).
- En el servidor se usan variables de entorno.
- Las credenciales no se comparten por chat. Si una ya se compartió así, se cambia.
- Si un secreto llega a subirse al repositorio, se cambia de inmediato; borrarlo del historial no es suficiente.

## Base de datos y producción

- No se modifica la base de producción a mano.
- Antes de aplicar una migración en producción se hace un respaldo.
- Los datos reales no se copian a las computadoras personales; se usan datos de prueba.

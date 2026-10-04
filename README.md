# Sistema de Control de Asistencias a Laboratorios — ITSPP

Sistema para registrar quién usa los laboratorios de cómputo del ITSPP: entradas y salidas de alumnos, docentes e invitados, qué equipo ocupa cada quien, solicitudes de ingreso, prácticas por materia y avisos al administrador.

> **Aviso sobre el código.** La carpeta `api/` contiene todavía una API de horarios (profesores, aulas, clases) que se recibió con el repositorio y **no corresponde a este sistema**. Está pendiente de sustituirse. El nombre del repositorio también viene de ese proyecto. Ver [docs/estado-actual.md](docs/estado-actual.md).

## Estructura

| Carpeta | Contenido |
|---|---|
| [db/](db/) | Esquema propuesto de la base de datos y datos iniciales |
| [docs/](docs/) | Modelo de datos, decisiones, stack, forma de trabajo y estado del proyecto |
| [api/](api/) | API en C# (ASP.NET Core). Hoy contiene el código de horarios heredado |
| [web/](web/) | Frontend en React (aún sin iniciar) |

## Documentación

- [Estado actual](docs/estado-actual.md): qué hay, qué se hizo y qué falta.
- [Modelo de datos](docs/modelo-de-datos.md): qué hace el sistema y cómo queda la base de datos.
- [Decisiones del proyecto](docs/decisiones.md): qué se decidió y por qué.
- [Stack tecnológico](docs/stack-tecnologico.md): herramientas elegidas, con su justificación y lo que se descartó.
- [Metodología de trabajo](docs/metodologia-de-trabajo.md): ramas, revisiones, convenciones y manejo de secretos.

## Cómo levantar la base de datos local

Las instrucciones están en [db/README.md](db/README.md).

## Cómo ejecutar la API

Requisito: SDK de .NET 9.

1. Configura la cadena de conexión. Se guarda fuera del repositorio:

   ```
   cd api/AsignacionAcademica.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=HOST;Database=BASE;Uid=USUARIO;Pwd=CONTRASEÑA;"
   ```

2. Ejecuta `dotnet run`. La API queda en `http://localhost:5270` y su documentación en `http://localhost:5270/scalar/v1`.

3. Para correr las pruebas, desde `api/`: `dotnet test`.

Mientras la API siga siendo la de horarios, sus rutas no funcionan contra la base de asistencias.

Nunca escribas contraseñas en `appsettings.json` ni en ningún archivo versionado.

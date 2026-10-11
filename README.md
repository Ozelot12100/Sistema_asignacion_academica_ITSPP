# Sistema de Control de Asistencias a Laboratorios — ITSPP

Sistema para registrar quién usa los laboratorios de cómputo del ITSPP: entradas y salidas de alumnos, docentes e invitados, qué equipo ocupa cada quien, solicitudes de ingreso, prácticas por materia y avisos al administrador.

El repositorio todavía se llama `Sistema_asignacion_academica_ITSPP` por el proyecto con el que se recibió; está pendiente renombrarlo.

## Estructura

| Carpeta | Contenido |
|---|---|
| [api/](api/) | API en C# (ASP.NET Core) y sus pruebas |
| [db/](db/) | Esquema de la base de datos y datos iniciales |
| [web/](web/) | Frontend en React (aún sin iniciar) |
| [docs/](docs/) | Modelo de datos, decisiones, stack, forma de trabajo y estado del proyecto |

## Documentación

- [Guía para asistentes (CLAUDE.md)](CLAUDE.md): contexto, stack y pautas para Claude Code / Antigravity.
- [Estado actual](docs/estado-actual.md): qué funciona y qué falta.
- [Modelo de datos](docs/modelo-de-datos.md): qué hace el sistema y cómo queda la base de datos.
- [Preguntas para el instituto](docs/preguntas-para-el-instituto.md): lo que falta que responda el personal para poder continuar.
- [Decisiones del proyecto](docs/decisiones.md): qué se decidió y por qué.
- [Stack tecnológico](docs/stack-tecnologico.md): herramientas elegidas, con su justificación y lo que se descartó.
- [Metodología de trabajo](docs/metodologia-de-trabajo.md): ramas, revisiones, convenciones y manejo de secretos.

## Cómo ejecutarlo en tu máquina

Requisitos: SDK de .NET 9 y Docker.

1. Levanta la base de datos y carga el esquema, desde la raíz del repositorio:

   ```
   docker run -d --name itspp-db -e MARIADB_ROOT_PASSWORD=local -e MARIADB_DATABASE=asistencias -p 3306:3306 mariadb:11.8
   docker exec -i itspp-db mariadb -uroot -plocal asistencias < db/esquema.sql
   docker exec -i itspp-db mariadb -uroot -plocal asistencias < db/datos-iniciales.sql
   ```

2. Configura la API una sola vez. Estos valores se guardan fuera del repositorio:

   ```
   cd api/Asistencias.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=asistencias;Uid=root;Pwd=local;"
   dotnet user-secrets set "AdminInicial:Email" "tu-correo@ejemplo.mx"
   dotnet user-secrets set "AdminInicial:Password" "una-contraseña-de-12-o-más"
   ```

   Al arrancar, si no existe ningún administrador, la API crea uno con ese correo y contraseña.

3. Ejecuta `dotnet run`. La API queda en `http://localhost:5270` y su documentación interactiva en `http://localhost:5270/scalar/v1`.

## Pruebas

Desde `api/`, con el contenedor de la base en marcha:

```
dotnet test
```

Las pruebas crean una base de datos nueva en ese servidor, la usan y la borran. Si tu servidor de pruebas no es `localhost:3306` con `root` / `local`, indícalo en la variable de entorno `ASISTENCIAS_TEST_DB`.

## En el servidor

La configuración se pasa con variables de entorno: `ConnectionStrings__DefaultConnection`, `AdminInicial__Email`, `AdminInicial__Password` y `ASPNETCORE_URLS`.

La contraseña `local` es solo para tu máquina. Nunca escribas contraseñas reales en `appsettings.json` ni en ningún archivo versionado.

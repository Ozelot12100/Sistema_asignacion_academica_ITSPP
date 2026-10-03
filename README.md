# Sistema de Asignación Académica — ITSPP

Sistema para capturar y validar los horarios del ITSPP: qué profesor imparte qué asignatura, a qué grupo, en qué aula y a qué hora. Avisa cuando una clase se traslapa con otra, cuando el grupo no cabe en el aula o cuando el profesor no está disponible.

Es la continuación de un sistema que ya está en producción. Este repositorio contiene la API heredada, que se está corrigiendo y ampliando, y contendrá el frontend nuevo.

## Estructura

| Carpeta | Contenido |
|---|---|
| [api/](api/) | API en C# (ASP.NET Core) y sus pruebas |
| [web/](web/) | Frontend en React (aún sin iniciar) |
| [db/](db/) | Esquema y migraciones de la base de datos (pendiente de recibir la base actual) |
| [docs/](docs/) | Decisiones, stack, forma de trabajo y estado del proyecto |

## Documentación

- [Estado actual](docs/estado-actual.md): qué funciona, qué se corrigió y qué falta.
- [Decisiones del proyecto](docs/decisiones.md): qué se decidió y por qué.
- [Stack tecnológico](docs/stack-tecnologico.md): herramientas elegidas, con su justificación y lo que se descartó.
- [Metodología de trabajo](docs/metodologia-de-trabajo.md): ramas, revisiones, convenciones y manejo de secretos.

## Cómo ejecutar la API

Requisitos: SDK de .NET 9 y acceso a una base de datos MySQL con el esquema del sistema.

1. Configura la cadena de conexión. Se guarda fuera del repositorio:

   ```
   cd api/AsignacionAcademica.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=HOST;Database=BASE;Uid=USUARIO;Pwd=CONTRASEÑA;"
   ```

2. Ejecuta:

   ```
   dotnet run
   ```

   La API queda en `http://localhost:5270` y la documentación interactiva en `http://localhost:5270/scalar/v1`.

3. Para correr las pruebas, desde `api/`:

   ```
   dotnet test
   ```

En el servidor, la cadena de conexión se pasa con la variable de entorno `ConnectionStrings__DefaultConnection` y la dirección de escucha con `ASPNETCORE_URLS`.

Nunca escribas contraseñas en `appsettings.json` ni en ningún archivo versionado.

# Stack tecnológico

Herramientas elegidas para el sistema, para qué sirve cada una, por qué se eligió y qué se descartó.

Criterio general: herramientas conocidas, con soporte largo y con mucha documentación, para que el equipo que herede el sistema pueda mantenerlo.

Última actualización: 3 de octubre de 2026.

## Resumen

| Parte | Herramienta |
|---|---|
| Backend | C# con ASP.NET Core |
| Acceso a datos | EF Core con migraciones (hoy: Dapper) |
| Base de datos | SQL; motor por confirmar (el código heredado usa MySQL) |
| Frontend | React, TypeScript y Vite |
| Interfaz | Tailwind CSS y shadcn/ui |
| Pruebas | xUnit en el backend; Vitest en el frontend |
| Integración continua | GitHub Actions |
| Despliegue | Un servidor en la universidad; Docker Compose si es Linux |

## Backend

### C# con ASP.NET Core

- **Para qué.** La API: rutas, validación, reglas de negocio, autenticación.
- **Por qué.** El código heredado ya está en C# y quien lleva el backend lo domina. Es de lo más sólido para sistemas con reglas de negocio y base relacional: tipado fuerte, buen rendimiento y soporte largo.
- **Descartado.** TypeScript con Node (NestJS): su única ventaja sería tener un solo lenguaje en backend y frontend, y no compensa reescribir. Python, Java, Go y PHP no aportan aquí nada que C# no tenga.

### Versión de .NET

- **Hoy.** .NET 9, que es lo que usa el código heredado y lo que está instalado en los equipos.
- **Pendiente.** Subir a .NET 10. La versión 9 es de soporte corto y lo pierde en noviembre de 2026; la 10 es de soporte largo, hasta noviembre de 2028. Requiere instalar el SDK de .NET 10.

### Estilo de la API: Minimal API organizada por módulo

- **Para qué.** Definir las rutas con poco código, agrupadas por entidad (profesores, clases, etc.).
- **Por qué.** Ya está hecho así y es suficiente para este tamaño.
- **Descartado.** Arquitecturas de cuatro o cinco capas (Clean Architecture): excesivas para un sistema de este tamaño.

### Acceso a datos: EF Core con migraciones

- **Para qué.** Leer y escribir en la base, y versionar el esquema.
- **Por qué.** Cada cambio al esquema queda como un archivo de migración en el repositorio, reproducible en cualquier máquina. Se puede generar el modelo a partir de la base existente.
- **Estado.** Sin adoptar. Hoy el código usa Dapper con SQL escrito a mano. El cambio se hará cuando se conozca el motor y el esquema de la base.
- **Descartado.** Seguir solo con Dapper: funciona, pero no versiona el esquema y cada consulta se mantiene a mano. Puede conservarse para consultas complejas puntuales.

### Documentación de la API: OpenAPI y Scalar

- **Para qué.** La API describe sola sus rutas y campos (`/openapi/v1.json`), con una página para probarlas (`/scalar/v1`). Solo disponible en desarrollo.
- **Por qué.** Es la fuente de la que el frontend genera sus tipos; así no pueden desincronizarse.

### Errores: Problem Details

- **Para qué.** Que todos los errores inesperados salgan con el mismo formato estándar.
- **Pendiente.** Las validaciones heredadas aún responden con texto simple; se unificarán al reescribir las rutas.

### Pruebas: xUnit

- **Para qué.** Probar las reglas de horario y, más adelante, las rutas contra una base de prueba.
- **Por qué.** Es el estándar en .NET.

## Base de datos

- **Motor.** Por confirmar. Se sabe que la base actual es SQL; el código heredado usa MySQL.
- **Criterio.** Si la base ya está en MySQL, SQL Server o PostgreSQL, se conserva el motor. Los tres sirven para este sistema y cambiarlo no aporta nada.
- **Esquema.** Versionado en `db/` mediante migraciones. Ver la decisión D8 en [decisiones.md](decisiones.md).

## Frontend

El frontend es una aplicación de una sola página: se compila a archivos estáticos que entrega el backend.

### Base

| Herramienta | Para qué | Por qué |
|---|---|---|
| TypeScript | El lenguaje | Es JavaScript con tipos; detecta errores antes de ejecutar |
| React | Construir la interfaz con componentes | Es lo que más probablemente conozca quien herede el sistema, y tiene las mejores librerías para la cuadrícula de horarios |
| Vite | Servidor de desarrollo y compilación | Rápido y casi sin configuración |
| Node.js (LTS) y npm | Ejecutar Vite, TypeScript y Tailwind | Solo para desarrollar y compilar; no se instala en el servidor |

### Interfaz

| Herramienta | Para qué | Por qué |
|---|---|---|
| Tailwind CSS | Estilos y diseño adaptable | El diseño para celular, tableta y computadora sale con poco esfuerzo |
| shadcn/ui | Componentes ya hechos (tablas, diálogos, formularios) | Son accesibles y su código queda dentro del proyecto, así que se personalizan con la identidad del ITSPP |
| dnd-kit | Arrastrar y soltar clases en el horario | Funciona con mouse y con pantalla táctil |
| CSS Grid propio | La cuadrícula semanal | Las librerías de calendario están pensadas para fechas, no para un horario semanal fijo, y sus vistas por aula o profesor suelen ser de pago |

### Datos y navegación

| Herramienta | Para qué | Por qué |
|---|---|---|
| React Router | Rutas de la aplicación y pantallas protegidas por rol | Es el estándar en React |
| TanStack Query | Pedir datos a la API, caché, estados de carga y error | Evita escribir ese manejo a mano en cada pantalla |
| openapi-typescript | Generar los tipos a partir de la documentación de la API | Si cambia un campo en C#, el frontend deja de compilar hasta que se corrija |
| React Hook Form y Zod | Formularios y validación | Validan antes de enviar, con poco código |

### Calidad y entrega

| Herramienta | Para qué |
|---|---|
| ESLint y Prettier | Mismo estilo de código para todos, aplicado automáticamente |
| Vitest y Testing Library | Pruebas de componentes y de lógica |
| vite-plugin-pwa | Que la aplicación se pueda instalar en el celular |

### Descartado en el frontend

| Opción | Por qué no |
|---|---|
| Next.js | Aporta renderizado en servidor y posicionamiento en buscadores, que una aplicación interna con inicio de sesión no necesita, y obligaría a correr Node en producción |
| Redux u otro estado global | TanStack Query cubre los datos del servidor; el resto es estado local |
| Blazor (frontend en C#) | Menos librerías de interfaz, sobre todo para arrastrar y soltar en táctil; peor experiencia en celular; menos gente lo conoce. Sería la alternativa si quien haga el frontend solo supiera C# |
| Razor Pages con HTMX | Simple y sin Node, bueno para catálogos, pero la cuadrícula de horarios habría que hacerla a mano en JavaScript |
| React sin compilar (desde un CDN) | Se pierden TypeScript, los tipos generados de la API y casi todas las librerías |
| Flutter o React Native | Ver la decisión D5 en [decisiones.md](decisiones.md) |

## Infraestructura

| Herramienta | Para qué | Estado |
|---|---|---|
| Git y GitHub | Control de versiones y revisión de cambios | En uso |
| GitHub Actions | Compilar y probar cada cambio automáticamente | Configurado para la API |
| Docker Compose | Levantar API, base de datos y proxy con HTTPS en el servidor | Pendiente; depende del sistema operativo del servidor |
| Respaldo diario de la base | Poder recuperar los datos | Pendiente |

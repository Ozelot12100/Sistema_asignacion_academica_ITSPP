# Base de datos

Esquema del sistema de control de asistencias a laboratorios. Motor: MariaDB 11.8.

| Archivo | Contenido |
|---|---|
| [esquema.sql](esquema.sql) | Las 15 tablas, con sus llaves y restricciones |
| [datos-iniciales.sql](datos-iniciales.sql) | El laboratorio de programación y sus 25 equipos |

El porqué de cada tabla y la comparación con la base recibida están en [docs/modelo-de-datos.md](../docs/modelo-de-datos.md).

**Estado: propuesta**, pendiente de revisión del equipo. La API y sus pruebas ya trabajan sobre este esquema.

## Crear una base local de prueba

Con Docker en marcha:

```
docker run -d --name itspp-db -e MARIADB_ROOT_PASSWORD=local -e MARIADB_DATABASE=asistencias -p 3306:3306 mariadb:11.8
docker exec -i itspp-db mariadb -uroot -plocal asistencias < db/esquema.sql
docker exec -i itspp-db mariadb -uroot -plocal asistencias < db/datos-iniciales.sql
```

La contraseña `local` es solo para la base de tu máquina.

Sin Docker, con un MariaDB ya instalado (por ejemplo el que trae WAMP o XAMPP; revisa en qué puerto escucha):

```
mariadb -uroot -p -P 3306 -e "CREATE DATABASE asistencias CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci"
mariadb -uroot -p -P 3306 --default-character-set=utf8mb4 asistencias < db/esquema.sql
mariadb -uroot -p -P 3306 --default-character-set=utf8mb4 asistencias < db/datos-iniciales.sql
```

En PowerShell el `<` no funciona: usa `-e "source db/esquema.sql"` en su lugar.

Tiene que ser MariaDB, no MySQL: la API usa `RETURNING`, que MySQL no tiene.

## Cuando cambia el esquema

`esquema.sql` crea las tablas desde cero; no actualiza una base que ya existe. Mientras el esquema sea una propuesta, al cambiar hay que volver a crear la base local, o aplicarle a mano el mismo cambio. Las pruebas no tienen ese problema: cada corrida crea su propia base desde estos archivos y la borra al terminar.

Cambios hasta ahora:

| Fecha | Cambio |
|---|---|
| 10 de octubre de 2026 | Tabla nueva `clases`, y columna `clase_id` en `asistencias` |

## Reglas

- No se guardan aquí datos reales, respaldos ni credenciales.
- Mientras el esquema sea una propuesta, los cambios se hacen sobre `esquema.sql`. Una vez aprobado y en uso, todo cambio se hace con una migración nueva; el archivo ya aplicado no se edita.

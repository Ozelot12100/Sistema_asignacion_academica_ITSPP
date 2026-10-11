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

## Reglas

- No se guardan aquí datos reales, respaldos ni credenciales.
- Mientras el esquema sea una propuesta, los cambios se hacen sobre `esquema.sql`. Una vez aprobado y en uso, todo cambio se hace con una migración nueva; el archivo ya aplicado no se edita.

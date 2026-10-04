-- Esquema propuesto del sistema de control de asistencias a laboratorios (ITSPP)
-- Motor: MariaDB 11.8, InnoDB, utf8mb4
-- Las fechas con hora se guardan en UTC; la API las convierte a la hora local.
-- La explicación de cada tabla está en docs/modelo-de-datos.md

SET NAMES utf8mb4;

-- ---------------------------------------------------------------------------
-- Catálogos
-- ---------------------------------------------------------------------------

CREATE TABLE carreras (
  id      INT UNSIGNED NOT NULL AUTO_INCREMENT,
  clave   VARCHAR(20)  NOT NULL,
  nombre  VARCHAR(100) NOT NULL,
  activa  BOOLEAN      NOT NULL DEFAULT TRUE,
  PRIMARY KEY (id),
  UNIQUE KEY uq_carreras_clave (clave),
  UNIQUE KEY uq_carreras_nombre (nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE periodos (
  id            INT UNSIGNED NOT NULL AUTO_INCREMENT,
  nombre        VARCHAR(50)  NOT NULL,
  fecha_inicio  DATE         NOT NULL,
  fecha_fin     DATE         NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_periodos_nombre (nombre),
  CONSTRAINT ck_periodos_fechas CHECK (fecha_fin > fecha_inicio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE laboratorios (
  id         INT UNSIGNED NOT NULL AUTO_INCREMENT,
  clave      VARCHAR(50)  NOT NULL,
  nombre     VARCHAR(100) NOT NULL,
  ubicacion  VARCHAR(100) DEFAULT NULL,
  activo     BOOLEAN      NOT NULL DEFAULT TRUE,
  PRIMARY KEY (id),
  UNIQUE KEY uq_laboratorios_clave (clave)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Si un equipo está en uso no se guarda aquí: se deduce de que tenga una asistencia sin hora de salida.
CREATE TABLE equipos (
  id                 INT UNSIGNED NOT NULL AUTO_INCREMENT,
  laboratorio_id     INT UNSIGNED NOT NULL,
  etiqueta           VARCHAR(50)  NOT NULL,
  fuera_de_servicio  BOOLEAN      NOT NULL DEFAULT FALSE,
  notas              TEXT         DEFAULT NULL,
  actualizado_en     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_equipos_etiqueta (laboratorio_id, etiqueta),
  -- Permite que asistencias exija que el equipo pertenezca al laboratorio indicado
  UNIQUE KEY uq_equipos_id_laboratorio (id, laboratorio_id),
  CONSTRAINT fk_equipos_laboratorio FOREIGN KEY (laboratorio_id) REFERENCES laboratorios (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Personas
-- ---------------------------------------------------------------------------

-- Cuentas que inician sesión: administradores, laboratoristas y docentes.
-- Las cuentas no se borran; se desactivan, para conservar el historial.
CREATE TABLE usuarios (
  id               INT UNSIGNED NOT NULL AUTO_INCREMENT,
  rol              ENUM('admin','laboratorista','docente') NOT NULL,
  numero_empleado  VARCHAR(20)  DEFAULT NULL,
  nombre           VARCHAR(100) NOT NULL,
  apellido         VARCHAR(100) NOT NULL,
  email            VARCHAR(150) NOT NULL,
  password_hash    VARCHAR(255) NOT NULL,
  departamento     VARCHAR(100) DEFAULT NULL,
  activo           BOOLEAN      NOT NULL DEFAULT TRUE,
  creado_en        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_usuarios_email (email),
  UNIQUE KEY uq_usuarios_numero_empleado (numero_empleado)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE alumnos (
  id          INT UNSIGNED NOT NULL AUTO_INCREMENT,
  matricula   VARCHAR(20)  NOT NULL,
  nombre      VARCHAR(100) NOT NULL,
  apellido    VARCHAR(100) NOT NULL,
  carrera_id  INT UNSIGNED DEFAULT NULL,
  semestre    TINYINT UNSIGNED DEFAULT NULL,
  turno       ENUM('matutino','vespertino') DEFAULT NULL,
  grupo       VARCHAR(20)  DEFAULT NULL,
  activo      BOOLEAN      NOT NULL DEFAULT TRUE,
  creado_en   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_alumnos_matricula (matricula),
  KEY ix_alumnos_carrera (carrera_id),
  CONSTRAINT fk_alumnos_carrera FOREIGN KEY (carrera_id) REFERENCES carreras (id),
  CONSTRAINT ck_alumnos_semestre CHECK (semestre IS NULL OR semestre BETWEEN 1 AND 15)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Materias, cursos y prácticas
-- ---------------------------------------------------------------------------

-- Catálogo: la materia en sí, sin docente ni periodo.
CREATE TABLE materias (
  id          INT UNSIGNED NOT NULL AUTO_INCREMENT,
  clave       VARCHAR(50)  NOT NULL,
  nombre      VARCHAR(150) NOT NULL,
  carrera_id  INT UNSIGNED DEFAULT NULL,
  semestre    TINYINT UNSIGNED DEFAULT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_materias_clave (clave),
  KEY ix_materias_carrera (carrera_id),
  CONSTRAINT fk_materias_carrera FOREIGN KEY (carrera_id) REFERENCES carreras (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Una materia impartida por un docente a un grupo en un periodo.
CREATE TABLE cursos (
  id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
  materia_id      INT UNSIGNED NOT NULL,
  periodo_id      INT UNSIGNED NOT NULL,
  grupo           VARCHAR(20)  NOT NULL,
  docente_id      INT UNSIGNED DEFAULT NULL,
  laboratorio_id  INT UNSIGNED DEFAULT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_cursos (materia_id, periodo_id, grupo),
  KEY ix_cursos_periodo (periodo_id),
  KEY ix_cursos_docente (docente_id),
  KEY ix_cursos_laboratorio (laboratorio_id),
  CONSTRAINT fk_cursos_materia FOREIGN KEY (materia_id) REFERENCES materias (id),
  CONSTRAINT fk_cursos_periodo FOREIGN KEY (periodo_id) REFERENCES periodos (id),
  CONSTRAINT fk_cursos_docente FOREIGN KEY (docente_id) REFERENCES usuarios (id),
  CONSTRAINT fk_cursos_laboratorio FOREIGN KEY (laboratorio_id) REFERENCES laboratorios (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Qué alumnos llevan cada curso. Sustituye a la lista de materias en texto del alumno.
CREATE TABLE inscripciones (
  curso_id   INT UNSIGNED NOT NULL,
  alumno_id  INT UNSIGNED NOT NULL,
  PRIMARY KEY (curso_id, alumno_id),
  KEY ix_inscripciones_alumno (alumno_id),
  CONSTRAINT fk_inscripciones_curso FOREIGN KEY (curso_id) REFERENCES cursos (id) ON DELETE CASCADE,
  CONSTRAINT fk_inscripciones_alumno FOREIGN KEY (alumno_id) REFERENCES alumnos (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE practicas (
  id                INT UNSIGNED NOT NULL AUTO_INCREMENT,
  materia_id        INT UNSIGNED NOT NULL,
  numero            SMALLINT UNSIGNED NOT NULL,
  titulo            VARCHAR(150) NOT NULL,
  descripcion       TEXT         DEFAULT NULL,
  duracion_minutos  SMALLINT UNSIGNED DEFAULT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_practicas_numero (materia_id, numero),
  CONSTRAINT fk_practicas_materia FOREIGN KEY (materia_id) REFERENCES materias (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Operación diaria
-- ---------------------------------------------------------------------------

CREATE TABLE solicitudes_ingreso (
  id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
  alumno_id       INT UNSIGNED NOT NULL,
  laboratorio_id  INT UNSIGNED NOT NULL,
  motivo          TEXT         NOT NULL,
  estado          ENUM('pendiente','aprobada','rechazada') NOT NULL DEFAULT 'pendiente',
  creada_en       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  resuelta_por    INT UNSIGNED DEFAULT NULL,
  resuelta_en     DATETIME     DEFAULT NULL,
  comentario      VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (id),
  KEY ix_solicitudes_estado (laboratorio_id, estado),
  KEY ix_solicitudes_alumno (alumno_id),
  CONSTRAINT fk_solicitudes_alumno FOREIGN KEY (alumno_id) REFERENCES alumnos (id),
  CONSTRAINT fk_solicitudes_laboratorio FOREIGN KEY (laboratorio_id) REFERENCES laboratorios (id),
  CONSTRAINT fk_solicitudes_resuelta_por FOREIGN KEY (resuelta_por) REFERENCES usuarios (id),
  CONSTRAINT ck_solicitudes_resolucion CHECK ((estado = 'pendiente') = (resuelta_en IS NULL))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Cada entrada a un laboratorio. Mientras hora_salida es NULL la persona sigue dentro.
-- Sustituye a asistencias y asistencias_historico.
CREATE TABLE asistencias (
  id               BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  laboratorio_id   INT UNSIGNED NOT NULL,
  -- NULL cuando la persona usa su propio equipo
  equipo_id        INT UNSIGNED DEFAULT NULL,
  tipo_usuario     ENUM('estudiante','docente','invitado') NOT NULL,
  alumno_id        INT UNSIGNED DEFAULT NULL,
  usuario_id       INT UNSIGNED DEFAULT NULL,
  invitado_nombre  VARCHAR(200) DEFAULT NULL,
  curso_id         INT UNSIGNED DEFAULT NULL,
  practica_id      INT UNSIGNED DEFAULT NULL,
  solicitud_id     INT UNSIGNED DEFAULT NULL,
  hora_entrada     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  hora_salida      DATETIME     DEFAULT NULL,
  -- Solo tienen valor mientras la asistencia está abierta; sus índices únicos impiden
  -- que un equipo tenga dos ocupantes o que un alumno esté dentro dos veces.
  equipo_ocupado   INT UNSIGNED AS (IF(hora_salida IS NULL, equipo_id, NULL)) STORED,
  alumno_dentro    INT UNSIGNED AS (IF(hora_salida IS NULL, alumno_id, NULL)) STORED,
  PRIMARY KEY (id),
  UNIQUE KEY uq_asistencias_equipo_ocupado (equipo_ocupado),
  UNIQUE KEY uq_asistencias_alumno_dentro (alumno_dentro),
  KEY ix_asistencias_laboratorio_fecha (laboratorio_id, hora_entrada),
  KEY ix_asistencias_alumno_fecha (alumno_id, hora_entrada),
  KEY ix_asistencias_equipo (equipo_id, laboratorio_id),
  KEY ix_asistencias_usuario (usuario_id),
  KEY ix_asistencias_curso (curso_id),
  KEY ix_asistencias_practica (practica_id),
  KEY ix_asistencias_solicitud (solicitud_id),
  CONSTRAINT fk_asistencias_laboratorio FOREIGN KEY (laboratorio_id) REFERENCES laboratorios (id),
  CONSTRAINT fk_asistencias_equipo FOREIGN KEY (equipo_id, laboratorio_id) REFERENCES equipos (id, laboratorio_id),
  CONSTRAINT fk_asistencias_alumno FOREIGN KEY (alumno_id) REFERENCES alumnos (id),
  CONSTRAINT fk_asistencias_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_asistencias_curso FOREIGN KEY (curso_id) REFERENCES cursos (id),
  CONSTRAINT fk_asistencias_practica FOREIGN KEY (practica_id) REFERENCES practicas (id),
  CONSTRAINT fk_asistencias_solicitud FOREIGN KEY (solicitud_id) REFERENCES solicitudes_ingreso (id),
  CONSTRAINT ck_asistencias_horas CHECK (hora_salida IS NULL OR hora_salida >= hora_entrada),
  CONSTRAINT ck_asistencias_persona CHECK (
       (tipo_usuario = 'estudiante' AND alumno_id IS NOT NULL AND usuario_id IS NULL AND invitado_nombre IS NULL)
    OR (tipo_usuario = 'docente'    AND usuario_id IS NOT NULL AND alumno_id IS NULL AND invitado_nombre IS NULL)
    OR (tipo_usuario = 'invitado'   AND invitado_nombre IS NOT NULL AND alumno_id IS NULL AND usuario_id IS NULL)
  )
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Avisos y bitácora
-- ---------------------------------------------------------------------------

CREATE TABLE notificaciones (
  id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
  tipo            VARCHAR(50)  NOT NULL,
  titulo          VARCHAR(150) NOT NULL,
  mensaje         TEXT         DEFAULT NULL,
  laboratorio_id  INT UNSIGNED DEFAULT NULL,
  equipo_id       INT UNSIGNED DEFAULT NULL,
  prioridad       ENUM('baja','media','alta') NOT NULL DEFAULT 'media',
  leida           BOOLEAN      NOT NULL DEFAULT FALSE,
  creada_en       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_notificaciones_pendientes (leida, creada_en),
  KEY ix_notificaciones_laboratorio (laboratorio_id),
  KEY ix_notificaciones_equipo (equipo_id),
  CONSTRAINT fk_notificaciones_laboratorio FOREIGN KEY (laboratorio_id) REFERENCES laboratorios (id),
  CONSTRAINT fk_notificaciones_equipo FOREIGN KEY (equipo_id) REFERENCES equipos (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Quién hizo qué. Solo se inserta; nunca se edita ni se borra.
CREATE TABLE bitacora (
  id          BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  usuario_id  INT UNSIGNED DEFAULT NULL,
  accion      VARCHAR(100) NOT NULL,
  entidad     VARCHAR(50)  DEFAULT NULL,
  entidad_id  VARCHAR(50)  DEFAULT NULL,
  detalles    TEXT         DEFAULT NULL,
  fecha       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_bitacora_fecha (fecha),
  KEY ix_bitacora_usuario (usuario_id),
  KEY ix_bitacora_entidad (entidad, entidad_id),
  CONSTRAINT fk_bitacora_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Datos iniciales, tomados de la base recibida: el laboratorio "programacion" con 25 equipos.
-- El equipo "personal" de la base recibida no se crea: usar equipo propio se registra con equipo_id NULL.
-- No se crea ninguna cuenta aquí; el primer administrador se da de alta desde la aplicación.

INSERT INTO laboratorios (clave, nombre) VALUES ('programacion', 'Laboratorio de Programación');

INSERT INTO equipos (laboratorio_id, etiqueta)
SELECT l.id, e.etiqueta
FROM laboratorios l
JOIN (
  SELECT 'PC-1' AS etiqueta UNION ALL SELECT 'PC-2' UNION ALL SELECT 'PC-3' UNION ALL SELECT 'PC-4' UNION ALL SELECT 'PC-5'
  UNION ALL SELECT 'PC-6' UNION ALL SELECT 'PC-7' UNION ALL SELECT 'PC-8' UNION ALL SELECT 'PC-9' UNION ALL SELECT 'PC-10'
  UNION ALL SELECT 'PC-11' UNION ALL SELECT 'PC-12' UNION ALL SELECT 'PC-13' UNION ALL SELECT 'PC-14' UNION ALL SELECT 'PC-15'
  UNION ALL SELECT 'PC-16' UNION ALL SELECT 'PC-17' UNION ALL SELECT 'PC-18' UNION ALL SELECT 'PC-19' UNION ALL SELECT 'PC-20'
  UNION ALL SELECT 'PC-21' UNION ALL SELECT 'PC-22' UNION ALL SELECT 'PC-23' UNION ALL SELECT 'PC-24' UNION ALL SELECT 'PC-25'
) e
WHERE l.clave = 'programacion';

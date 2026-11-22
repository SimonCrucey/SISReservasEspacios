# Verificación de Práctica 5

Auditoría realizada el 22 de noviembre de 2026. Trabajo local, sin publicación.
La delegación original solicitó implementar RF-NOT-01 a 13 y RF-REP-01 a 05.
Este documento registra resultados comprobados, no una garantía de calificación.

## Orden y alcance

1. `be50c0a`: movimientos de doce servicios con contenido idéntico.
2. `779192d`: AGENTS.md y criterios antes de programar.
3. `5af5211`: notificaciones, preferencias y migración.
4. `08ba2ff`: cola, reclamos, reintentos y seguimiento.
5. `e176dcf`: reportes y agregaciones.

Los criterios preceden a todos los commits de implementación. Se conservaron
las etiquetas previas. El enunciado dice todos los Administradores; la misión
concreta que los destinatarios son los Administradores activos. Esta decisión
está explícita desde los criterios.

## Pruebas ejecutadas

- Línea base: 158 aprobadas y una falla intermitente de sincronización del test
  del despachador deshabilitado. No fue un fallo de SMTP ni de la cola.
- Suite final de desarrollo: 177 aprobadas, 0 fallidas y 0 omitidas.
- Build: 0 errores y 0 advertencias. Restauración correcta.
- EF Core: siete migraciones y sin cambios de modelo pendientes.
- Docker Compose validado con .env.example mediante config --quiet.

El test intermitente ahora espera la finalización de ExecuteTask antes de
detener el servicio. Conserva su aserción y la ausencia de acceso al DbContext.
Al probar reportes se detectó una consulta con OrderBy después de construir
el DTO que EF InMemory no traducía. Se ordenó por la clave de grupo antes de
proyectar. La corrección se verificó en InMemory y luego en SQL Server.

### Integración Docker desde copia de trabajo

Proyecto aislado `sisp5-79624ad1a2714e3ebc67e98a734a97e0`, con puertos y
volúmenes propios. Configuración ficticia creada desde .env.example, sin leer
el .env del proyecto principal.

Resultado: **170 comprobaciones aprobadas, 0 fallidas**, **134 peticiones HTTP**,
**38 combinaciones de ruta y método** probadas.

Se verificaron los seis frentes acumulados, los endpoints nuevos, aislamiento
de bandeja, lectura idempotente, preferencias, ausencia de correo opcional,
agregación real SQL, filtros, autorización, persistencia tras recrear API,
procesadores SQL concurrentes, ausencia de reenvío, y tres intentos hasta Fallido
con SMTP detenido. La cuarta ejecución no cambió el mensaje fallido.

La evidencia privada incluye resultados.json y cobertura.json en la carpeta
temporal del proyecto, sin cuerpos HTTP ni tokens. Los mensajes SMTP contienen
credenciales de un solo uso ficticias y no se publican como evidencia.
Los contenedores y volúmenes principales no se tocaron.

### Actualización de esquema existente

Base LocalDB nueva `SISReservas_P5_Upgrade_56cd830f57714ae29ed11963a42b8e57`.
Se aplicó primero el esquema de Práctica 4, se insertó un usuario ficticio y
un correo enviado ficticio, y después las dos migraciones nuevas.
Se comprobaron siete migraciones, el usuario con preferencia true y el correo
enviado conservado con cero intentos históricos. No se aplicaron cambios a bases
preexistentes.

## Trazabilidad de aceptación

| RF | Evidencia principal |
| --- | --- |
| NOT-01 | NotificacionesTests.CrearSolicitud_NotificaSoloAdministradoresActivos_YPersisteAmbosCanales |
| NOT-02 | NotificacionesTests.Resolver_NotificaResultadoYMotivo_UnaSolaVez |
| NOT-03 | NotificacionesTests.ConfirmacionReserva_NotificaPropietario_SinEventoEnTransicionInvalida |
| NOT-04/05/07 | NotificacionesTests.Bandeja_LecturaYPreferencia_SoloPropiasPersistentes; HTTP SQL |
| NOT-06 | Guardado conjunto en servicios; prueba GuardadoFallido_NoDejaSolicitudNiNotificacionesYNoSeFiltranEnReintento |
| NOT-08 | Evento completado con Mailpit apagado en Docker |
| NOT-09/12 | CLI y servicio hospedado; dos procesadores SQL y contador SMTP sin duplicación |
| NOT-10 | ColaCorreosTests.FalloConfiguracion_TresIntentosYEstadoFallido_NoReintentaIndefinidamente; fallo SMTP real |
| NOT-11 | AutorizacionPractica5Tests; GET /api/correos con Admin, Estándar y sin sesión |
| NOT-13 | Lectura de configuración existente; búsqueda de patrones en historial sin coincidencias |
| REP-01 | ReportesTests.Solicitudes_AgrupaEnRango_YAislaPropietarios; ejecución SQL |
| REP-02/03 | ReportesTests.Reservas_SumaHorasDeInterseccion_ExcluyeCanceladasYAjenas; ejecución SQL |
| REP-04/05 | ReportesTests.FechasInvalidasYUsuarioInactivo_Rechazados y pruebas de rangos/aislamiento; HTTP SQL |

## Límites explícitos

- Un fallo entre aceptación SMTP y guardado SQL puede producir repetición después
  de vencer el reclamo. El protocolo no ofrece una transacción conjunta con SQL.
- El correo fallido queda conservado. No se agregó una operación pública de reenvío.
- El canal opcional no desactiva activación ni recuperación.
- Los cuerpos de los correos permanecen en SQL para el despacho. Solo deben ser
  accesibles a operadores autorizados. El endpoint de cola no los expone.
- No se implementó frontend ni se modificaron las reglas de estados de Reservas.
- WeatherAPI real ya se verificó en Práctica 4; en esta regresión se comprobó
  su respuesta controlada sin clave, usando configuración ficticia.

## Evidencia para Asignación 3

Conservar la misión y los criterios originales, el orden real de commits, las
pruebas que detectaron fallos y las correcciones. No presentar esta auditoría
como una implementación de Asignación 3. No se delegó trabajo a subagentes.
Los datos privados, tokens y mensajes completos quedan fuera del repositorio.

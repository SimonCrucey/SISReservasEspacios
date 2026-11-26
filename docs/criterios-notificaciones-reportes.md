# Criterios de aceptación de Práctica 5

Definidos antes de implementar. Administradores destinatarios activos. Correo opcional habilitado por defecto. Activación y recuperación no dependen de esa preferencia. Máximo tres intentos totales, lotes de 20, reclamo exclusivo temporal SQL. SMTP tiene una ventana entre aceptación y guardado que impide garantizar entrega exactamente una vez.

## RF-NOT-01

- Dado Estándar y dos Administradores activos, más uno inactivo.
- Cuando crea solicitud válida.
- Entonces solo ambos Administradores activos reciben interna sin leer y correo; error no genera eventos.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-02

- Dado solicitud pendiente.
- Cuando otro Administrador aprueba o rechaza.
- Entonces solicitante recibe resultado y motivo del rechazo; segunda resolución 409 no notifica.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-03

- Dado reserva pendiente de usuario con permiso.
- Cuando Administrador confirma.
- Entonces propietario recibe confirmación; condición inválida no crea evento.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-04

- Dado dos usuarios con bandejas.
- Cuando GET /api/notificaciones.
- Entonces cada uno recibe solo su bandeja con leídas y no leídas, sin sesión 401.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-05

- Dado notificación propia.
- Cuando PATCH /api/notificaciones/{id}/leida.
- Entonces lectura idempotente con fecha conservada; ajena 404.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-06

- Dado preferencia de correo habilitada.
- Cuando evento válido.
- Entonces interna y correo Pendiente se guardan con negocio de forma consistente.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-07

- Dado usuario activo.
- Cuando PUT /api/notificaciones/preferencias con correoHabilitado=false y evento.
- Entonces solo interna, preferencia persistente propia; correo de activación/recuperación sigue encolado.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-08

- Dado SMTP apagado.
- Cuando evento de negocio.
- Entonces operación completa sin esperar SMTP y cola pendiente.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-09

- Dado correo pendiente.
- Cuando BackgroundService o CLI --procesar-correos.
- Entonces se procesa sin repetir negocio.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-10

- Dado SMTP inaccesible.
- Cuando tres ejecuciones del procesador.
- Entonces tres intentos totales y Fallido persistido con último error; cuarta no procesa.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-11

- Dado correos de varios estados.
- Cuando GET /api/correos.
- Entonces solo Administrador ve estado, intentos y fechas/error sanitizado, sin cuerpo ni tokens; Estándar 403.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-12

- Dado correo enviado y otro pendiente.
- Cuando dos ejecuciones consecutivas y dos procesadores concurrentes SQL.
- Entonces enviado no se reenvía y un solo procesador reclama cada pendiente; contar recepciones SMTP.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-NOT-13

- Dado variables SMTP ficticias.
- Cuando inspección de código e historial.
- Entonces credenciales solo por entorno/configuración segura, sin secretos impresos; nunca leer .env.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-REP-01

- Dado solicitudes en distintos estados y periodos.
- Cuando GET /api/reportes/solicitudes?inicio=UTC&fin=UTC.
- Entonces grupos por estado real con total, usando CreadaEn; Aplicada representa concesión efectiva.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-REP-02

- Dado varias reservas por espacio parcialmente solapadas.
- Cuando GET /api/reportes/reservas?inicio=UTC&fin=UTC.
- Entonces grupos por espacio/estado con conteo y horas de intersección [inicio,fin), excluye Cancelada; ordenar por horas y conteo.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-REP-03

- Dado varias filas de origen por grupo.
- Cuando ambos reportes.
- Entonces GroupBy/Count/Sum en SQL, menos filas en escenario de agrupación, sin materializar entidades previamente.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-REP-04

- Dado dos Estándar y Administrador.
- Cuando ambos reportes.
- Entonces Estándar solo propios, Administrador globales, sin sesión/revocada 401; servicio rechaza inactivo/rol inválido.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## RF-REP-05

- Dado datos en dos rangos.
- Cuando ambos reportes con rangos diferentes.
- Entonces métricas distintas según datos; vacío con totales cero; UTC obligatorio, inicio>=fin devuelve 400.
- Verificar con pruebas automatizadas y peticiones HTTP en SQL aislado. Comparar filas y respuestas con el estado previo, incluida la ausencia de efectos en rechazos.

## Regresión

Copia aislada con .env.example y Compose, registro y activación por cola, sesiones y 403, recuperación, permisos y transición inválida, reserva válida y prohibida, documento idéntico tras down/up sin -v. Verificar migración desde cero y actualización desde practica-4. No alterar datos principales.

## Ampliaciones tras auditoría — Asignación 3

Los criterios anteriores son los auditados de Práctica 5; se conservan. Estas ampliaciones responden a defectos reproducidos, no sustituyen sus requisitos.

### H-01: concurrencia de lectura (RF-NOT-05)

- Dada una notificación propia sin leer y dos contextos concurrentes, coordinar ambos intentos.
- Las dos operaciones autorizadas terminan correctamente y una sola establece LeidaEn; la primera fecha guardada permanece. Mantener rechazo de la notificación ajena.
- Verificar xUnit sin API y actualización condicional en SQL real.

### H-02: errores de dependencia en reportes (RD-08)

- Dada una consulta SQL que falla, incluso en Development, la respuesta es controlada (503 en indisponibilidad de persistencia), comprensible y sin traza, rutas, SQL ni cabeceras.
- Un error inesperado devuelve 500 genérico. No alterar los 400/401/403/404/409 de negocio existentes.
- Verificar ambos casos sin levantar API y repetir un timeout real en SQL aislado.

### H-03: separación verificable (RD-03)

- Retirar Reservas y sus extensiones en una copia de fuentes; Core compila y ejecuta permisos, bandeja y reporte de solicitudes sin API ni módulo de negocio.
- El host de composición se sustituye por un ejecutor mínimo, no por reglas de negocio falsas. No afirmar independencia solo porque compila el sistema completo.
- La separación conserva el modelo EF sin nuevas operaciones de migración, contratos y siete migraciones históricas.

### Evidencia y delegación

Cada corrección tiene prueba AAA nueva y commit que cita el hallazgo. Ejecutar esa misma prueba sobre el padre y exigir un fallo pertinente; en la versión final debe pasar. Registrar en bitácora por qué faltó esa comprobación y el cambio concreto a estos criterios/contexto.

## Revisión personal posterior — condiciones reforzadas

Las ampliaciones iniciales se conservan como historial; las siguientes precisan su aceptación definitiva.

- **H-01:** sobre SQL real y dos contextos, coordinar ambas escrituras sin pausas arbitrarias; las dos llamadas devuelven éxito y primera fecha/ticks persistidos permanecen. Ejecutor en Git, código anterior RED y corregido GREEN; xUnit principal sigue sin SQL/API.
- **H-02:** comprobar middleware registrado en el host real, respuesta sin token propio ni marcadores sintéticos, códigos previos de negocio intactos y reporte 200 después de revertir el fallo SQL.
- **H-03:** CoreOnly mantiene Program y siete migraciones; excluye todos los tipos de Reservas. Arranca en Docker y ejecuta Core real con SQL/Mailpit, sin servicios falsos ni CoreSmoke. El modelo parcial solo omite las dos tablas permitidas; cualquier diferencia adicional se rechaza. Repetir también el sistema completo con negocio.
- **Reproducibilidad:** perfil y scripts versionados, SQL nuevo y redes configurables. Una adaptación temporal no versionada ya no basta como procedimiento definitivo.
- **Trazabilidad:** distinguir instrucciones iniciales, correcciones iniciales, limitaciones detectadas por revisión personal e instrucciones posteriores. No atribuir al estudiante ejecuciones realizadas solo por el agente.

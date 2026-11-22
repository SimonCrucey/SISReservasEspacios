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

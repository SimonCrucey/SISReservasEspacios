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

## Clon Git local y cierre final

Se creó un segundo clon local limpio, sin depender de GitHub ni de archivos
ignorados del repositorio original, del commit de código
8038f09bd6e729b002813f68c4612dfbd1e28552.

- dotnet test restauró, compiló y pasó 177 pruebas, sin fallidas ni omitidas.
- El script versionado pasó **178 comprobaciones**, **139 peticiones HTTP** y
  **38 rutas/métodos**, sin fallos, en el proyecto Docker aislado
  sisp5-ba6051e407934bd490cd169f19d9fd0d.
- Se comprobó también el down/up posterior a las pruebas nuevas, conservando
  la notificación leída, la preferencia, el correo Fallido y los datos de reportes.
- La composición original del clon, con .env generado copiando .env.example,
  construyó la API y respondió HTTP 200 en Swagger con el auxiliar detenido.
  Solo se ajustaron el nombre del proyecto y el puerto para aislar la prueba.
- El origen del clon es el repositorio local. No es un clon de una publicación
  de Práctica 5 en GitHub.

El primer clon detectó un error propio de la validación MVC del DTO de
preferencias. Un atributo Required asociado a la propiedad de un record
posicional provocaba 500. El commit 8038f09 lo reemplazó por una clase de entrada
con propiedad nullable obligatoria. La regresión HTTP comprobó cuerpo vacío 400,
false 200 y true 200. El clon anterior y sus evidencias se conservaron.

El cierre inicial posterior a 8038f09 solo documentó estos resultados. La etiqueta
local de entrega se preparó después de comprobarlos, sin publicar referencias.
La ventana SMTP/SQL descrita arriba permanece como limitación técnica explícita,
no como garantía de entrega exactamente una vez.

## Corrección H-01: precisión de las horas reservadas (2026-11-22)

La fórmula anterior sumaba `DATEDIFF(second, inicio, fin) / 3600.0` después
de recortar cada reserva al rango consultado. DATEDIFF cuenta límites de segundo,
no la duración real: una reserva entre 00:00:00.9000001 y 00:00:01.1000004
aportaba un segundo en lugar de 0.2000003 segundos.

La fórmula corregida recupera los ticks de 100 ns de `datetime2(7)`:

```text
ticks = DATEDIFF(second, inicio, fin) * 10000000
      + (fin.Millisecond - inicio.Millisecond) * 10000
      + (fin.Microsecond - inicio.Microsecond) * 10
      + (fin.Nanosecond - inicio.Nanosecond) / 100
horas = double(SUM(decimal(ticks))) / 36000000000
```

EF Core traduce los componentes de fracción a DATEPART en SQL Server.
La agrupación y la suma permanecen en SQL; la suma decimal evita desbordar
bigint al acumular intervalos. Se conservan intersecciones, filtros de usuario,
fechas, estados y grupos. La salida double mantiene su redondeo binario habitual.
InMemory calcula los mismos ticks mediante DateTime.Ticks; no demuestra por sí
solo la corrección SQL.

Evidencia antes/después con la misma prueba HTTP y SQL Server real:

| Caso | Antes | Después |
| --- | --- | --- |
| Intervalo completo de 0.2000003 s | 1 s: FAIL | 0.2000003 s: PASS |
| Intersección cruzando segundo | No alcanzado tras el fallo anterior | 0.1000001 s: PASS |
| Intersección dentro del mismo segundo | No alcanzado tras el fallo anterior | 0.0300001 s: PASS |
| Suma de dos reservas en un grupo | No alcanzado tras el fallo anterior | 0.4000006 s: PASS |

- La ejecución anterior falló por la aserción H-01, con HTTP 200 y sin errores
  de compilación, en el proyecto `sisp5-1f937b643b984a47a0aa12bfcf950065`.
- La ejecución corregida pasó **188 comprobaciones, 145 peticiones HTTP y
  38 rutas/métodos**, sin fallos, en `sisp5-7829f8f4e5cd410d833f1946f3452ce0`.
  También comprobó horas completas, filtros, rangos sin resultados, concurrencia,
  reintentos SMTP y persistencia tras down/up. Se usaron datos ficticios y
  volúmenes propios; no se modificaron las bases ni los volúmenes del proyecto principal.
- Los resultados JSON privados de ambas ejecuciones se conservaron fuera de Git.
  Para repetir la regresión: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/integracion/Validar-Practica5.ps1`.
- Se añadieron seis casos AAA a `ReportesTests.Reservas_ConservaPrecisionDeTicks_EnIntersecciones`:
  fracciones, intersecciones, un tick, cruce de medianoche y máximo de 3660 días.
- `dotnet build SISReservasEspacios.slnx`: **0 errores, 0 advertencias**.
- `dotnet test SISReservasEspacios.slnx --no-build`: **183 aprobadas, 0 fallidas,
  0 omitidas**. Las pruebas existentes de rangos vacíos permanecen aprobadas.
- `dotnet ef migrations has-pending-model-changes --project backend/SISReservas.Api --no-build`:
  sin cambios de modelo pendientes. No se modificaron migraciones.

Esta intervención corrige únicamente H-01. No corrige ni da por resueltos H-02,
H-03 o H-04. La etiqueta practica-5 se actualiza únicamente en local tras validar;
no se publica esta corrección.

# Auditoría de Asignación 3
## Historial definitivo consolidado

La rama `entrega-asignacion-3` parte de `practica-5` (`0b887de`). Conserva fuera de esta rama la sesión histórica en `trabajo-asignacion-3` y sus respaldos. Las referencias antiguas que aparecen en las secciones históricas identifican lo que se ejecutó entonces; no son los commits definitivos de evaluación.

| Hallazgo | Commit definitivo | Padre directo |
| --- | --- | --- |
| H-01 | `fcc384638f6f02ae33a9314805e9fc8461967e1c` | `0b887deaa22e61de44ca91b2e83e99d2feadb033` |
| H-02 | `158b75324606c1c60db4aeb09bee8656bb11f676` | `fcc384638f6f02ae33a9314805e9fc8461967e1c` |
| H-03 | `35aedc040ee7a549baccc85f0e19b25efb21427b` | `158b75324606c1c60db4aeb09bee8656bb11f676` |

Los refuerzos posteriores están incluidos en el mismo commit de cada hallazgo. El cuarto commit reúne documentación, prevención y verificación transversal; no corrige funcionalidades adicionales. El verificador ejecuta la misma prueba del commit de corrección sobre su padre, sobre ese commit y sobre el candidato final.

`asignacion-3` sigue en `e8c18c4` por instrucción del estudiante; no se ha publicado ni actualizado. Para revisar esta entrega usar HEAD de `entrega-asignacion-3`, no la etiqueta antigua.


Fecha: 26 de noviembre de 2026. Base auditada: `practica-5`, `0b887deaa22e61de44ca91b2e83e99d2feadb033`. Las numeraciones H-01 a H-03 de este documento pertenecen a Asignación 3; el H-01 histórico de precisión de horas ya estaba corregido en Práctica 5 y no se presenta otra vez como defecto.

## Referencias y alcance

- Enunciado de Asignación 3 y definiciones oficiales RD proporcionados por el estudiante en esta sesión.
- [Criterios originales y ampliaciones](criterios-notificaciones-reportes.md), [contexto](../AGENTS.md), [verificación histórica](verificacion-practica-5.md).
- RD-02: reglas fuera de presentación; RD-03: Core construible y ejecutable sin negocio; RD-06: autorización en servidor; RD-07: rechazo controlado de entradas; RD-08: sin trazas, rutas ni consultas visibles; RD-10: credenciales por entorno, incluido historial; RD-11: criterio temporal uniforme. RD-12 exige pruebas sin levantar toda la aplicación.

Se creó el worktree independiente `trabajo-asignacion-3`. No se modificó el trabajo local de frontend, main, etiquetas anteriores, bases ni volúmenes de desarrollo. Las pruebas usaron SQL Server y Mailpit de proyectos Docker nuevos, con datos ficticios y SMTP interno. Se preservaron sus volúmenes; no se ejecutó `down -v`.

## Hallazgos confirmados

### H-01 — Fecha de lectura sobrescrita bajo concurrencia (media)

**Requisito:** RF-NOT-05 y criterio «lectura idempotente con fecha conservada».

**Localización anterior:** `Services/Notificaciones/NotificacionService.cs`, `LeerAsync`, líneas 59–69 de la base. Consultaba `LeidaEn == null` y después guardaba sin condición atómica.

**Reproducción real:** dos contextos SQL y dos llamadas al servicio original se detuvieron antes de guardar. Ambas leyeron NULL. Tras liberar primero una y luego otra, ambas devolvieron éxito; SQL pasó de `2026-11-26T19:32:48.6022003` a `2026-11-26T19:32:48.7202110`. Es una pérdida de idempotencia, no un desplazamiento de zona horaria. La reproducción usó interceptores de EF para coordinar operaciones reales, sin modificar el servicio ni el esquema.

**Prueba nueva:** `LecturaConcurrenteTests.H01_DosLecturasConcurrentes_ConservanUnaSolaFechaPersistida`. AAA, dos contextos y un guardado coordinado, sin API. Antes: fallo de aserción, esperado 1 guardado, obtenido 2. Después: pasa.

**Corrección inicial histórica:** `750240acc1b9c3e8067317b265064dd0640d9a81`. UPDATE SQL condicionado por propietario y `LeidaEn IS NULL`; una lectura repetida reconoce la fila sin sobrescribir su fecha. Para InMemory, que no implementa ExecuteUpdate, exclusión y recarga del estado. No se añadió migración. SQL real corregido conservó exactamente `2026-11-26T19:51:42.8100322` en ambas operaciones.

**Prevención:** exigir concurrencia, además de la prueba secuencial, en RF-NOT-05.

### H-02 — Fallos de reportes exponen detalles internos (media)

**Requisito:** RD-08; omisión de fallos de infraestructura en los criterios RF-REP-01 a 05.

**Localización anterior:** `Controllers/ReportesController.cs`, `Ejecutar`, líneas 23–28; Program carecía de frontera común de errores. Solo se controlaban ArgumentException y UnauthorizedAccessException.

**Reproducción HTTP real:** sobre la nueva base de auditoría se mantuvo un bloqueo de esquema de Reservas durante 40 segundos dentro de una transacción revertida. Un usuario ficticio autenticado consultó `/api/reportes/reservas?inicio=2026-01-01T00:00:00Z&fin=2026-01-02T00:00:00Z`. Tras 30,05 segundos, la API Development devolvió 500 con SqlException, traza, ruta `/src/` y el valor de su propia cabecera de sesión. Solo se conservaron indicadores booleanos; no se guardó el cuerpo ni el token. El bloqueo se revirtió; se comprobó que no quedó la columna temporal. Una prueba previa con bloqueo de filas no provocó timeout por el versionado de lecturas: no se cuenta como defecto.

**Prueba nueva:** `ErroresControladosTests.H02_Reportes_ErrorInterno_NoExponeTrazaRutaConsultaNiCabecera`, dos casos AAA sin servidor. Invoca controlador y servicio reales con una consulta que falla; también comprueba el error inesperado general. Antes: dos fallos por excepciones sin controlar. Después: 503 para persistencia y 500 genérico para otros fallos, sin detalles.

**Corrección inicial histórica:** `012805bf7852631c6cd0fa4be9f078dbb3690293`. Middleware común registrado antes de endpoints; preserva los rechazos 400/401/403/404/409 existentes. Registra tipo e identificador de correlación, no la excepción ni cabeceras. No se modifica autenticación. Repetición SQL real: 503 a los 30,08 segundos, sin SqlException, traza, ruta ni token. El reporte normal sigue devolviendo 200 y los rangos iguales/invertidos 400.

**Reproducción automática sin secretos:** desde la raíz, ejecutar `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Verificar-RegresionesAsignacion3.ps1 -Hallazgo H-02`. Exporta el padre y ejecuta la misma prueba; no toca bases ni levanta API.

Para repetir el caso HTTP, usar únicamente un proyecto Compose desechable con cuentas ficticias preparadas conforme al README. En otra terminal de ese proyecto, ejecutar `docker compose exec -T sqlserver sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -d SISReservasEspacios -Q "$1"' sh "BEGIN TRAN; ALTER TABLE dbo.Reservas ADD [A3BloqueoTemporal] int NULL; WAITFOR DELAY '00:00:40'; ROLLBACK;"` y consultar el reporte durante la espera. Nunca ejecutar esta instrumentación en desarrollo compartido ni producción. No requiere editar migraciones. El script de regresión es el recorrido recomendado para evaluar el defecto sin modificar datos.

### H-03 — Core acoplado a Reservas (media)

**Requisito:** RD-03. Los criterios originales de reportes no comprobaban retirar el módulo.

**Localización anterior:** DbSets/configuración de Reservas en `Data/SISReservasDbContext.cs:19–20`; navegación en `Models/Usuario.cs:33`; `DTOs/Reportes/ReporteDtos.cs:7`; reporte Core y reporte de negocio en la misma implementación. La mezcla de reportes fue introducida en `e176dcf`; parte del acoplamiento de entidades era anterior a Práctica 5. No se atribuye todo a ese commit.

**Reproducción:** copia temporal de fuentes, excluyendo Reservas y el host de composición. El Core no compiló: CS0246 en Espacio, Reserva y EstadoReserva. La aplicación completa sí compilaba; eso no demostraba RD-03.

**Prueba nueva:** `IndependenciaCoreTests.H03_CoreSinModuloReservas_CompilaYEjecutaPermisosNotificacionesYReporteCore`. Exporta fuentes públicas, retira los archivos de negocio y sustituye el host por un ejecutor mínimo InMemory. Antes: falla por esos errores de compilación. Después: compila y ejecuta las tres piezas Core, sin API, SQL ni negocio. Se conserva la copia y su log en una carpeta temporal identificable. Requiere SDK 10 y paquetes NuGet, como la suite. No pretende levantar el host completo sin cambiar su composición.

**Corrección inicial histórica:** `85521d7128e6b09f96b45bf1df3e5a606a150fee`. Las extensiones de DbContext, Usuario, DTO, servicio y controlador de reportes propias de Reservas están en archivos parciales de su módulo. El Core tiene un punto de configuración parcial opcional, sin depender de tipos de Reservas. Se mantienen nombres públicos, rutas y modelo EF. No se creó una arquitectura paralela ni se modificaron migraciones.

**Protección adicional:** `H03_SeparacionDeReservas_NoCambiaModeloDeMigraciones` comprueba `HasPendingModelChanges() == false`, sin abrir conexión.

## Reproducción por el profesor

Desde la raíz de la versión corregida:

```powershell
dotnet test SISReservasEspacios.slnx
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Verificar-RegresionesAsignacion3.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Validar-Practica5.ps1
```

El segundo comando obtiene cada commit `fix H-NN`, exporta su padre, incorpora únicamente su nueva prueba y exige que falle allí y pase en la versión actual. No cambia ramas ni etiquetas. El tercero crea recursos Docker propios y comprueba los seis frentes acumulados. Sus evidencias quedan en Temp y no deben publicarse completas: la entrega contiene solo resúmenes sanitizados.

## Cobertura de los requisitos auditados

| Requisito | Evidencia y resultado |
| --- | --- |
| RF-NOT-01 | xUnit y SQL: dos administradores activos notificados; inactivo excluido. |
| RF-NOT-02 | Resolución válida notifica; segunda resolución rechazada sin nuevo evento. |
| RF-NOT-03 | Confirmación válida notifica al propietario; transición inválida no genera evento. |
| RF-NOT-04 | Bandejas propias; sin sesión 401; notificación ajena no accesible. |
| RF-NOT-05 | Secuencial correcto; concurrencia defectuosa H-01, corregida y protegida. |
| RF-NOT-06 | Notificación interna y correo se preparan con el guardado del negocio. |
| RF-NOT-07 | Preferencia persistente: conserva interna y omite correo opcional; seguridad independiente. |
| RF-NOT-08 | SMTP detenido no impide completar negocio ni encolar. |
| RF-NOT-09 | Procesador hospedado y CLI independientes de la operación originadora. |
| RF-NOT-10 | SQL: tres intentos y Fallido; cuarto ciclo no reprocesa. |
| RF-NOT-11 | Admin autorizado, Estándar 403; cola sin cuerpos/tokens; página fuera de rango 400. |
| RF-NOT-12 | Procesadores concurrentes SQL: una recepción; enviado no se reenvía. |
| RF-NOT-13 | Configuración por entorno; revisión heurística de 221 blobs públicos del historial de la base, sin candidatos de claves privadas y sin .env privado versionado. No certifica ausencia absoluta. |
| RF-REP-01 | Agrupación por estado, período y propietario; Admin global. |
| RF-REP-02 | Conteos y horas de intersección; fracciones y ventanas parciales verificadas en SQL. |
| RF-REP-03 | GroupBy/Count/Sum antes de materializar; no se suman entidades de origen en memoria. |
| RF-REP-04 | Usuario propio, Admin global, sesión requerida; H-02 afecta errores, no filtrado. |
| RF-REP-05 | Vacío cero; rango igual/invertido o sin UTC rechazado; límites [inicio, fin). |

RD-02: no se confirmó una infracción en las reglas auditadas, ubicadas en servicios. RD-03: H-03. RD-06: no se logró acceso cruzado en los recorridos ejecutados. RD-07: entradas inválidas comprobadas con rechazo controlado; no se halló desbordamiento de paginación porque ya existe límite 100000. RD-08: H-02. RD-10: alcance heurístico indicado arriba, sin leer archivos privados. RD-11: fechas SQL conservan ticks y JSON declara Z en correos, permisos, reservas, notificaciones y reportes.

## Casos que no son hallazgos

- Sin administradores activos: solicitud creada, cero destinatarios; no hay requisito que imponga un destinatario inexistente.
- Dos solicitudes concurrentes distintas, aunque del mismo permiso: dos solicitudes guardadas. Los criterios no prohíben ese caso; no se presenta como duplicación del mismo evento.
- Correo mal formado: Pendiente(1), Pendiente(2), Fallido(3); cuarto ciclo sigue en 3. Un correo válido del mismo lote se envía una vez. Es comportamiento correcto.
- Ventana entre aceptación SMTP y persistencia: limitación explícita original; no se promete entrega exactamente una vez ante caída del proceso.
- Recepción SMTP comprobada solo en Mailpit, no buzones externos. WeatherAPI real no se repitió en esta asignación.

## Validación y límites

La base pasó nuevamente 207 xUnit y 243 comprobaciones de integración, con 172 peticiones HTTP, 38 rutas/métodos y cinco ciclos persistentes. La versión corregida pasó 212 xUnit (0 fallidas/omitidas), compilación sin errores/advertencias y nuevamente 243 comprobaciones integrales (0 fallidas), 172 peticiones, 38 rutas/métodos y cinco ciclos. El verificador de padres confirmó RED/GREEN para los tres hallazgos. Resultados finales en [evidencia sanitizada](evidencias/asignacion-3/resultados.json). Las copias operativas del script de integración solo recibieron una subred explícita disponible por agotamiento de los pools Docker locales y la ruta al worktree; no se cambiaron comprobaciones funcionales del script versionado.

Los tres hallazgos están corregidos localmente. La evaluación final del profesor no puede garantizarse. La publicación queda pendiente de autorización; ninguna etiqueta anterior se modifica.

## Alcance definitivo tras revisión personal

Los apartados anteriores describen la auditoría y correcciones iniciales. Su límite de H-03 (CoreSmoke sin Program/migraciones) y el ejecutor SQL temporal de H-01 han sido reemplazados por procedimientos versionados más fuertes. La [revisión posterior](revision-personal-asignacion-3.md) documenta la decisión del estudiante y la implementación: SQL coordinado sin API; pipeline HTTP con recuperación; host CoreOnly real, siete migraciones y servicios Core reales sin tipos de Reservas. El perfil original completo continúa funcionando.

La etiqueta asignacion-3 no se ha movido: los refuerzos son candidatos posteriores, pendientes de consolidación autorizada. Los resultados iniciales de 212/243 no se deben confundir con los nuevos resultados de revisión registrados por separado.

### Cierre de consolidación

El estudiante autorizó crear una rama nueva y cuatro commits, manteniendo intactos trabajo-asignacion-3, main, frontend, practica-5, respaldos y asignacion-3. La consolidación se realizó sobre la nueva rama; las referencias a «pendiente de consolidar» en la cronología anterior describen decisiones previas, ya cumplidas aquí. Solo sigue pendiente la autorización de etiqueta/publicación. La cronología no atribuye al estudiante pruebas ejecutadas únicamente por el agente.

Para inspeccionar los cuatro commits: `git log --oneline practica-5..HEAD`. Para comprobar equivalencia funcional con la sesión aprobada: `git diff bd81a59 HEAD -- backend docker-compose.core.yml docker-compose.red-pruebas.yml tests/SISReservas.Api.Tests tests/integracion/SqlConcurrencyProbe tests/integracion/Validar-Practica5.ps1 tests/integracion/Validar-H01Sql.ps1` (vacío). Los cambios adicionales se limitan a documentación/evidencia y al verificador de trazabilidad.

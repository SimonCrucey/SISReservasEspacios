# Revisión personal posterior — Asignación 3
## Historial definitivo consolidado

La rama `entrega-asignacion-3` parte de `practica-5` (`0b887de`). Conserva fuera de esta rama la sesión histórica en `trabajo-asignacion-3` y sus respaldos. Las referencias antiguas que aparecen en las secciones históricas identifican lo que se ejecutó entonces; no son los commits definitivos de evaluación.

| Hallazgo | Commit definitivo | Padre directo |
| --- | --- | --- |
| H-01 | `fcc384638f6f02ae33a9314805e9fc8461967e1c` | `0b887deaa22e61de44ca91b2e83e99d2feadb033` |
| H-02 | `158b75324606c1c60db4aeb09bee8656bb11f676` | `fcc384638f6f02ae33a9314805e9fc8461967e1c` |
| H-03 | `35aedc040ee7a549baccc85f0e19b25efb21427b` | `158b75324606c1c60db4aeb09bee8656bb11f676` |

Los refuerzos posteriores están incluidos en el mismo commit de cada hallazgo. El cuarto commit reúne documentación, prevención y verificación transversal; no corrige funcionalidades adicionales. El verificador ejecuta la misma prueba del commit de corrección sobre su padre, sobre ese commit y sobre el candidato final.

`asignacion-3` sigue en `e8c18c4` por instrucción del estudiante; no se ha publicado ni actualizado. Para revisar esta entrega usar HEAD de `entrega-asignacion-3`, no la etiqueta antigua.


## Decisión del estudiante

El estudiante aceptó los tres hallazgos, pero no autorizó publicación ni reescritura. Solicitó SQL real versionado para H-01, pipeline y recuperación para H-02 y evidencia más fuerte de RD-03. Posteriormente rechazó una separación general en múltiples proyectos y autorizó investigar una solución modular dentro de SISReservas.Api.csproj. No se afirma que haya ejecutado las nuevas comprobaciones: son ejecuciones del agente pendientes de su revisión.

## H-03: alternativa mínima seleccionada

Se conserva un único proyecto. No se crearon ensamblados Core/Reservas separados, servicios falsos ni un Program alternativo.

| Frontera | Responsabilidad real |
| --- | --- |
| Core | Acceso, permisos, documentos, notificaciones, cola y reporte de solicitudes. |
| Reservas | Entidades/estados, servicios, endpoints, planificación exterior y reporte de reservas. |
| Host | Program.cs real: construye ASP.NET, sesiones/roles, Swagger y middleware. BusinessModule declara puntos parciales opcionales sin referencias a tipos de Reservas. |
| Persistencia | DbContext de ejecución y siete migraciones históricas. El esquema completo conserva tablas aunque una aplicación use solo Core. |

Se evaluaron: separar ensamblados (descartado por alcance), ocultar rutas por configuración (insuficiente), mantener el CoreSmoke anterior (insuficiente) y compilación modular real (seleccionada).

`Configuration=CoreOnly` excluye realmente las fuentes de Reservas, incluidas sus extensiones de registro e inicialización. Mantiene Program.cs y todas las migraciones. La prueba xUnit publica ese ejecutable y comprueba que faltan los tipos Reserva/Espacio/ReservaService/PlanificacionService, conservando Program y servicios Core. El script HTTP arranca exactamente ese host con SQL Server y Mailpit.

### Migraciones sin borrar negocio ni ignorar inconsistencias

El modelo Core no incluye dos tablas que sí pertenecen al esquema histórico. No se ejecuta una migración que las elimine. `CoreSchemaMigration` compara el modelo Core con el snapshot: solo admite exactamente la omisión de Espacios y Reservas; cualquier otra operación de diferencia provoca un error. Dos pruebas cubren admisión y rechazo. No se ignoran advertencias EF ni se modifica el snapshot.

Tras validar la vista parcial, un contexto de migración del mismo tipo usa el modelo histórico completo para aplicar las siete migraciones. El contexto de peticiones sigue usando las entidades Core reales. La aplicación completa conserva su arranque y comparación de modelo habituales. Retener tablas históricas no introduce dependencia del código de Reservas en Core.

El piloto y la prueba definitiva arrancaron desde SQL vacío, aplicaron las siete migraciones y ejecutaron el Core real. La verificación definitiva pasó 87 comprobaciones y 62 peticiones HTTP: registro/activación SMTP, login/logout, 403, administración, permisos/aprobación/rechazo/409, notificaciones propias/404/lectura/preferencias, reporte SQL, documentos/descarga ajena rechazada, recuperación SMTP con correo opcional deshabilitado y persistencia tras down/up. Swagger no publica Reservas ni su reporte. No se sustituyó ningún servicio Core.

## H-01: SQL reproducible y versionado

Se conserva LecturaConcurrenteTests. `tests/integracion/SqlConcurrencyProbe` utiliza dos DbContext y dos interceptores coordinados por TaskCompletionSource. Detiene cada UPDATE antes de enviarlo, libera el primero, lee la fecha persistida, libera el segundo y vuelve a leer. Ambas operaciones deben ser exitosas y los ticks deben permanecer iguales. No usa Sleep/Delay para forzar la carrera; los límites de espera solo evitan bloqueos infinitos.

`Validar-H01Sql.ps1` exporta las referencias anterior/corregida, añade el mismo ejecutor de prueba y corre cada versión en un contenedor SDK sobre SQL Server aislado, sin levantar API. Exige resultado JSON y código 1 con fecha cambiada antes; código 0 con fecha conservada después. Un error de compilación no cuenta como RED. Exige catálogos nuevos SISAsignacion3_GUID y siete migraciones; nunca los borra.

La primera ejecución versionada comprobó RED/GREEN. La referencia anterior era la etiqueta anotada de Práctica 5; se precisó después el registro para resolver siempre su commit y no el objeto de etiqueta. El código archivado de esa ejecución sí era el commit correcto.

## H-02: contexto, pipeline y recuperación

La prueba unitaria ahora coloca cabeceras sintéticas y comprueba que sus valores no aparezcan. La prueba Docker provoca un bloqueo de esquema en una transacción revertida y observa el pipeline real: exige 503 y ausencia del token de la cuenta ficticia, marcador sintético, SQL, traza y rutas. Espera la reversión y consulta de nuevo con 200. Así, retirar el registro del middleware hace fallar la comprobación HTTP, aunque la clase aún exista.

CoreOnly ejecutó ese caso sobre solicitudes con resultado satisfactorio. El recorrido completo lo repite sobre reservas y conserva los 400/401/403/404/409 anteriores. No se añaden endpoints de diagnóstico ni se cambia autenticación.

## Reproducción desde la raíz de un clon candidato

Requisitos: SDK 10 para xUnit, Git, Windows PowerShell 5.1+ y Docker Desktop Linux para integración. Ningún comando utiliza el .env privado del operador.

```powershell
dotnet test SISReservasEspacios.slnx
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Verificar-RegresionesAsignacion3.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Validar-H01Sql.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Validar-Practica5.ps1 -CoreOnly
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Validar-Practica5.ps1
```

Si Docker no dispone de un pool libre, los últimos tres comandos admiten `-SubredDocker CIDR`. El operador debe elegir una subred libre diferente para proyectos simultáneos; no existe una subred fija obligatoria. Por ejemplo, usar una red libre de laboratorio no solapada; comprobar primero `docker network ls` e inspeccionar sus subredes. No borrar redes ajenas.

Arranque manual del host sin negocio, también con un proyecto Docker propio:

```powershell
$env:APP_PORT='15143'
docker compose -p revision-core --env-file .env.example -f docker-compose.yml -f docker-compose.core.yml up --build
```

Si hace falta red explícita, definir `DOCKER_TEST_SUBNET` con un CIDR libre y añadir `-f docker-compose.red-pruebas.yml`. No aplicar ese overlay sin elegir una red. La API usa 127.0.0.1:15143/swagger; SQL/Mailpit siguen internos. Consultar README para inspección SMTP. `docker compose ... down` conserva datos; nunca usar `-v`.

## Estado histórico anterior a la consolidación

El respaldo `codex/respaldo-asignacion3-pre-revision-personal` y la etiqueta asignacion-3 conservan e8c18c4. Las mejoras son commits nuevos, no sustituyen los anteriores. Por eso este historial de revisión aún no es la presentación final de un solo commit por hallazgo.

Propuesta, solo tras autorización: crear una rama candidata desde practica-5, incorporar infraestructura de pruebas por separado, reunir corrección inicial y refuerzos en un commit H-01, otro H-02 y otro H-03, y añadir documentación con los nuevos hashes. Repetir RED/GREEN en sus padres y toda la validación. No modificar referencias antiguas; mover asignacion-3 solo con autorización expresa posterior. No se ha ejecutado push.

## Resultados de esta revisión

- xUnit: **214 aprobadas, 0 fallidas, 0 omitidas**.
- H-01 SQL versionado: RED en la base y GREEN en la corregida; ambas llamadas exitosas y ticks comparados directamente. Siete migraciones en ambos catálogos nuevos.
- CoreOnly real: **87 comprobaciones, 62 peticiones HTTP, todas aprobadas**; siete migraciones, recuperación SMTP y persistencia sin negocio compilado.
- Sistema completo: **248 comprobaciones, 173 peticiones HTTP y 38 rutas/métodos, todas aprobadas**; cinco ciclos y seis frentes acumulados.
- RED/GREEN reforzado de los tres hallazgos: aprobado.

La [evidencia resumida](evidencias/asignacion-3/revision-personal.json) contiene únicamente conteos y booleanos; no copia registros privados. Los resultados iniciales permanecen como históricos. No se garantiza una calificación. La estructura final de un commit por hallazgo y la etiqueta actualizada siguen pendientes de consolidación autorizada.

### Cierre de consolidación

El estudiante autorizó crear una rama nueva y cuatro commits, manteniendo intactos trabajo-asignacion-3, main, frontend, practica-5, respaldos y asignacion-3. La consolidación se realizó sobre la nueva rama; las referencias a «pendiente de consolidar» en la cronología anterior describen decisiones previas, ya cumplidas aquí. Solo sigue pendiente la autorización de etiqueta/publicación. La cronología no atribuye al estudiante pruebas ejecutadas únicamente por el agente.

Para inspeccionar los cuatro commits: `git log --oneline practica-5..HEAD`. Para comprobar equivalencia funcional con la sesión aprobada: `git diff bd81a59 HEAD -- backend docker-compose.core.yml docker-compose.red-pruebas.yml tests/SISReservas.Api.Tests tests/integracion/SqlConcurrencyProbe tests/integracion/Validar-Practica5.ps1 tests/integracion/Validar-H01Sql.ps1` (vacío). Los cambios adicionales se limitan a documentación/evidencia y al verificador de trazabilidad.

## Rúbrica de la rama consolidada

| Apartado oficial | Máximo | Evidencia |
| --- | --- | --- |
| Auditoría contra criterios | 1.5 | Tres hallazgos reproducidos: notificaciones H-01, reportes H-02 y RD-03 H-03; requisito, gravedad y reproducción documentados. |
| Correcciones | 1.5 | Un commit definitivo por hallazgo; Core real, sistema completo, seis frentes y persistencia verificados. |
| Regresión | 1.0 | Misma prueba RED en padre, GREEN en corrección exacta y candidato; H-01 también SQL versionado; suite única 214 aprobadas. |
| Bitácora | 1.0 | Instrucciones originales, causas de omisión, revisión personal, correcciones y prevención visible en AGENTS/criterios. |

La rama consolidada volvió a pasar 214 xUnit, 248 integrales/173 HTTP y 87 CoreOnly/62 HTTP. [Resumen de entrega](evidencias/asignacion-3/entrega-consolidada.json). Los JSON históricos de esta rama omiten identificadores de filas y texto libre por minimización; los originales intactos permanecen en trabajo-asignacion-3 y sus respaldos. Se conservan cronología, cifras, fechas y referencias de código. La evaluación académica final corresponde al profesor.

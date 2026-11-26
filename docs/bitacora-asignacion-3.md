# Bitácora de delegación — Asignación 3
## Historial definitivo consolidado

La rama `entrega-asignacion-3` parte de `practica-5` (`0b887de`). Conserva fuera de esta rama la sesión histórica en `trabajo-asignacion-3` y sus respaldos. Las referencias antiguas que aparecen en las secciones históricas identifican lo que se ejecutó entonces; no son los commits definitivos de evaluación.

| Hallazgo | Commit definitivo | Padre directo |
| --- | --- | --- |
| H-01 | `fcc384638f6f02ae33a9314805e9fc8461967e1c` | `0b887deaa22e61de44ca91b2e83e99d2feadb033` |
| H-02 | `158b75324606c1c60db4aeb09bee8656bb11f676` | `fcc384638f6f02ae33a9314805e9fc8461967e1c` |
| H-03 | `35aedc040ee7a549baccc85f0e19b25efb21427b` | `158b75324606c1c60db4aeb09bee8656bb11f676` |

Los refuerzos posteriores están incluidos en el mismo commit de cada hallazgo. El cuarto commit reúne documentación, prevención y verificación transversal; no corrige funcionalidades adicionales. El verificador ejecuta la misma prueba del commit de corrección sobre su padre, sobre ese commit y sobre el candidato final.

`asignacion-3` sigue en `e8c18c4` por instrucción del estudiante; no se ha publicado ni actualizado. Para revisar esta entrega usar HEAD de `entrega-asignacion-3`, no la etiqueta antigua.


Fecha: 26/11/2026. Base: `practica-5` (`0b887de`). Esta bitácora registra fallos reales del código entregado, no una sesión en que todo salió bien. Se apoya en criterios, commits, pruebas y ejecución de esta sesión; no inventa instrucciones históricas ni exporta conversaciones privadas.

## H-01 — Lectura concurrente (media)

- **Qué se pidió:** RF-NOT-05 de los criterios de Práctica 5: «lectura idempotente con fecha conservada; ajena 404».
- **Qué entregó el agente:** lectura de fila y SaveChanges separado. `NotificacionService.LeerAsync` en `0b887de`. La prueba anterior `Bandeja_LecturaYPreferencia_SoloPropiasPersistentes` verificaba lecturas consecutivas. Dos contextos SQL reprodujeron dos fechas persistidas distintas; ver [auditoría](auditoria-asignacion-3.md#h-01--fecha-de-lectura-sobrescrita-bajo-concurrencia-media).
- **Por qué pasó:** el criterio ya decía idempotencia, pero la prueba y revisión eran secuenciales; no se coordinó la carrera entre lectura y escritura. Es revisión insuficiente, no ausencia total del criterio.
- **Corrección inicial histórica:** realizada por el agente en esta sesión, commit `750240acc1b9c3e8067317b265064dd0640d9a81`. El estudiante autorizó: «tienes permiso para todo menos para publicar o dañar piezas o códigos que ya funcionan». Se tradujo en preservar contratos y sustituir la escritura por un UPDATE condicional. La prueba H01 falla antes (2 guardados) y pasa después (1); también se verificó SQL real.
- **Qué cambia:** ampliación RF-NOT-05 y apartado de Asignación 3 en AGENTS: coordinar dos lectores y conservar primera fecha; exigir atomicidad real y no solo repetición secuencial.

## H-02 — Errores SQL visibles (media)

- **Qué se pidió:** reportes con filtros, autorización y agregación SQL en RF-REP; RD-08 oficial exige mensajes sin trazas, rutas ni consultas. El estudiante pidió expresamente comprobar «RD-07, RD-08 y RD-11 mediante entradas inválidas, errores controlados y casos límite temporales».
- **Qué entregó el agente:** controlador con solo dos categorías de excepción controladas y sin frontera común. El timeout SQL expuso una respuesta Development con detalles y la cabecera de la propia cuenta de prueba. Evidencia: [resumen](evidencias/asignacion-3/resultados.json); no se conserva el token ni el cuerpo de esa respuesta.
- **Por qué pasó:** los criterios RF-REP trataban rangos y acceso, pero no caída/bloqueo de persistencia ni sanitización en Development. La auditoría previa pasó recorridos válidos sin provocar ese fallo de dependencia. No se afirma una intención del agente; esto se deduce de los criterios y pruebas disponibles.
- **Corrección inicial histórica:** agente, `012805bf7852631c6cd0fa4be9f078dbb3690293`, bajo la misma autorización de corrección sin publicación. Se aplicó al límite HTTP común porque el patrón se repetía, preservando errores de negocio ya controlados. Dos pruebas fallan antes y pasan después; SQL/HTTP real pasó de 500 con detalles a 503 sin ellos.
- **Qué cambia:** criterios de fallos de reportes y AGENTS: probar errores de infraestructura, no devolver excepción/cabeceras ni en Development; registrar solo información sanitizada.

## H-03 — Dependencia de Core (media)

- **Qué se pidió:** RD-03 oficial: «Si eliminas el módulo de negocio del proyecto, el Core sigue construyéndose y ejecutándose». En esta sesión el estudiante destacó que compilar todo no era suficiente. Contexto y criterios originales preceden a la implementación: `779192d` es antecesor de `5af5211`.
- **Qué entregó el agente:** reporte Core y de negocio mezclados en servicio/contratos; extensiones de reservas dentro de contexto y Usuario. La adición de reportes fue `e176dcf`; parte del acoplamiento era anterior. Al retirar Reservas en una copia fallaron Reserva/Espacio/EstadoReserva con CS0246.
- **Por qué pasó:** el contexto definía capas y los criterios comprobaban resultados/roles, pero no incluían una ejecución del Core sin negocio. La suite completa 207/207 no medía ese requisito de diseño.
- **Corrección inicial histórica:** agente, `85521d7128e6b09f96b45bf1df3e5a606a150fee`, bajo autorización actual. Archivos parciales del módulo separan configuración, navegación y reportes sin cambiar nombres ni SQL. La prueba nueva compila y ejecuta permisos, bandeja y reporte Core sin incluir Reservas ni levantar la API. Otra verifica que no hay cambios respecto del snapshot de migraciones.
- **Qué cambia:** AGENTS y criterios exigen retirar el módulo en una copia, ejecutar Core y comparar metadatos EF; no considerar la compilación monolítica como prueba de RD-03.

## Verificación independiente de las correcciones

[Script RED/GREEN](../tests/integracion/Verificar-RegresionesAsignacion3.ps1): ejecuta cada prueba nueva sobre el padre directo de su commit y sobre la versión corregida, sin cambiar el repositorio. Los commits de corrección no mezclan hallazgos. El commit documental posterior enlaza las evidencias y conserva los resultados originales como históricos.

La evidencia de esta sesión es reproducible mediante scripts y pruebas, y se resume sin secretos en [resultados](evidencias/asignacion-3/resultados.json). El archivo de contexto y los criterios muestran las ampliaciones declaradas. La publicación de rama o etiqueta no está autorizada.

## Revisión personal posterior del estudiante

El estudiante aceptó los hallazgos pero señaló límites de las evidencias: H-01 no ejercitaba SQL desde un ejecutor versionado, H-02 no verificaba registro/contexto/recuperación y H-03 sustituía Program y excluía migraciones. No se atribuye al estudiante la ejecución de las nuevas pruebas.

Primero pidió una propuesta si la refactorización era considerable. Se creó respaldo en e8c18c4 y no se cambió la etiqueta. Posteriormente indicó: «no autorizo una refactorización que implique separar todo el backend en múltiples proyectos o ensamblados» y autorizó una solución modular verificable dentro del proyecto actual.

- **H-01, instrucción nueva:** conservar xUnit y versionar SQL coordinado, dos contextos y comparación directa de primera fecha. **Mejora:** SqlConcurrencyProbe y Validar-H01Sql.ps1; RED/GREEN real sin API, sin pausas para forzar la carrera. El ejecutor ya no depende de archivos temporales personales.
- **H-02, instrucción nueva:** verificar pipeline, datos sensibles sintéticos, errores existentes y recuperación. **Mejora:** cabeceras sintéticas en xUnit y prueba HTTP/SQL que exige 503 seguro y posterior 200. El bloqueo se detecta en SQL, no mediante una espera arbitraria.
- **H-03, instrucción nueva:** conservar proyecto único y demostrar Core real. **Mejora:** extensiones opcionales de composición, inicialización separada, perfil CoreOnly que conserva Program y migraciones, validación estricta de vista de esquema e integración real Core sin Reservas. Se descartaron múltiples ensamblados, ocultar rutas y el ejecutor alternativo anterior.
- **Docker:** red opcional configurable en los scripts y overlay manual, sin fijar una red universal ni eliminar recursos ajenos.
- **Qué cambia definitivamente:** AGENTS y criterios exigen estas evidencias de producción y separación, no inferirlas de pruebas de otro proveedor o de un host sustituto.

La nueva [guía de revisión](revision-personal-asignacion-3.md) registra fronteras, comandos, alternativas y riesgos. Los commits nuevos se conservan sin reescribir los iniciales; la consolidación académica de un commit por hallazgo y la actualización de etiqueta siguen pendientes de autorización.

### Commits históricos de revisión, preservados en la rama de trabajo

| Hallazgo | Corrección inicial | Refuerzos posteriores |
| --- | --- | --- |
| H-01 | 750240a | 3285c23 (SQL versionado), 3444dcb (referencias resueltas a commits) |
| H-02 | 012805b | 29db7b5 (pipeline real, contexto sintético y recuperación) |
| H-03 | 85521d7 | 9941298 (host CoreOnly real y persistencia compatible) |

La infraestructura de red configurable se añadió en 9d51168. Estos commits son adicionales por orden del estudiante de no reescribir todavía. No se afirma que la forma final de un único commit por hallazgo esté consolidada.

### Cierre de consolidación

El estudiante autorizó crear una rama nueva y cuatro commits, manteniendo intactos trabajo-asignacion-3, main, frontend, practica-5, respaldos y asignacion-3. La consolidación se realizó sobre la nueva rama; las referencias a «pendiente de consolidar» en la cronología anterior describen decisiones previas, ya cumplidas aquí. Solo sigue pendiente la autorización de etiqueta/publicación. La cronología no atribuye al estudiante pruebas ejecutadas únicamente por el agente.

Para inspeccionar los cuatro commits: `git log --oneline practica-5..HEAD`. Para comprobar equivalencia funcional con la sesión aprobada: `git diff bd81a59 HEAD -- backend docker-compose.core.yml docker-compose.red-pruebas.yml tests/SISReservas.Api.Tests tests/integracion/SqlConcurrencyProbe tests/integracion/Validar-Practica5.ps1 tests/integracion/Validar-H01Sql.ps1` (vacío). Los cambios adicionales se limitan a documentación/evidencia y al verificador de trazabilidad.

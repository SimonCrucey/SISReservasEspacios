# Auditoría de rúbrica — fase 7

Referencia: enunciado completo de Práctica 3 proporcionado en la conversación.
Se inspeccionaron código, pruebas y evidencia, no solo las afirmaciones del README.
No es una calificación oficial ni una garantía de 8/8.

## Criterios y evidencia

| Criterio | Valor | Estado | Evidencia exacta | Falta para Logrado |
| --- | --- | --- | --- | --- |
| A. RF-NEG-02 a 06 y 09 | 1.5 | Cumplido para las seis funcionalidades HTTP; límite interno declarado | backend/SISReservas.Api/Controllers/ReservasController.cs: ListarEspacios, Disponibilidad, Crear, ObtenerMias, Confirmar, Cancelar; Services/ReservaService.cs: métodos correspondientes y CambiarEstadoAsync; Models/Enums/EstadoReserva.cs y TransaccionesReserva.cs: clase TransicionesReserva; tests/ReservaServiceTests y evidencia-fase-6.json | Incluir los archivos pendientes en el commit de entrega. CompletarAsync existe y está probado, pero no tiene invocador automático; no contar esa automatización como funcionalidad implementada. |
| B. Suite máquina de estados | 1.0 | Cumplido en árbol de trabajo | tests/SISReservas.Api.Tests/TransicionesReservaTests.cs: AsegurarPermitida_TransicionDocumentada_NoRechaza, AsegurarPermitida_ProhibidaDocumentada_RechazaSinCambiarReserva, AsegurarPermitida_DesdeTerminal_RechazaCualquierDestino; ReservaServiceTests: condiciones y terminales; README: comando completo | Ejecutar el mismo comando desde el futuro clon/tag. Hoy los nuevos tests aún no están versionados. |
| C. Defectos 1 a 3 y forma | 1.5 | Pendiente | README sección Defectos relaciona cambios y pruebas reales; git tag --list practica-3* no devuelve etiquetas | Ejecutar cada mutación y demostrar fallos; commits de producción hijos directos de practica-3, mensaje archivo/línea y etiquetas 1–3. Ningún test modificado. |
| D. Integración y defecto 4 | 2.0 | Parcial | tests/SISReservas.Api.Tests/ReservasAutorizacionTests.cs: Crear_SolicitudPendienteRechazaYTrasAprobacionRealPermite; usa SolicitudPermisoService, PermisoService y ReservaService reales; antes de aprobar exige Prohibido y cero reservas, después verifica concesión y reserva del mismo usuario | Ejecutar el defecto 4 y verificar que falla esta integración; commit con forma requerida y etiqueta practica-3-defecto-4. La detección actual se infiere por inspección, no se ha ejecutado. |
| E. Revisión acumulada | 2.0 | Parcial | RevisionAcumuladaTests: registro/activación, login/logout y recuperación; ReservasAutorizacionTests y SolicitudPermisoServiceTests; scripts/Validar-Fase6.ps1 y docs/evidencia-fase-6.json: HTTP real | Demostrar recepción real de activación y recuperación siguiendo README, local o externa. Tokens de cola no acreditan correo recibido. Cuatro frentes tienen evidencia funcional sin SMTP; el frente registro/activación por correo sigue sin demostrarse íntegro. |

Aplicando de forma conservadora los niveles discretos de la rúbrica al estado
actual: A 1.5 + B 1.0 + C 0 + D 1.0 + E 1.0 = **4.5/8 provisional**. La nota la
determina el profesor sobre el commit etiquetado, que aún no existe; no debe
interpretarse como puntaje obtenido. Los dos primeros criterios están completos
en el workspace, no todavía en un clon de la entrega.

## Máquina de estados

Mapa único de validez: TransicionesReserva.Permitidas en TransaccionesReserva.cs.
Cambios reales centralizados en ReservaService.CambiarEstadoAsync, que llama a
AsegurarPermitida antes de asignar o guardar. No hay mapa de estados de permisos
reutilizado para Reservas. Cuatro estados y dos terminales. Cobertura por cada una
de las cuatro filas permitidas y cuatro prohibidas documentada en
[maquina-de-estados.md](maquina-de-estados.md).

La primera fila es Pendiente → Confirmada: rol Administrador más condición
CumpleCondicionesConfirmacionAsync. Las pruebas separan rechazo por rol y por
espacio inactivo/intervalo inválido/pasado/conflicto. Defecto 3 debe retirar esta
comprobación de condición, sin introducir otros cambios. Toda combinación no
permitida se rechaza; las prohibidas documentadas son ejemplos, no lista exhaustiva.

## Persistencia y evidencia funcional

RD-09 sí tiene evidencia SQL: fase 6 aplicó migraciones a un catálogo nuevo,
creó reserva por HTTP, cerró el proceso API, arrancó otro y comparó identidad,
fechas y estado por HTTP. No depende de que esa base esté en el equipo evaluador.
Las 73 verificaciones son peticiones y aserciones del script, **no 73 pruebas xUnit**.
Son evidencia histórica; esta fase solo repite la regresión, no el escenario SQL.

## Git y reproducibilidad

Los nuevos archivos de producción, migración, pruebas, script y guías aparecen en
git ls-files --others --exclude-standard: no están ignorados, pero tampoco
incluidos todavía en el índice/historial. README y archivos modificados previos
tampoco están confirmados. Sin añadirlos posteriormente al commit, el profesor
recibiría un proyecto incompleto. Esta fase no ejecuta git add ni commit.

Se debe verificar antes de etiquetar que git ls-files incluya ReservaService.cs,
ReservasController.cs, ReservaDtos.cs, Espacio.cs, TipoEspacio.cs, AssemblyInfo.cs,
EmailDispatcherService.cs, los dos archivos de migración y todos los nuevos tests,
README y guías. El snapshot debe reflejar las entidades nuevas. Los archivos
appsettings.Development.json y .env están ignorados: el README por eso usa
variables de entorno explícitas en vez de depender de configuración privada.

## Pendientes exactos

1. Recepción SMTP local o externa de activación y recuperación; conservar evidencia
   sin tokens, contraseñas ni datos personales reales. No hace falta Mailpit/Docker.
2. Preparar commit base con todos los archivos necesarios y futura etiqueta practica-3;
   repetir suite en clon limpio. No hacerlo antes de resolver/aceptar los pendientes.
3. Plantar los cuatro defectos por separado sobre esa misma base; ejecutar suite,
   registrar pruebas fallidas y archivo/línea, crear etiquetas con forma requerida.
4. Si un mutante sobrevive, reforzar pruebas en la versión correcta, reconstruir
   base y los cuatro hijos. No fingir detección mediante documentación.
5. Verificar publicación posterior de commit/etiquetas y URL Moodle cuando se
   autorice; no se ejecuta ninguna operación Git de entrega en esta fase.

Limitaciones adicionales, sin presentarlas como requisitos extra de esta rúbrica:
sin tarea automática de completado, sin ensayo concurrente SQL, y advertencias
NU1510/NU1902/NU1903. No se implementan notificaciones/reportes ni adjuntos.

## Regresión de fase 7

Comandos: dotnet build SISReservasEspacios.slnx; dotnet test SISReservasEspacios.slnx;
git diff --check; git status --short. Build: 0 errores y 6 advertencias preexistentes.
Suite: 138 aprobadas, 0 fallidas, 0 omitidas. git diff --check sin errores; avisos
de normalización futura LF/CRLF no son fallos de whitespace. El estado conserva
los archivos nuevos sin seguimiento y los cambios previos pendientes.
No se modificó código ni pruebas para hacer pasar la auditoría.

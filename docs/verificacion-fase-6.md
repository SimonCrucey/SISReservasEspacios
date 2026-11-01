# Fase 6: validación real sin SMTP

## Resultado actualizado tras autorización del nuevo alcance

Se creó exclusivamente SISReservas_Practica3_Test en (localdb)\MSSQLLocalDB,
tras confirmar su inexistencia. Se aplicaron las cuatro migraciones allí, incluida
20261101193103_AgregarEspaciosYReservas. No se modificaron las dos bases originales.
La API temporal usó http://127.0.0.1:15064, AppBaseUrl con ese mismo origen y
EmailDispatcher__Enabled=false. Se detuvo al terminar. La base de prueba se conserva.

**73 verificaciones HTTP/SQL y aserciones aprobaron**. Evidencia sin tokens ni
contraseñas: [evidencia-fase-6.json](evidencia-fase-6.json).

| Flujo | Resultado real | Evidencia |
| --- | --- | --- |
| Registro y activación HTTP, sin entrega | APROBADO | Tres registros 200, activación 200 y reutilización 400; token leído directamente de la cola aislada. |
| Entrega de correo | NO EJECUTADO | SMTP deshabilitado; todos los mensajes continúan Pendientes. |
| Login/logout | APROBADO | Login 200 con token, logout 200 y token revocado 401. |
| Autorización | APROBADO | Sin sesión 401; Estándar en bandeja administrativa, aprobación y confirmación recibe 403. |
| Recuperación sin entrega | APROBADO | Recovery 200, código de cola, reset 200, reutilización 400, sesión anterior 401 y login con nueva contraseña 200. |
| Gestión de permisos | APROBADO | Solicitar 201, pendiente no autoriza 403, aprobar 200/Aplicada, doble aprobación 409, rechazo sin motivo 400, rechazo válido 200/Rechazada y aprobar rechazada 409. |
| Reservas | APROBADO | Crear con permiso 201/Pendiente, confirmar 200/Confirmada, cancelar 200/Cancelada; salidas de Cancelada 409 y estado SQL intacto. |
| Disponibilidad y privacidad | APROBADO | Libre/ocupado coherentes; solapamiento 409, adyacente 201, fechas inválidas 400, espacio inactivo 404; otro propietario no ve reservas y no puede cancelar (404). |
| RD-09 | APROBADO | Reserva Pendiente creada por HTTP, proceso API detenido y nuevo proceso arrancado; consulta HTTP conserva Id, espacio, fechas y estado de todas las reservas propias. |

El Administrador se preparó promoviendo solo su cuenta ficticia en la base nueva.
Las contraseñas aleatorias se mantuvieron en memoria; no se imprimieron ni se
guardaron en la evidencia. La lectura directa de tokens de cola no demuestra
registro/activación ni recuperación completos por correo. No hace falta SMTP
para login/logout, roles, permisos, reservas o RD-09; sí hace falta un transporte
de correo para verificar recepción del mensaje y uso del enlace/código recibido.

Completada sigue comprobándose en pruebas unitarias como operación interna; no
hay endpoint que suplante al sistema. No se probaron aquí concurrencia simultánea,
entrega SMTP ni finalización automática.

### Comandos ejecutados y reproducción

```powershell
# Tras verificar que no existía el catálogo, se ejecutó únicamente sobre este destino:
$pruebaCadena='Server=(localdb)\MSSQLLocalDB;Database=SISReservas_Practica3_Test;Trusted_Connection=True;TrustServerCertificate=True;'
$env:ConnectionStrings__DefaultConnection=$pruebaCadena
$env:EmailDispatcher__Enabled='false'
dotnet ef database update --project backend/SISReservas.Api/SISReservas.Api.csproj --connection $pruebaCadena
./scripts/Validar-Fase6.ps1
dotnet build SISReservasEspacios.slnx
dotnet test SISReservasEspacios.slnx
git diff --check
git status --short
```

El script es deliberadamente de una sola ejecución sobre un catálogo recién
migrado sin usuarios. Rechaza una base con usuarios en vez de borrar o reemplazar
datos; no repetir ahora sobre el catálogo conservado. Genera contraseñas en memoria,
prepara cuentas y espacios, ejecuta peticiones y detiene únicamente su propio
proceso. No aplica migraciones ni elimina bases. Requiere PowerShell 7 y SDK .NET.
El primer arranque del comprobador falló por las comillas de una ruta con espacios;
se corrigió únicamente el script y se repitió antes de crear cuentas. No fue un
fallo de código de producción.

Build final: 0 errores, 6 advertencias preexistentes. Suite: 138 aprobadas,
0 fallidas, 0 omitidas. No se cambió código de producción en esta continuación.

## Registro histórico del bloqueo inicial (superado para el alcance sin SMTP)

Fecha: 2026-11-01. No se arrancó una API ni se modificaron bases en esta fase.

## Inspección ejecutada

```powershell
Get-Command SqlLocalDB,sqlcmd,docker,mailpit -ErrorAction SilentlyContinue
SqlLocalDB info
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d master -Q "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE database_id > 4;" -l 5
```

LocalDB y sqlcmd están disponibles. Instancias enumeradas: MSSQLLocalDB y
ProjectModels. En MSSQLLocalDB solo se encontraron GastosPersonalesDb y
SISReservasEspacios; ninguna fue usada para pruebas ni modificada.
SISReservas_Practica3_Test no existe y sería el destino propuesto, no una base
utilizada. No se creó ni se aplicaron migraciones.

Docker y Mailpit no se encontraron como comandos disponibles. Las conexiones TCP
a 127.0.0.1:1025 y 127.0.0.1:8025 fueron rechazadas: no hay captura SMTP disponible
en los puertos previstos. No se instaló software ni se inició ningún contenedor.

## Resultados funcionales reales de esta fase

| Flujo | Resultado | Evidencia |
| --- | --- | --- |
| Registro y activación por correo | NO EJECUTADO | Falta capturador SMTP autorizado. |
| Inicio y cierre de sesión HTTP | NO EJECUTADO | Se detuvo la preparación antes de crear datos. |
| Estándar recibe HTTP 403 | NO EJECUTADO | Sin cuentas del nuevo entorno. |
| Recuperación y código de un solo uso HTTP | NO EJECUTADO | Sin captura de correo. |
| Solicitud, aprobación, rechazo y transición inválida HTTP | NO EJECUTADO | Sin nuevo entorno preparado. |
| Crear, consultar, confirmar, cancelar, solapamientos y autorización HTTP | NO EJECUTADO | Sin migración ni datos del entorno nuevo. |
| RD-09 con reinicio real de API | NO EJECUTADO | No se creó reserva SQL ni se reinició una API de prueba. |

Los resultados InMemory de regresión no cambian estos estados funcionales.

## Intervención necesaria

Autorizar la descarga y ejecución local de Mailpit, o preparar personalmente un
capturador SMTP y proporcionar host/puerto y modo TLS/autenticación sin compartir
secretos en el chat. Docker no está disponible como comando; no ejecutar todavía
el ejemplo docker run de la guía. No se necesita instalar Docker si se autoriza
Mailpit nativo. Confirmar que 1025 es SMTP de captura y 8025 es su interfaz local.

Una vez disponible, retomar el inventario y verificar otra vez que el catálogo
propuesto no existe. Con la autorización de fase 6 se podrá crear exclusivamente
ese catálogo nuevo y aplicar allí las migraciones. Ejemplo de configuración para
el siguiente paso, **todavía no ejecutado**:

```powershell
$env:ConnectionStrings__DefaultConnection='Server=(localdb)\MSSQLLocalDB;Database=SISReservas_Practica3_Test;Trusted_Connection=True;TrustServerCertificate=True;'
$env:AppBaseUrl='http://localhost:5054'
$env:SMTP_HOST='127.0.0.1'
$env:SMTP_PORT='1025'
$env:SMTP_FROM='reservas@test.invalid'
$env:SMTP_USE_AUTH='false'
$env:SMTP_ENABLE_SSL='false'
$env:EmailDispatcher__Enabled='true'
```

No ejecutar esta configuración contra las dos bases existentes. Las peticiones y
resultados esperados están separados de la evidencia ejecutada en
[control-acceso-local.md](control-acceso-local.md). Persistencia está descrita en
[verificacion-fase-4.md](verificacion-fase-4.md).

## Regresión ejecutada

```powershell
dotnet build SISReservasEspacios.slnx
dotnet test SISReservasEspacios.slnx
git diff --check
git status --short
```

Build: 0 errores y 6 advertencias preexistentes (NU1510, NU1902, NU1903).
Tests: 138 aprobadas, 0 fallidas, 0 omitidas. git diff --check sin errores;
Git solo advierte normalización futura LF/CRLF en Permiso.cs. git status --short
muestra los cambios pendientes de las fases anteriores y las dos guías tocadas
en esta fase. No se crearon commits, etiquetas ni push.

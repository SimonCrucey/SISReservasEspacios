# SISReservasEspacios

Sistema académico de reservas de salones, canchas y equipos de **Programación III · TDS-007 · ITLA · 2026-C-3**. Integra Control de acceso, Gestión de permisos, Reservas, Documentos, planificación exterior, Notificaciones y Reportes.

El objetivo es ejecutar y evaluar el backend con Docker Compose, sin instalar .NET ni SQL Server en el host. Swagger es la interfaz de evaluación; no se requiere frontend.

**Versión de esta guía.** Entrega consolidada de Asignación 3 en `entrega-asignacion-3`, basada en la Práctica 5 publicada. Conserva un commit por hallazgo y commits documentales posteriores. La sesión histórica y sus evidencias permanecen identificadas como tales. La etiqueta local `asignacion-3` todavía conserva `e8c18c4`: no se ha movido ni publicado. En la revisión local usar la rama de entrega; la selección de la etiqueta desde GitHub solo corresponde después de autorizar su actualización/publicación.

## Funcionalidades principales

- Registro con activación por correo, sesiones Bearer, roles, bloqueo temporal y recuperación de contraseñas.
- Solicitudes de permisos con aprobación/rechazo y concesión efectiva.
- Reservas con disponibilidad, autorización, prevención de solapamientos y máquina de estados.
- Documentos con subida, listado, descarga autorizada y eliminación lógica.
- Planificación exterior mediante WeatherAPI.
- Bandejas de notificaciones propias, preferencias y correo en cola con reintentos.
- Reportes agregados de solicitudes y reservas, filtrados por usuario y periodo.

## Stack y estructura

C# y ASP.NET Core **.NET 10**, EF Core SQL Server **10.0.12**, SQL Server **2022**, Swagger/Swashbuckle, Docker Compose, SMTP/Mailpit y xUnit **2.9.3**. Las versiones de paquetes están en [API](backend/SISReservas.Api/SISReservas.Api.csproj) y [pruebas](tests/SISReservas.Api.Tests/SISReservas.Api.Tests.csproj).

```text
backend/SISReservas.Api/
  Controllers/     HTTP y autorización
  DTOs/            Contratos de entrada/salida
  Services/        Acceso, Correo, Permisos, Reservas, Documentos, Notificaciones, Reportes
  Models/          Entidades y enums
  Data/            DbContext y arranque SQL
  Migrations/      Siete migraciones, diseñadores y snapshot EF
  Security/        Sesiones y credenciales
  Serialization/   Convención JSON de instantes UTC
  Properties/      Perfiles de ejecución
tests/
  SISReservas.Api.Tests/   Suite xUnit
  integracion/            Validación HTTP/SQL/SMTP aislada
docs/                     Criterios, evidencias y entregas anteriores
```

Las transiciones de Reservas se definen en `TransicionesReserva`. SQL conserva los metadatos de documentos; sus archivos se guardan en disco.

## Requisitos previos

| Camino | Requisitos en el host |
| --- | --- |
| Aplicación con Docker | Git, Docker Desktop iniciado en modo contenedores Linux y Docker Compose v2; conexión para descargar imágenes y restaurar paquetes en el primer build. SQL Server requiere arquitectura compatible con su imagen x86-64 y recursos disponibles. |
| Suite xUnit | .NET SDK 10 y acceso a NuGet para restaurar paquetes; no requiere API, SQL Server ni Docker iniciados. Las pruebas SMTP de loopback necesitan puertos locales disponibles. |
| Integración completa | Windows PowerShell 5.1 o posterior, Git y Docker Desktop con Compose v2, espacio, memoria y subredes disponibles para un proyecto aislado. No necesita .NET instalado en el host en modo Docker. |
| Desarrollo sin contenedores para API/SQL | .NET SDK 10, SQL Server o LocalDB instalado y una instancia SMTP accesible para los flujos de correo. Véase [Desarrollo local](#desarrollo-local-con-visual-studio-y-localdb). |

No se requieren Node.js, React ni una instalación local de EF CLI para el arranque Docker. No hay cuentas preconfiguradas con contraseñas compartidas.

## Inicio rápido con Docker

**Directorio:** ejecuta el clon desde una carpeta donde puedas crear el repositorio. Después, todos los comandos de esta guía parten de la **raíz `SISReservasEspacios`**, salvo indicación expresa.

```powershell
git clone https://github.com/SimonCrucey/SISReservasEspacios.git
cd SISReservasEspacios
```

**Procedimiento oficial del profesor:** después de publicarse la etiqueta definitiva de Asignación 3, selecciona la versión evaluable desde el clon de GitHub:

```powershell
git checkout asignacion-3
git rev-parse HEAD
```

Compara el hash con el comunicado para la entrega. No es necesario publicar ni seleccionar una rama de trabajo. Antes de autorizar la actualización y publicación, la etiqueta local antigua no representa este candidato; para esa revisión previa utiliza exclusivamente el procedimiento «Clon local limpio antes de publicar» al final de esta guía, sobre `entrega-asignacion-3`.

En un clon nuevo de la versión correcta:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

No sobrescribas un `.env` privado existente. Los valores de demostración están en `.env.example`; no es necesaria una clave WeatherAPI para arrancar los demás módulos. Deja esta terminal abierta. En otra PowerShell situada en la misma raíz:

```powershell
docker compose ps
docker compose logs --tail=50 api
```

Espera SQL healthy y la API en ejecución. Abre **http://localhost:5054/swagger**; el documento de contratos es **http://localhost:5054/swagger/v1/swagger.json**. El sitio raíz `/` no es un endpoint de salud y puede devolver 404.

| Servicio | Puerto dentro de Docker | Puerto del host en Compose principal |
| --- | --- | --- |
| `api` | HTTP 8080 | `127.0.0.1:5054`, configurable con `APP_PORT` |
| `sqlserver` | SQL 1433 | Ninguno |
| `mailpit` | SMTP 1025, HTTP 8025 | Ninguno |
| `mail-inspector` | Cliente curl bajo demanda | Ninguno; no arranca con el up normal |

La red interna usa los nombres `sqlserver` y `mailpit`. Desde el navegador del host solo se publica la API. Swagger está habilitado por `Development`; esta composición es para evaluación local, no un despliegue público.

**Apagado y reinicio**, desde la misma raíz:

```powershell
docker compose down
docker compose up --build
```

No añadas `-v`. Se conservan los volúmenes; Mailpit no conserva su buzón tras recrear el contenedor. Para arrancar en segundo plano puede usarse `docker compose up -d --build`.

## Variables de entorno

[.env.example](.env.example) es la plantilla pública; `.env` es privado, está ignorado por Git y excluido del contexto Docker. No incluyas contraseñas, tokens, claves ni correos privados en commits o capturas. Las variables exportadas en PowerShell tienen prioridad sobre `.env` al interpolar Compose: evita valores heredados de otras pruebas.

| Variable de la plantilla | Uso |
| --- | --- |
| `MSSQL_SA_PASSWORD` | Contraseña de la instalación SQL. El valor de la plantilla es público, únicamente para demostración local; usa uno privado en entornos compartidos. |
| `APP_PORT` | Puerto del host para API y origen de enlaces; predeterminado 5054. |
| `EmailDispatcher__Enabled` | `true` activa el despacho de la cola; `false` deja mensajes pendientes. |
| `SMTP_HOST` / `SMTP_PORT` | En Compose: `mailpit` / `1025`. |
| `SMTP_FROM` | Remitente del mensaje. |
| `SMTP_ENABLE_SSL` / `SMTP_USE_AUTH` | STARTTLS y autenticación; ambos `false` con Mailpit local. |
| `SMTP_USERNAME` / `SMTP_PASSWORD` | Solo si el servidor requiere autenticación; mantener privados. |
| `WEATHER_API_KEY` | Clave personal para una consulta real a WeatherAPI; puede quedar vacía para evaluar los demás módulos. |
| `WEATHER_LOCATION` | Ubicación del recinto; la plantilla usa coordenadas de Santo Domingo. |

Compose fija además `ConnectionStrings__DefaultConnection` hacia `sqlserver,1433` y el catálogo `SISReservasEspacios`; `Database__ApplyMigrations=true`; `AppBaseUrl=http://localhost:APP_PORT` y `Documentos__Directorio=/app/data/documentos`.

Después de editar variables de la API en `.env`, incluida `WEATHER_API_KEY`, desde la raíz:

```powershell
docker compose up -d --force-recreate api
```

Cambiar `MSSQL_SA_PASSWORD` en la plantilla/entorno **no cambia la contraseña de un SQL Server ya inicializado**. Conserva la correspondiente al volumen existente. No intentes solucionar diferencias de credenciales borrando los datos.

`Database__StartupTimeoutSeconds` es una opción del proceso .NET (120 s por defecto, rango 1–300). El Compose principal no la reenvía desde `.env`; añadirla allí por sí solo no cambia el timeout. En desarrollo local puede establecerse en las variables del proceso.

## SQL Server y migraciones

La API aplica automáticamente las migraciones pendientes **antes de atender HTTP** cuando `Database:ApplyMigrations` está habilitado. No necesitas ejecutar `dotnet ef database update` para Compose.

El healthcheck comprueba `master`; el catálogo de la aplicación puede seguir recuperándose. `DatabaseStartup` espera estado y acceso efectivo, con plazo total acotado y un bloqueo de sesión en `master` que serializa creación y migración entre instancias. Conserva el bloqueo propio de EF y no utiliza `EnsureCreated`. Requiere acceso a `master` y permisos de migración. Credenciales rechazadas en master, catálogo offline y errores conocidos de permisos/esquema fallan explícitamente. Un acceso al catálogo todavía indisponible tras autenticar en master se espera hasta el límite.

Las siete migraciones versionadas en [Migrations](backend/SISReservas.Api/Migrations) se aplican en este orden:

| Identificador | Alcance |
| --- | --- |
| `20261004141300_InitialAccessControl` | Usuarios, tokens de acceso y cola de correos. |
| `20261004152153_AddSessions` | Sesiones. |
| `20261019220956_AgregarGestionPermisos` | Solicitudes y permisos concedidos. |
| `20261101193103_AgregarEspaciosYReservas` | Espacios, reservas, relaciones e índices. |
| `20261109203709_AgregarDocumentos` | Metadatos de documentos. |
| `20261122190020_AgregarNotificaciones` | Notificaciones y preferencia de correo del usuario. |
| `20261122190414_AmpliarColaCorreos` | Intentos, error, reclamo temporal e índices de la cola. |

También están sus archivos Designer y el snapshot de EF; no deben editarse para reparar una base. Para consultar el historial, desde la raíz y sin exponer SQL al host:

```powershell
docker compose --% exec -T sqlserver sh -c "SQLCMDPASSWORD=$MSSQL_SA_PASSWORD; export SQLCMDPASSWORD; exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -d SISReservasEspacios -Q 'SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;'"
```

En Windows PowerShell, `--%` evita expandir la variable antes de llegar al shell
del contenedor; la contraseña permanece en su entorno y no se imprime. La
consulta se pasa con `-Q`, sin tubería SQL por stdin: así no depende del BOM de
la consola observado en PowerShell 5.1 y no requiere cambiar su codificación.

En una inicialización completa se esperan los siete identificadores. No apliques estas comprobaciones contra bases ajenas. `docker-compose.dev.yml` es opcional y publica SQL en `127.0.0.1:14333`; **no es necesario para evaluar** ni se carga automáticamente. Si un operador necesita esa inspección local, debe usar explícitamente ambos archivos:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```

Al administrar esa variante, conserva los mismos argumentos `-f` y el mismo proyecto. No cambies credenciales ni archivos de migración para eludir un error de esquema.

## SMTP y Mailpit

Compose incluye Mailpit como destino de pruebas, sin autenticación, sin STARTTLS y sin puerto publicado al host. El despachador está registrado como servicio hospedado y habilitado por la plantilla. No hace falta un proveedor de correo externo para activar cuentas o recuperar contraseñas.

Desde la raíz, lista los mensajes y consulta uno por ID:

```powershell
docker compose run --rm --no-deps mail-inspector -fsS http://mailpit:8025/api/v1/messages
$mensajeId = Read-Host 'ID del mensaje nuevo mostrado en la lista'
docker compose run --rm --no-deps mail-inspector -fsS "http://mailpit:8025/api/v1/message/$mensajeId"
```

El servicio explícito activa su perfil `inspeccion-correo` para esa ejecución y `--rm` elimina el contenedor auxiliar al terminar. No se crea un puerto público adicional. Revisa `Text` del mensaje; contiene el enlace de activación o el código de recuperación.

Usa cuentas ficticias y mantén la salida en una terminal privada: contiene credenciales de un solo uso. **Pendiente** significa encolado; **Enviado** significa aceptación SMTP; observar el mensaje en Mailpit acredita recepción SMTP local, no entrega a un buzón externo. El despachador espera diez segundos entre ciclos; una fila en cola por sí sola no demuestra recepción.

Habilitarlo procesa también correos antiguos pendientes del catálogo conectado. En pruebas utiliza un proyecto/base nuevos y un SMTP de captura; no dirijas una cola de datos reales a un destino de prueba. Los controles de reintento y el modo CLI están en [Cola de correo](#cola-de-correo-y-reintentos).

## Autenticación, usuarios y roles

Prepara dos cuentas ficticias distintas, Administrador y Estándar. Todas nacen Estándar; **no hay contraseña administrativa inicial**. Registra y activa ambas, promueve únicamente la cuenta elegida con el comando de operador indicado abajo y vuelve a iniciar sesión. En Swagger cambia el token en Authorize según el actor de cada operación.

### Registro, activación y sesión

En Swagger:

1. Ejecuta `POST /api/auth/register` con `nombre`, `email` y `password`. La contraseña requiere al menos ocho caracteres, letras y números. La cuenta nace Estándar e inactiva.
2. Consulta el mensaje capturado en Mailpit y abre el enlace recibido de activación. Se consume una sola vez.
3. Ejecuta `POST /api/auth/login` con `email` y `password`.
4. Copia `sessionToken` en **Authorize**. Swagger usa el esquema Bearer. Para otros clientes, envía `Authorization: Bearer TOKEN`.
5. Consulta `GET /api/auth/me` y cierra con `POST /api/auth/logout`. La sesión revocada recibe 401 al reutilizarse.

Un HTTP 200 sin `sessionToken` en login puede indicar una cuenta inactiva o bloqueada. Para reenviar activación usa `POST /api/auth/resend-activation` con el correo como string JSON, por ejemplo `"persona@evaluacion.test.invalid"`. El reenvío invalida los enlaces anteriores.

### Primer Administrador

Registra y activa primero la cuenta. El operador de Docker puede promoverla con:

```powershell
docker compose exec -T api dotnet SISReservas.Api.dll --administrador-inicial admin@evaluacion.test.invalid
```

Sustituye el correo por el de la cuenta activada. El comando rechaza ejecutarse si ya existe un Administrador, revoca las sesiones previas y crea una cancha de evaluación si no hay espacios. Inicia sesión de nuevo y comprueba el rol en `/api/auth/me`.

### Recuperación y cambio de contraseña

- `POST /api/passwords/recovery` con `{"email":"persona@evaluacion.test.invalid"}` devuelve una respuesta genérica y encola el mensaje si corresponde.
- Lee el código recibido y ejecuta `POST /api/passwords/reset` con `token` y `nuevaPassword`. El código dura 30 minutos y se consume una sola vez.
- Con sesión, `POST /api/passwords/change` recibe `passwordActual` y `nuevaPassword`.
- Un Administrador puede solicitar `POST /api/passwords/{id}/force-reset`. El destinatario completa el restablecimiento mediante el correo.

### Gestión de usuarios

| Operación | Endpoint | Acceso |
| --- | --- | --- |
| Listar usuarios | `GET /api/users` | Administrador o Estándar con ConsultarUsuarios. Este último recibe solo id y nombre. |
| Cambiar rol | `PUT /api/users/{id}/role` con `{"rol":1}` o `{"rol":2}` | Administrador. 1=Administrador, 2=Estándar. |
| Desactivar/reactivar | `POST /api/users/{id}/deactivate` y `/activate` | Administrador. |

Cambiar rol o desactivar revoca las sesiones del destinatario. El Administrador no puede quitarse su propio rol ni desactivarse. Cinco intentos de login fallidos bloquean la cuenta durante 15 minutos.

### Gestión de permisos

Un Estándar solicita permisos con `POST /api/permission-requests`. Los valores solicitables son `3` (ConsultarUsuarios) y `4` (ReservarEspacios).

- Consultar las propias con `GET /api/permission-requests/mine`.
- El Administrador consulta `GET /api/permission-requests/pending`.
- Aprobar con `POST /api/permission-requests/{id}/approve`.
- Rechazar con `POST /api/permission-requests/{id}/reject` y `{"motivoRechazo":"Motivo"}`.

La aprobación concede el permiso. Una segunda resolución o una transición inválida recibe 409. La resolución propia está prohibida. La guía detallada de RF-GP-01 a 07 se conserva en la documentación histórica.

Para solicitar ReservarEspacios, el cuerpo es `{"permiso":4}`. Conserva el `id` devuelto para aprobar/rechazar desde la otra cuenta. Un Estándar no debe resolver solicitudes; comprueba HTTP 403 en `GET /api/permission-requests/pending`. Usa Swagger para completar los cuerpos según sus contratos reales.

## Reservas, documentos y WeatherAPI

Estas funcionalidades de prácticas anteriores se conservan y permiten provocar los eventos de Práctica 5. El comando de primer Administrador crea una cancha solo si no existen espacios; no hay un endpoint de administración de espacios en esta versión.

### Reservas y disponibilidad

Todas las operaciones HTTP requieren sesión y rol válido.

| Operación | Endpoint y datos |
| --- | --- |
| Espacios activos | `GET /api/reservas/espacios` |
| Disponibilidad | `GET /api/reservas/espacios/{id}/disponibilidad?inicio=...&fin=...` |
| Crear | `POST /api/reservas` con `espacioId`, `fechaInicio` y `fechaFin` |
| Reservas propias | `GET /api/reservas/mine` |
| Confirmar | `POST /api/reservas/{id}/confirmar`, Administrador |
| Cancelar | `POST /api/reservas/{id}/cancelar`, propietario o Administrador |

Desde la raíz, genera un cuerpo con un intervalo futuro UTC y pégalo en Swagger.
Sustituye `1` por el ID de un espacio activo del listado:

```powershell
$inicioReserva = [DateTime]::UtcNow.AddDays(1)
@{
    espacioId = 1
    fechaInicio = $inicioReserva.ToString('o')
    fechaFin = $inicioReserva.AddHours(1).ToString('o')
} | ConvertTo-Json
```

El Estándar necesita ReservarEspacios aprobado para crear. Las fechas deben ser coherentes y el inicio anterior al fin. Las reservas Pendientes y Confirmadas bloquean los intervalos solapados. Los horarios adyacentes son válidos.

Los instantes de correos (creación, envío e intento), notificaciones, permisos,
reservas y límites de reportes se devuelven en ISO 8601 con UTC explícito (`Z`).
SQL Server `datetime2` conserva los valores UTC pero no su identificador de zona;
la serialización identifica esos valores sin desplazar sus ticks ni actualizar
registros. Las fechas de calendario `DateOnly` y los contratos `DateTimeOffset`
conservan su formato. Las entradas sin zona no se reinterpretan como UTC:
siguen sujetas a las validaciones existentes. El cliente decide la zona de
presentación; no debe sumar o restar horas manualmente.

### Máquina de estados

Pendiente permite Confirmada o Cancelada. Confirmada permite Completada o Cancelada. Cancelada y Completada son terminales. Completar es una operación interna posterior al fin del intervalo y carece de endpoint público. Una transición inválida rechaza el cambio y conserva el estado.

Consulta las tablas, condiciones y cobertura de pruebas en [máquina de estados](docs/maquina-de-estados.md).

### Documentos

| Operación | Endpoint | Resultado |
| --- | --- | --- |
| Subir | `POST /api/documentos`, multipart con campo `archivo` | 201 con nombre, MIME, tamaño, fecha y propietario. Límite de 10 MiB. |
| Listar | `GET /api/documentos` | Estándar ve los propios y Administrador todos los vigentes. |
| Descargar | `GET /api/documentos/{id}/descargar` | Propietario o Administrador. |
| Eliminar lógicamente | `DELETE /api/documentos/{id}` | 204, conserva registro y archivo físico. |

Los eliminados dejan de aparecer y descargarse. La descarga o eliminación ajena devuelve 404. Archivos vacíos o ausentes se rechazan de forma controlada. Un formato distinto de multipart puede devolver 415. Las autorizaciones se comprueban en el servidor.

### Planificación exterior con WeatherAPI

Obtén una clave propia en [WeatherAPI](https://www.weatherapi.com/) y configura `WEATHER_API_KEY` en el `.env` privado. Ajusta `WEATHER_LOCATION` al recinto y recrea la API:

```powershell
docker compose up -d --force-recreate api
```

Con sesión, consulta `GET /api/reservas/espacios/{id}/planificacion-exterior?inicio=...&fin=...` para una cancha exterior. La operación combina disponibilidad con el pronóstico horario de tres días y exige cobertura completa del intervalo. Recomienda el horario cuando la lluvia prevista es inferior al 60 % y el viento inferior a 40 km/h. Son umbrales académicos de demostración. La clasificación de canchas cubiertas se decide al seleccionar el espacio, ya que el modelo no la almacena.

Ejemplo comprobado mediante proveedor simulado:

```json
{"espacioId":1,"disponible":true,"recomendado":true,"motivo":"Cancha disponible y condiciones favorables."}
```

También se comprobó una consulta real del endpoint en Docker con una clave autorizada. Las pruebas simuladas cubren éxito, lluvia, clave ausente o rechazada, HTTP 503, timeout y datos incompletos. La ausencia de clave o fallo del proveedor produce HTTP 503 controlado y las demás operaciones siguen disponibles. La clave se obtiene del entorno y se suprimen los logs de URLs del proveedor.

## Práctica 5: notificaciones y reportes

### Notificaciones

Práctica 5 añade una bandeja persistente y reutiliza la cola SMTP existente.
Todas las rutas requieren una sesión Bearer válida. Los Administradores destinatarios
de solicitudes nuevas son los activos, según los criterios de esta entrega.

| Evento | Cómo provocarlo en Swagger | Destinatario |
| --- | --- | --- |
| Solicitud creada | Estándar: POST `/api/permission-requests` con `{"permiso":4}` o 3 | Todos los Administradores activos |
| Solicitud aprobada | Otro Administrador: POST `/api/permission-requests/{id}/approve` | Solicitante, indicando aprobación y permiso aplicado |
| Solicitud rechazada | Administrador: POST `/api/permission-requests/{id}/reject` con `{"motivoRechazo":"Motivo de prueba"}` | Solicitante, con motivo |
| Reserva confirmada | Crear reserva con permiso aprobado y luego Administrador: POST `/api/reservas/{id}/confirmar` | Propietario de la reserva |

Una operación rechazada no genera notificaciones. El negocio, la bandeja y su
correo se guardan juntos. El correo opcional está habilitado inicialmente.

| Operación propia | Petición |
| --- | --- |
| Bandeja y contador de no leídas | GET `/api/notificaciones` |
| Marcar leída | PATCH `/api/notificaciones/{id}/leida`, sin cuerpo |
| Consultar preferencia | GET `/api/notificaciones/preferencias` |
| Cambiar preferencia | PUT `/api/notificaciones/preferencias` con `{"correoHabilitado":false}` o true |

La bandeja devuelve `noLeidas` y `notificaciones`, con `leidaEn=null` para las
nuevas. Marcar una propia devuelve 204 y repetir conserva su fecha. Una ajena
o inexistente devuelve 404. El usuario no puede elegir otro propietario.
Al desactivar correo, los eventos posteriores conservan la interna y no encolan
su correo. No retira correos ya encolados. Activación y recuperación siguen
encolando sus mensajes indispensables, independientemente de esta preferencia.

#### Cola de correo y reintentos

El servicio hospedado envía en segundo plano, en lotes de hasta 20, y espera
diez segundos después de cada ciclo. Cada envío tiene timeout de 20 segundos.
**Máximo de tres intentos totales por mensaje**. Después queda Fallido y no se
reintenta automáticamente. Los estados JSON son Pendiente=1, Enviado=2 y Fallido=3.

El Administrador consulta GET `/api/correos?pagina=1` (100 por página), con
`total`, `pagina` y `correos`. Incluye identificador, destinatario, estado,
intentos, creación, último intento, envío y último error sanitizado. Excluye el
cuerpo del mensaje. Estándar recibe 403 y sin sesión 401.

También puede ejecutarse un lote independiente desde la consola del operador:

```powershell
docker compose exec -T api dotnet SISReservas.Api.dll --procesar-correos
```

Para comprobar los intentos de manera controlada en **un entorno de pruebas**:

1. En PowerShell, desactiva temporalmente el despachador de esa composición.
2. Detén Mailpit y provoca una reserva confirmada o solicitud nueva. La operación
   termina correctamente y queda el correo pendiente.
3. Ejecuta el lote anterior tres veces. Consulta la cola como Administrador y
   comprueba tres intentos, Fallido y último error. La cuarta ejecución no lo cambia.
4. Inicia Mailpit y restaura el despachador.

```powershell
$despachoAnterior = [Environment]::GetEnvironmentVariable('EmailDispatcher__Enabled', 'Process')
$env:EmailDispatcher__Enabled='false'
docker compose up -d --force-recreate api
docker compose stop mailpit
# Provocar el evento HTTP y ejecutar --procesar-correos tres veces.
# Consultar GET /api/correos desde Swagger.
docker compose start mailpit
[Environment]::SetEnvironmentVariable('EmailDispatcher__Enabled', $despachoAnterior, 'Process')
docker compose up -d --force-recreate api
```

Para comprobar ausencia de reenvío, mantén el despachador desactivado durante la
prueba, genera un evento, ejecuta el procesador y verifica el mensaje recibido
en Mailpit y el estado Enviado. Ejecútalo de nuevo y comprueba que no aparece otra
entrega del mismo evento. Restaura el despachador al terminar.

SQL Server reclama cada correo mediante UPDATE condicional y un identificador
exclusivo. El reclamo vence a los dos minutos, permitiendo recuperar trabajos
interrumpidos. Los intentos se registran antes del envío. No se comparte un
DbContext entre procesadores. Si SMTP acepta y el proceso cae antes de registrar
Enviado, al recuperar el reclamo puede repetirse la entrega. SMTP no permite
garantizar entrega exactamente una vez en esa ventana. Los correos fallidos se
conservan para consulta.

### Reportes

Ambos reportes requieren sesión. Administrador consulta información general,
Estándar únicamente la propia. Sus DTO JSON contienen grupos y totales, sin
devolver las entidades individuales.

| Reporte | Endpoint | Pregunta y fecha utilizada |
| --- | --- | --- |
| Core | GET `/api/reportes/solicitudes?inicio=...&fin=...` | ¿Cuántas solicitudes se crearon por estado en el periodo? Filtra por CreadaEn. |
| Reservas | GET `/api/reportes/reservas?inicio=...&fin=...` | ¿Qué espacios reciben más reservas y cuántas horas se reservan en cada uno? Filtra por intersección del intervalo. |

Ejemplos ilustrativos de rutas para Swagger, no comandos PowerShell. Sustituye
los rangos por UTC que incluya las solicitudes creadas y el intervalo de las
reservas de tu evaluación; las fechas siguientes no generan datos de prueba:

```text
GET /api/reportes/solicitudes?inicio=2026-11-01T00:00:00Z&fin=2026-12-01T00:00:00Z
GET /api/reportes/reservas?inicio=2026-11-01T00:00:00Z&fin=2026-12-01T00:00:00Z
```

- Rango `[inicio, fin)`, UTC obligatorio, inicio anterior al fin y máximo 3660 días.
  Un rango inválido devuelve 400.
- Solicitudes devuelve `total` y `grupos` por estado real. Pendiente=1,
  Aprobada=2, Rechazada=3 y Aplicada=4. El flujo normal de aprobación guarda
  Aplicada cuando concede el permiso; Aprobada es un paso intermedio.
- Reservas devuelve `totalReservas`, `totalHoras` y grupos por espacio/estado
  con identificador, nombre, número de reservas y horas dentro del periodo.
  Excluye Cancelada. Incluye Pendiente, Confirmada y Completada para representar
  demanda vigente o realizada. Las horas son la suma de intersecciones por reserva.
- Una reserva parcialmente solapada aporta solo las horas dentro del rango.
  Las duraciones se calculan en ticks de 100 ns y se convierten a horas.
- Los grupos de reservas se ordenan por horas, cantidad de reservas y espacio.
  Los totales resumen los grupos, no un listado de entidades.
- Sin resultados se devuelve una lista vacía y totales cero.
- Para probar el filtro, crea datos en dos periodos distintos y consulta ambos.
  Un Estándar no recibe registros ajenos ni siquiera en los totales.
- `GroupBy`, `Count` y `Sum` se ejecutan en SQL Server. Se ajusta `DateDiffSecond`
  con las diferencias de milisegundos, microsegundos y nanosegundos para conservar
  la precisión de `datetime2(7)`: cruzar un límite de segundo no redondea la duración.
  Los ticks se suman como decimal y luego se convierten a horas; el resultado
  público `double` conserva su redondeo habitual de representación binaria.
  La variante EF InMemory calcula la misma diferencia mediante `DateTime.Ticks`.

### Recorrido manual mínimo

Usa Swagger, la captura Mailpit y las dos cuentas activadas:

1. Estándar crea una solicitud `{"permiso":4}`. Administrador revisa su bandeja, contador y correo recibido.
2. Administrador aprueba la solicitud ajena. Estándar revisa la resolución y crea una reserva futura en un espacio activo; antes de aprobar el permiso, esa creación debe rechazarse.
3. Administrador confirma la reserva. Su propietario verifica interna y correo. Repetir la confirmación debe rechazarse sin notificar de nuevo.
4. Estándar marca una notificación propia leída, repite y comprueba que no cambia su fecha. Otra cuenta no puede marcarla.
5. Estándar desactiva su preferencia, solicita otro permiso válido y Administrador lo rechaza con motivo. Debe conservarse la interna sin correo opcional nuevo. Restaurar la preferencia no envía retroactivamente ese correo.
6. Administrador consulta la cola. Estándar debe recibir 403, y sin sesión 401.
7. Consulta ambos reportes con rangos UTC que contengan las solicitudes recién creadas y el intervalo de las reservas. Los ejemplos de fechas son ilustrativos: ajusta las fechas al momento de evaluación. Compara Admin global y Estándar propio, otro rango y un rango vacío.
8. Completa los seis frentes acumulados: registro/activación por cola SMTP; login/logout y 403; recuperación; permisos y transición inválida; reserva válida/prohibida; documento persistente tras down/up.

Los criterios previos están en [AGENTS.md](AGENTS.md) y [criterios de aceptación](docs/criterios-notificaciones-reportes.md). La correspondencia entre requisitos, pruebas y resultados está en [verificación de Práctica 5](docs/verificacion-practica-5.md).

## Pruebas automatizadas

**Desde la raíz**, con .NET SDK 10:

```powershell
dotnet restore SISReservasEspacios.slnx
dotnet build SISReservasEspacios.slnx
dotnet test SISReservasEspacios.slnx
```

El **comando único de suite** es `dotnet test SISReservasEspacios.slnx`; restaura y compila si hace falta. No levanta la API. Los proyectos de prueba usan EF InMemory y SMTP de loopback: no necesitan una base SQL, Docker, claves meteorológicas ni un SMTP externo. InMemory no sustituye las pruebas de SQL Server real.

La base histórica de Práctica 5 pasó **207 pruebas**. El cierre inicial de Asignación 3 incorporó cinco casos: una prueba de lectura concurrente, dos casos de errores controlados y dos de independencia/modelo EF. Aquella ejecución inicial pasó **212 aprobadas, 0 fallidas y 0 omitidas**, con compilación **0 errores y 0 advertencias**. Los resultados se registran en la [evidencia de Asignación 3](docs/evidencias/asignacion-3/resultados.json). No se atribuyen al commit histórico resultados de otra versión.

La prueba de independencia crea una copia temporal de fuentes, excluye Reservas y ejecuta piezas reales del Core con InMemory. Necesita SDK 10/NuGet; no inicia la API ni requiere SQL. Conserva su log en `Temp/SISReservas-H03-*`.

Existe una intermitencia histórica en `ConfiguracionCorreoTests`. No se ha corregido ni se declara resuelta por una ejecución aprobada. Si reaparece, conserva el nombre de prueba, error y contexto; no elimines ni deshabilites pruebas para obtener una suite verde.

## Pruebas de integración

**Desde la raíz**, con Windows PowerShell 5.1 o posterior, Git y Docker Desktop iniciado:

Usa una terminal de pruebas sin una variable de proceso `MSSQL_SA_PASSWORD`
heredada de otra instalación. El script espera la contraseña pública de la
plantilla para sus sondas de arranque; una variable heredada tendría prioridad
sobre esa plantilla y haría fallar la prueba. No imprimas su valor para revisarla.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Validar-Practica5.ps1
```

El bypass aplica únicamente al proceso de esa ejecución, no cambia la política global. No instales PowerShell 7 para ejecutar este script.

El script versionado:

- Comprueba Docker CLI/Compose y motor; crea un snapshot temporal y un proyecto `sisp5-<id>` con volúmenes nuevos.
- Usa `.env.example`, sin leer el `.env` privado del proyecto. Incluye archivos de trabajo no ignorados; para una evaluación reproducible ejecútalo desde un clon limpio de la referencia aprobada.
- Excluye `frontend/test-results/`; en la copia temporal mantiene un inspector de correo para evitar contenedores auxiliares repetidos.
- Usa el puerto API 15094 si está disponible y selecciona otro libre si está ocupado; imprime el puerto y la carpeta de evidencia.
- Ejecuta HTTP contra SQL Server real y recepción Mailpit, errores de arranque, dos migradores concurrentes, reportes, roles, preferencias, reintentos y procesadores SMTP concurrentes.
- Repite cinco ciclos down/up con la misma base/volúmenes, verificando datos y contenido de documentos.
- Escribe `resultados.json` y, al completar el recorrido, `cobertura.json`, además de registros de diagnóstico. Un archivo de resultados parciales no significa que la suite completa pasó.
- Conserva sus recursos Docker al terminar. No detiene automáticamente los contenedores Docker ni elimina los volúmenes; la limpieza es una decisión del operador.

No es requisito ejecutar el modo opcional `-Local` ni `-ProbarWeatherReal` para esta regresión. Por defecto prueba el rechazo controlado de WeatherAPI sin clave; una consulta real al proveedor requiere una clave privada y cobertura horaria válida.

La ejecución histórica de Práctica 5 pasó **243 comprobaciones, 172 peticiones HTTP y 38 rutas/métodos**. Asignación 3 repitió esta regresión por separado: **243 aprobadas, 0 fallidas, 172 peticiones HTTP, 38 rutas/métodos, siete migraciones y cinco ciclos de persistencia**; consultar la [evidencia actual](docs/evidencias/asignacion-3/resultados.json). La recepción SMTP local no demuestra entrega a buzones externos. La evolución anterior permanece en [verificación de Práctica 5](docs/verificacion-practica-5.md).

Para detener exclusivamente una ejecución de integración, sustituye estas variables por los valores que **imprimió esa misma ejecución** y verifica su identidad antes:

```powershell
$proyectoPrueba = Read-Host 'Nombre exacto sisp5-... de esta ejecución'
$carpetaPrueba = Read-Host 'Ruta completa de evidencia de esa misma ejecución'
docker compose -p $proyectoPrueba --env-file "$carpetaPrueba/.env" -f "$carpetaPrueba/docker-compose.yml" ps
docker compose -p $proyectoPrueba --env-file "$carpetaPrueba/.env" -f "$carpetaPrueba/docker-compose.yml" stop
```

No uses comandos globales de prune ni borres volúmenes para liberar espacio. Un fallo por subredes agotadas exige revisar los proyectos temporales, sin eliminar recursos del proyecto principal.

## Persistencia de datos

| Volumen | Contenido |
| --- | --- |
| `sql-data` | Usuarios, sesiones, tokens, cola, permisos, reservas, notificaciones y metadatos de documentos. |
| `documentos-data` | Archivos físicos en `/app/data/documentos`. |

**Comprobación manual desde la raíz del proyecto evaluado:**

1. Registra/activa una cuenta, inicia sesión y sube un archivo; conserva su ID, tamaño y contenido original.
2. Consulta listado y descarga. Crea una reserva y conserva su ID; marca una notificación propia leída.
3. Ejecuta `docker compose down` y luego `docker compose up --build`, sin `-v`, conservando carpeta/proyecto Compose y credenciales SQL.
4. Inicia sesión de nuevo; consulta las reservas propias y la notificación, lista el documento y compara la descarga con el original.

La ejecución anterior verificó los cinco ciclos: API HTTP 200 y conservación de preferencias, notificación leída, cola Fallida, reporte y documento idéntico en cada uno. No se repitieron en esta fase documental.

La eliminación lógica de documentos conserva registro y archivo. El buzón Mailpit no tiene volumen persistente y puede vaciarse al recrearse; eso no implica pérdida de los datos SQL. No cambies el nombre del proyecto Compose si deseas reutilizar sus volúmenes.

## Desarrollo local con Visual Studio y LocalDB

Este camino es independiente de Docker: las cuentas, sesiones y colas de LocalDB no son las del contenedor. La evaluación recomendada usa Compose. El `.env` de Compose **no se carga** al ejecutar `dotnet run`.

Requiere SDK 10, LocalDB instalado y la instancia `MSSQLLocalDB` disponible. En Visual Studio usa un IDE compatible con `net10.0`; no se presupone soporte nativo en Visual Studio 2022. Puedes usar su terminal integrada con el SDK 10 instalado. Selecciona el perfil `http` si ejecutas desde un IDE compatible; no uses el perfil Container para este camino.

**Desde la raíz, en la misma terminal PowerShell que iniciará la API**, este ejemplo elige un catálogo nuevo con identificador único y no reutiliza bases existentes:

```powershell
$baseLocal = 'SISReservas_Local_' + [guid]::NewGuid().ToString('N')
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=$baseLocal;Trusted_Connection=True;TrustServerCertificate=True;"
$env:Database__ApplyMigrations = 'true'
$env:AppBaseUrl = 'http://localhost:5054'
$env:Documentos__Directorio = Join-Path $env:LOCALAPPDATA "SISReservasEspacios\$baseLocal\documentos"
$env:EmailDispatcher__Enabled = 'false'
dotnet run --project backend/SISReservas.Api --launch-profile http
```

Anota `$baseLocal` para reutilizar esa base después; volver a generar el GUID crea otra distinta. Swagger abre en http://localhost:5054/swagger. Con despacho deshabilitado **no puedes completar activación/recuperación por correo**. Las variables de terminal no se heredan retroactivamente por un Visual Studio ya abierto.

Para correo local se necesita un servidor SMTP de captura disponible. Opcionalmente, **solo en este camino local**, puede usarse Mailpit independiente con puertos loopback. Desde otra terminal en la raíz y sin ejecutar simultáneamente otro contenedor con ese nombre/puertos:

```powershell
docker run --rm --name sisreservas-mailpit-local-p5 -p 127.0.0.1:8025:8025 -p 127.0.0.1:1025:1025 axllent/mailpit:v1.33.0
```

Tras detener la API local con Ctrl+C, en su terminal configura y vuelve a arrancar:

```powershell
$env:SMTP_HOST = '127.0.0.1'
$env:SMTP_PORT = '1025'
$env:SMTP_FROM = 'reservas@test.invalid'
$env:SMTP_ENABLE_SSL = 'false'
$env:SMTP_USE_AUTH = 'false'
$env:EmailDispatcher__Enabled = 'true'
dotnet run --project backend/SISReservas.Api --launch-profile http
```

Consulta el buzón local en http://localhost:8025 y ejecuta los mismos flujos de Swagger. Esta captura opcional requiere Docker; si deseas prescindir de él por completo, proporciona un SMTP instalado por el operador y configura sus datos privados. No se instala ningún servidor desde esta guía. Usa Ctrl+C para detener los procesos iniciados en sus terminales.

Las variables `$env:...` son de la terminal actual: no modifican configuración global. Al cerrar esa terminal dejan de aplicarse a procesos nuevos. No guardes secretos en `launchSettings.json`. Los procedimientos largos anteriores están archivados en [la guía acumulada](docs/entregas/guia-acumulada-practica-4.md); sus rutas y pendientes reflejan su etapa histórica.

## Solución de problemas

| Síntoma | Comprobación |
| --- | --- |
| Clon sin las correcciones | Comprueba referencia y hash. La etiqueta corregida debe publicarse antes de usarla desde GitHub. |
| Puerto 5054 ocupado | Cambia `APP_PORT` en el entorno privado, recrea la API y usa el puerto nuevo en Swagger y enlaces. |
| SQL no pasa a healthy | Revisa `docker compose logs --tail=50 sqlserver`, recursos y contraseña del volumen. No borres datos. |
| Error 1801, catálogo offline o timeout de migración | Revisa referencia de código y `docker compose logs --tail=50 api`. El arranque corregido espera recuperación; errores permanentes requieren intervención. No reemplaces migraciones por EnsureCreated. |
| `/` devuelve 404 | Usa `/swagger` o `/swagger/v1/swagger.json`; la raíz no es comprobación de salud. |
| No aparece un correo | Comprueba despachador habilitado, destino SMTP, estado/intentos como Admin y mensaje nuevo en Mailpit. Un correo Fallido no se reenvía automáticamente. |
| Activación rechazada | Usa el último enlace, antes de su vencimiento y en la API del mismo catálogo; el reenvío invalida los anteriores. |
| Login 200 sin sesión | Puede ser cuenta inactiva o bloqueada; inspecciona mensaje y activa la cuenta o espera el bloqueo. No equivale a login exitoso. |
| HTTP 401 / 403 / 404 | Sesión ausente/revocada / falta de rol o permiso / recurso inexistente o ajeno protegido. |
| HTTP 400 en fechas | Usa ISO 8601 UTC con Z, inicio anterior al fin y rangos admitidos. |
| WeatherAPI 503 | Clave, cuota, cobertura del pronóstico o proveedor; tras cambiar la clave recrea la API. |
| Integración falla por redes/subredes agotadas | Identifica proyectos de prueba propios antes de detener/liberar sus contenedores/redes; conserva volúmenes. No ejecutes limpieza global. |
| ConfiguracionCorreoTests intermitente | Conserva el fallo y compáralo con la limitación conocida; no se considera corregido ni se oculta. |

El protocolo SMTP no comparte transacción con SQL: si el servidor acepta el mensaje y el proceso cae antes de guardar Enviado, puede repetirse al recuperar el reclamo. No se garantiza entrega exactamente una vez. La composición Development/HTTP no sustituye una configuración de producción con HTTPS y secretos seguros.

## Documentación complementaria e historial

Para el comportamiento actual prioriza este README, [criterios de Práctica 5](docs/criterios-notificaciones-reportes.md) y [verificación de Práctica 5](docs/verificacion-practica-5.md). La máquina de estados sigue vigente. Las guías de fases y el archivo acumulado conservan evidencias históricas: sus frases de SMTP pendiente o ausencia de reclamos distribuidos no describen la implementación actual.

| Entrega | Etiqueta Git | Funcionalidades | Documentación |
| --- | --- | --- | --- |
| Asignación 1 | `asignacion-1` | Control de acceso | [Bitácora](docs/bitacora-asignacion-1.md) |
| Práctica 1 | `practica-1` | Estructura del proyecto y entidades | [Guía histórica](docs/entregas/guia-acumulada-practica-4.md) y README de la etiqueta |
| Asignación 2 | `asignacion-2` | Gestión de permisos y persistencia | [Bitácora](docs/bitacora-asignacion-2.md) |
| Práctica 2 | `practica-2`, defectos 1 a 6 | Pruebas y mutantes de permisos | [Defectos históricos](docs/entregas/guia-acumulada-practica-4.md#práctica-2-defectos-históricos-de-gestión-de-permisos) |
| Práctica 3 | `practica-3`, defectos 1 a 4 | Reservas, estados e integración con permisos | [Defectos y evidencias](docs/entregas/guia-acumulada-practica-4.md#defectos) |
| Práctica 4 | `practica-4` | Documentos, Docker, persistencia y WeatherAPI | [Guía publicada y evidencias acumuladas](docs/entregas/guia-acumulada-practica-4.md) |
| Práctica 5 | [practica-5 (publicada)](https://github.com/SimonCrucey/SISReservasEspacios/tree/practica-5) | Notificaciones, cola robusta y reportes | [Criterios](docs/criterios-notificaciones-reportes.md), [verificación](docs/verificacion-practica-5.md) |
| Asignación 3 | `asignacion-3` (entrega local, sin publicar) | Auditoría y correcciones H-01, H-02 y H-03 | [Auditoría](docs/auditoria-asignacion-3.md), [bitácora](docs/bitacora-asignacion-3.md), [evidencia](docs/evidencias/asignacion-3/resultados.json) |

Los nombres completos de los mutantes son `practica-2-defecto-N` (N=1…6) y `practica-3-defecto-N` (N=1…4). Cada etiqueta conserva su versión evaluable. La guía archivada mantiene tablas detectoras, hashes, auditorías, evidencia SMTP y limitaciones de sus respectivas etapas. Sus afirmaciones históricas se interpretan en ese contexto.

La referencia histórica de Práctica 4 es `64125eb9c74ab5811edb47cfaabf88dc23f32ddb`.

- [Máquina de estados y cobertura](docs/maquina-de-estados.md).
- [Control de acceso y SMTP local](docs/control-acceso-local.md).
- [Auditoría de Práctica 3](docs/auditoria-practica-3.md).
- [Verificación de fase 4](docs/verificacion-fase-4.md).
- [Verificación de fase 6](docs/verificacion-fase-6.md).
- [Evidencia funcional HTTP/SQL de fase 6](docs/evidencia-fase-6.json).
- [Archivo de entregas y procedimientos anteriores](docs/entregas/guia-acumulada-practica-4.md).

## Asignación 3: auditoría y regresión por hallazgo

- H-01: la lectura concurrente conserva la primera fecha persistida; UPDATE SQL atómico con propietario y LeidaEn NULL.
- H-02: los fallos internos se controlan también en Development; indisponibilidad de persistencia devuelve 503 genérico, sin traza, SQL, rutas ni cabeceras. Los rechazos de negocio conservan sus códigos.
- H-03: las extensiones de Reservas están en archivos parciales de su módulo; Core puede compilar y ejecutar permisos, bandeja y reporte de solicitudes sin incluir negocio. El modelo EF y las siete migraciones no cambian.

Desde la raíz, además del comando único `dotnet test SISReservasEspacios.slnx`, se puede demostrar cada prueba sobre el padre de su corrección:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Verificar-RegresionesAsignacion3.ps1
```

Requiere Git, Windows PowerShell 5.1 o posterior y SDK 10/NuGet. Exporta copias temporales, incorpora solo la prueba nueva, exige fallo anterior y aprobación actual; no cambia ramas, etiquetas ni bases. Los tres commits de corrección citan su hallazgo. La [bitácora](docs/bitacora-asignacion-3.md) registra qué se pidió, qué falló, por qué pasó y las mejoras visibles en AGENTS y criterios.

La publicación de Asignación 3 sigue pendiente. El `git checkout asignacion-3` del inicio rápido es el procedimiento oficial desde GitHub después de publicar la etiqueta definitiva. Para la preparación previa local, usar `entrega-asignacion-3` mediante «Clon local limpio antes de publicar». La rama histórica `trabajo-asignacion-3` se conserva como respaldo de la sesión y no es la versión que debe evaluar el profesor. No se requiere mover ninguna etiqueta anterior.

## Revisión personal de Asignación 3: comprobación definitiva

La [guía de revisión](docs/revision-personal-asignacion-3.md) sustituye el alcance limitado del CoreSmoke inicial: CoreOnly conserva Program.cs y las siete migraciones, excluye los tipos de Reservas y ejecuta el host real. H-01 dispone de SQL coordinado versionado; H-02 comprueba el pipeline real y recuperación.

Desde la raíz, con Docker Desktop y PowerShell 5.1+:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Validar-H01Sql.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Validar-Practica5.ps1 -CoreOnly
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Validar-Practica5.ps1
```

Cada ejecución crea datos/volúmenes propios a partir del ejemplo público. Si Docker tiene pools agotados o solapados, añadir `-SubredDocker` con un CIDR libre elegido por el operador; no es obligatorio ni existe una red universal fija. Para el arranque Compose manual se puede usar el overlay opcional `docker-compose.red-pruebas.yml` definiendo `DOCKER_TEST_SUBNET`. No eliminar redes ni volúmenes ajenos.

El comando principal sigue siendo `dotnet test SISReservasEspacios.slnx`, sin API, SQL ni Docker. Las mejoras posteriores están en la rama candidata; `asignacion-3` y su respaldo conservan e8c18c4 hasta autorización. No evaluar las mejoras mediante esa etiqueta antigua todavía.

Resultados de la revisión posterior: **214 xUnit aprobadas**, **87 comprobaciones / 62 HTTP en CoreOnly** y **248 comprobaciones / 173 HTTP en el sistema completo**, sin fallos; siete migraciones y cinco ciclos completos de persistencia. Son resultados nuevos de la rama candidata, no de la etiqueta antigua. [Evidencia de revisión](docs/evidencias/asignacion-3/revision-personal.json).

## Historial consolidado de Asignación 3

Los commits definitivos de corrección son `fcc3846` (H-01), `158b753` (H-02) y `35aedc0` (H-03), hijos consecutivos desde practica-5. Los hashes anteriores de la sesión se conservan como históricos, no como identificadores de la entrega consolidada. [Auditoría y referencias](docs/auditoria-asignacion-3.md).

La rama de trabajo y los respaldos están intactos. El historial ya está consolidado; las menciones anteriores a consolidación pendiente pertenecen a la cronología de revisión. La etiqueta antigua no representa aún esta rama: no entregar su referencia hasta autorizar la actualización.

El verificador RED/GREEN comprueba padre, corrección exacta y candidato final. El ejecutor SQL admite referencias explícitas:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\integracion\Validar-H01Sql.ps1 -Antes 0b887de -Despues fcc3846
```

### Arranque local de evaluación desde el clon de la entrega

Desde la raíz, copiar únicamente el ejemplo público. Elegir un nombre de proyecto de prueba propio y un puerto libre. Estas variables solo afectan a la terminal actual.

```powershell
Copy-Item .env.example .env
$env:COMPOSE_PROJECT_NAME='evaluacion-asig3'
$env:APP_PORT='15150'
$env:SMTP_HOST='mailpit'
$env:SMTP_PORT='1025'
$env:SMTP_USE_AUTH='false'
$env:SMTP_ENABLE_SSL='false'
$env:SMTP_FROM='reservas@test.invalid'
$env:SMTP_USERNAME=''
$env:SMTP_PASSWORD=''
$env:EmailDispatcher__Enabled='true'
docker compose -p evaluacion-asig3 up --build -d
```

Abrir `http://127.0.0.1:15150/swagger`. Si la red predeterminada no puede crearse, elegir una subred libre (sin solapar redes existentes) y ejecutar la alternativa versionada, usando el mismo proyecto:

```powershell
# Ejemplo de laboratorio: comprobar antes que ese CIDR esté libre.
$env:DOCKER_TEST_SUBNET='10.251.74.0/24'
docker compose -p evaluacion-asig3 -f docker-compose.yml -f docker-compose.red-pruebas.yml up --build -d
```

La subred es configurable y opcional, no un valor obligatorio. Al detener usar los mismos archivos Compose y `down` sin `-v`; no borrar volúmenes. Para CoreOnly añadir `-f docker-compose.core.yml`; no se sustituye Program.cs. Las instrucciones de cuentas, SMTP e inspección de correo descritas anteriormente siguen vigentes.

### Clon local limpio antes de publicar

Desde la raíz del repositorio local que contiene la rama consolidada, se puede revisar sin depender de GitHub ni cambiar la etiqueta:

```powershell
$origen=(Get-Location).Path
$destino=Join-Path $env:TEMP ('asig3-evaluacion-'+[guid]::NewGuid().ToString('N'))
git clone --no-hardlinks --branch entrega-asignacion-3 $origen $destino
Set-Location $destino
dotnet build SISReservasEspacios.slnx
dotnet test SISReservasEspacios.slnx
```

Continuar allí con «Arranque local de evaluación desde el clon de la entrega» y los scripts versionados. `COMPOSE_PROJECT_NAME` hace que los comandos posteriores `docker compose exec` de esta guía utilicen el mismo proyecto aislado; el puerto y SMTP se configuran solo en esa terminal.

**Verificación del historial consolidado:** se repitieron 214 xUnit, 248 comprobaciones integrales/173 HTTP y 87 CoreOnly/62 HTTP sin fallos sobre las correcciones definitivas. El verificador exige RED en cada padre y GREEN en su corrección exacta y candidato. [Referencias y resultados de entrega](docs/evidencias/asignacion-3/entrega-consolidada.json). Los resúmenes históricos omiten identificadores de filas y texto libre; sus originales siguen preservados en la rama de trabajo.

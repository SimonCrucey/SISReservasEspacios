# SISReservasEspacios

Sistema de reservas y disponibilidad de salones, canchas y equipos, con Control
de acceso, Gestión de permisos y módulo de Reservas. Proyecto académico de
Programación III · TDS-007 · ITLA · 2026-C-3. Esta guía reúne la configuración,
el uso de los módulos y las entregas acumulativas; los resultados históricos se
distinguen de las instrucciones para ejecutar la versión actual.

Estado: 138 pruebas aprobadas desde un clon local limpio y 73 verificaciones HTTP/SQL
históricas. Las cinco etiquetas anotadas practica-3 y practica-3-defecto-1 a 4
están publicadas en GitHub; los cuatro defectos fueron creados y detectados por la suite.
La última comprobación mediante git ls-remote origin confirmó la base publicada
be2080d49a0f855112fe9849231e1927f5422966. La corrección documental y la
reconstrucción de mutantes descritas más abajo están preparadas solo localmente;
las etiquetas publicadas todavía no se han sustituido. La recepción SMTP local y los
flujos de activación y recuperación fueron comprobados mediante mensajes realmente
recibidos en Mailpit: 30 verificaciones aprobadas. No se comprobó entrega a buzones externos.
El cierre de Práctica 3 se encuentra más abajo. La [auditoría de fase 7](docs/auditoria-practica-3.md)
es un registro histórico anterior a crear los mutantes, publicar y comprobar SMTP;
no representa por sí sola el estado actual.

## Tecnologías utilizadas

- C# y ASP.NET Core Web API sobre .NET 10.
- Entity Framework Core 10 con SQL Server; LocalDB para desarrollo en Windows.
- Autenticación por sesiones persistidas, transportadas como Bearer (no JWT).
- Swagger/OpenAPI para explorar y probar los endpoints.
- xUnit y EF Core InMemory para la suite sin API ni base SQL.
- SMTP y cola persistida de correos; Mailpit es una opción de captura local.

El repositorio actual contiene la API; no se requieren Node.js, npm ni React para
seguir esta guía. Las instrucciones iniciales de frontend eran un prototipo y no
corresponden a una aplicación frontend ejecutable incluida actualmente.

## Repositorio y clonación

Repositorio: [SimonCrucey/SISReservasEspacios](https://github.com/SimonCrucey/SISReservasEspacios).

```powershell
git clone https://github.com/SimonCrucey/SISReservasEspacios.git
cd SISReservasEspacios
```

main contiene la guía acumulativa y el cierre documental. Para evaluar una entrega
histórica, seleccionar su etiqueta según el índice siguiente; no mezclar código
de un mutante con una ejecución normal del sistema.

## Índice de entregas y documentación

| Entrega | Etiquetas | Documentación |
| --- | --- | --- |
| Asignación 1: Control de acceso | asignacion-1 | [Bitácora de Asignación 1](docs/bitacora-asignacion-1.md) |
| Práctica 1 | practica-1 | README conservado en esa etiqueta y bitácora de Asignación 1 |
| Asignación 2: Gestión de permisos | asignacion-2 | [Bitácora de Asignación 2](docs/bitacora-asignacion-2.md) |
| Práctica 2: pruebas y mutantes de permisos | practica-2; practica-2-defecto-1 a 6 | Tabla histórica de defectos en esta guía; README de practica-2 y de 2f2a056 |
| Práctica 3: Reservas e integración | practica-3; practica-3-defecto-1 a 4 | Secciones de Reservas, Defectos y Cierre de evidencia de esta guía |
| Validaciones funcionales y correo | Sin nuevas etiquetas de entrega | [Fase 6](docs/verificacion-fase-6.md), [evidencia HTTP/SQL](docs/evidencia-fase-6.json), [guía local de correo](docs/control-acceso-local.md) |

La [máquina de estados](docs/maquina-de-estados.md) documenta Reservas y su
cobertura por fila. Las guías de fases 4, 6 y 7 reflejan el momento de su auditoría;
para resultados posteriores consultar el cierre de evidencia de esta guía.

## Contenedor

La validación final en Docker Desktop pasó 114 comprobaciones en una copia de trabajo aislada. Incluyó el reinicio sin borrar volúmenes y la comparación del documento descargado con el original. La entrega se inicia con los comandos siguientes.

Desde la raíz de un clon limpio, con Docker y Compose en modo de contenedores
Linux y un equipo compatible con la imagen de SQL Server, ejecutar:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

En Bash, copiar el archivo con `cp .env.example .env`. El ejemplo incluye una
contraseña pública de demostración para el entorno local. Conserva el `.env`
de esa instalación cuando vuelvas a levantar el mismo volumen SQL.

Con el puerto de ejemplo, acceder a Swagger en `http://localhost:5054/swagger`.
Si cambias `APP_PORT`, usa el nuevo puerto en esa dirección. Compose publica
solo la API en loopback. SQL Server queda en la red interna, y la API espera
una consulta exitosa a SQL antes de aplicar las migraciones y atender peticiones.
La ejecución local descrita más abajo sigue usando su configuración habitual.

Para verificar el arranque, ejecutar `docker compose ps` desde otra terminal.
SQL Server debe aparecer como `healthy` y Swagger debe responder en la dirección
indicada. El despacho SMTP se habilita con la configuración de correo siguiente.

Detener con `Ctrl+C` y ejecutar `docker compose down` para retirar los
contenedores. Los volúmenes `sql-data` y `documentos-data` se conservan.
Para volver a levantar los servicios, ejecutar `docker compose up` desde la
misma carpeta. Los archivos subidos se guardan en `documentos-data`, montado
en `/app/data/documentos`.

### Correo en Docker con Mailpit

Para la evaluación local, configura estas variables en el `.env` de la raíz.
Conserva los valores de SQL Server y del puerto de tu instalación.

```dotenv
EmailDispatcher__Enabled=true
SMTP_HOST=mailpit
SMTP_PORT=1025
SMTP_FROM=reservas@test.invalid
SMTP_ENABLE_SSL=false
SMTP_USE_AUTH=false
SMTP_USERNAME=
SMTP_PASSWORD=
```

```powershell
docker compose up -d --build
docker compose logs --tail=50 api
```

Mailpit arranca también con el comando principal, recibe SMTP dentro de la red
Compose y no tiene reenvío configurado. No requiere activar un perfil adicional.
Al habilitar el despachador se procesan también los correos pendientes del
catálogo conectado. Usa esta captura solo en un entorno local autorizado.
La API sigue siendo el único servicio con un puerto publicado.

Consulta el buzón desde una terminal privada con el contenedor auxiliar.
El comando de listado muestra el identificador y el destinatario. Sustituye
`ID` en el segundo comando por el identificador del mensaje que necesitas.
Su respuesta contiene el texto con el enlace o código de un solo uso.

```powershell
docker compose run --rm --no-deps mail-inspector -fsS http://mailpit:8025/api/v1/messages
docker compose run --rm --no-deps mail-inspector -fsS http://mailpit:8025/api/v1/message/ID
```

Mantén la salida del mensaje fuera de capturas públicas, logs compartidos y Git.
El log `SMTP aceptó el correo` indica aceptación por SMTP. El mensaje observado
en el buzón confirma la recepción local.

1. **Activación.** En Swagger, registra una cuenta ficticia con
   `POST /api/auth/register`. Espera el siguiente ciclo del despachador,
   consulta el mensaje nuevo y abre su enlace desde el anfitrión.
   Si la cuenta ya existía y seguía inactiva, usa
   `POST /api/auth/resend-activation` con el correo como string JSON.
2. **Recuperación.** Para una cuenta existente, envía
   `POST /api/passwords/recovery` con `{"email":"correo-de-la-cuenta"}`.
   Usa la dirección de la cuenta. Busca el mensaje nuevo y usa
   su código en `POST /api/passwords/reset` con los campos `token` y
   `nuevaPassword`. El código dura 30 minutos y es de un solo uso.
   Inicia sesión con la nueva contraseña. Si la cuenta aún está inactiva,
   completa también la activación.

Para un proveedor externo, sustituye host, puerto y remitente, habilita
`SMTP_ENABLE_SSL` para STARTTLS y usa `SMTP_USE_AUTH=true` con usuario y
contraseña privados en el `.env` ignorado. Conserva una sola instancia del
despachador para la misma cola.

## Desarrollo local: Visual Studio, Docker y SQL Server

| Modo | Instancia y base | Autenticación SQL |
| --- | --- | --- |
| Visual Studio, perfil `http` | `(localdb)\MSSQLLocalDB`, base `SISReservasEspacios` | Windows |
| Docker Compose | `sqlserver,1433`, base `SISReservasEspacios` | SQL Server |

Son catálogos independientes. Usuarios, sesiones y correos pertenecen a la
instancia en la que se crearon. Con el perfil `http` y sin overrides,
Visual Studio usa `appsettings.json`. En desarrollo, la configuración de
`appsettings.Development.json` y User Secrets puede reemplazar ese archivo.
`ConnectionStrings__DefaultConnection` prevalece sobre esos valores, y un
argumento de línea de comandos puede prevalecer sobre el entorno. Compose
inyecta su propia cadena en el contenedor. El archivo `.env` de Compose
no cambia por sí solo la conexión del perfil `http`.

Para ejecutar desde Visual Studio 2022, abre `SISReservasEspacios.slnx`,
selecciona `SISReservas.Api` como proyecto de inicio y elige el perfil
`http`. Pulsa F5 y abre `http://localhost:5054/swagger`. Si Compose ya
ocupa ese puerto, detén su API o usa un puerto distinto. El perfil local
requiere un catálogo actualizado. Para una instalación nueva usa la sección
«Clon limpio y base nueva». Si quieres actualizar una base local existente,
respalda sus datos y confirma el destino antes de aplicar migraciones.

Para ejecutar Docker sin abrir el puerto SQL, conserva el `.env` de tu
instalación y ejecuta `docker compose up -d --build`. Si vas a probar correo
local, revisa la sección «Correo en Docker con Mailpit»; ya está incluido.

### Consultar SQL Server Docker desde Visual Studio

Desde la misma carpeta y usando el mismo proyecto Compose, ejecuta:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
docker compose -f docker-compose.yml -f docker-compose.dev.yml ps
```

El archivo adicional publica SQL solo en `127.0.0.1:14333`. En Visual Studio
abre **Ver → Explorador de objetos de SQL Server → Agregar SQL Server**.
Indica `127.0.0.1,14333` como servidor, elige **Autenticación de SQL Server**,
usa el usuario `sa` y la contraseña de la instancia existente. Si la instancia
se creó con el `.env` actual, es el valor de `MSSQL_SA_PASSWORD`. Con el
certificado local de demostración, marca **Confiar en el certificado del servidor**
en las opciones de conexión. Expande **Bases de datos → SISReservasEspacios → Tablas**.
Para LocalDB, agrega `(localdb)\MSSQLLocalDB` con autenticación de Windows.

Ejecuta la misma consulta en cada instancia. Sustituye el correo de ejemplo por
el de la cuenta buscada:

```sql
USE [SISReservasEspacios];
DECLARE @correo nvarchar(320) = N'cuenta@test.invalid';
SELECT @@SERVERNAME AS Servidor, DB_NAME() AS Base;
SELECT Activo, IntentosFallidos, BloqueadoHasta
FROM dbo.Usuarios WHERE Email = @correo;
SELECT Estado, CreadoEn, EnviadoEn
FROM dbo.CorreosEnCola WHERE Destinatario = @correo ORDER BY CreadoEn DESC;
SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
```

La cola pertenece a SQL Server. `Pendiente` incluye mensajes aún no intentados
y mensajes que serán reintentados tras un fallo. `Enviado` indica que SMTP
aceptó el mensaje. Mailpit muestra las entregas que él capturó y su buzón de
demostración puede quedar vacío si se recrea su contenedor. Un mensaje viejo
de activación en Mailpit no confirma que una recuperación reciente se haya
generado. El endpoint de recuperación responde igual si la cuenta no existe
en el catálogo de la API. En ese caso, registra y activa una cuenta de prueba
en Docker o recupera la cuenta usando la API conectada a LocalDB.

Antes de solicitar activación o recuperación de prueba, confirma en el
`.env` que `SMTP_HOST=mailpit`, `SMTP_PORT=1025` y que el perfil de correo
está activo. Usa el buzón interno para obtener el enlace o código según la
guía anterior.

Para retirar el puerto SQL de desarrollo, vuelve a ejecutar la composición
principal con el mismo proyecto. Compose recreará el servicio SQL si cambia
su publicación de puertos y reutilizará su volumen:

```powershell
docker compose up -d
```

`docker compose stop` pausa los servicios. `docker compose down` retira
contenedores y conserva los volúmenes con nombre. Mantén el mismo proyecto
Compose, los mismos volúmenes y las credenciales de la instancia al volver a
levantar. Evita `docker compose down -v`, que elimina los volúmenes.

## Documentos

Usa un token de sesión en Swagger o en el encabezado `Authorization: Bearer …`.
En la ejecución local, los archivos se guardan en `App_Data/documentos` bajo
la raíz de contenido de la API. Puedes cambiarlo con `Documentos__Directorio`.
En Compose, la variable apunta al volumen `documentos-data`. La migración
`AgregarDocumentos` crea la tabla de metadatos.

| Operación | Petición | Resultado |
| --- | --- | --- |
| Subir | POST `/api/documentos`, multipart con campo `archivo` | 201 con metadatos. Se aceptan archivos no vacíos de hasta 10 MiB. |
| Listar | GET `/api/documentos` | 200. Estándar ve los propios y Administrador ve todos los vigentes. |
| Descargar | GET `/api/documentos/{id}/descargar` | 200 como adjunto para el dueño o Administrador. |
| Eliminar | DELETE `/api/documentos/{id}` | 204 para el dueño o Administrador. Conserva el registro y el archivo. |

Para comprobar autorización, sube un archivo con una cuenta y prueba la
descarga y eliminación con otra cuenta Estándar. Ambas devuelven 404, igual
que para un identificador inexistente o un documento eliminado. Luego elimina
con la cuenta dueña y comprueba que desaparece del listado y ya no se descarga.
Para probar persistencia, usa otro archivo vigente, ejecuta `docker compose down`
y vuelve a levantar con `docker compose up`. Inicia sesión, lista y descarga ese
mismo archivo.

## API externa

La operación autenticada
`GET /api/reservas/espacios/{id}/planificacion-exterior?inicio=...&fin=...`
combina disponibilidad real de una cancha con el pronóstico horario de
[WeatherAPI.com](https://www.weatherapi.com/docs/). Para usarla, el operador
selecciona una cancha que vaya a utilizarse al aire libre. El modelo todavía
no clasifica las canchas por cubierta. Se usa una ubicación de recinto
configurada mediante `WEATHER_LOCATION`; el ejemplo apunta a Santo Domingo
y debe ajustarse al recinto evaluado.

La clave se lee exclusivamente de `WEATHER_API_KEY` en el entorno del proceso.
Crea una clave en tu cuenta de WeatherAPI.com y guárdala en `WEATHER_API_KEY` dentro del `.env` local. La configuración de ejemplo
deja la clave vacía y permite demostrar el rechazo controlado HTTP 503.
Una clave inválida, error HTTP, respuesta malformada o timeout también producen
un mensaje controlado. El cliente suprime el registro de URLs del proveedor,
porque su autenticación coloca la clave en la query.

Después de cambiar `WEATHER_API_KEY` en `.env`, recrea el contenedor de la API
para que reciba el nuevo valor:

```powershell
docker compose up -d --force-recreate api
```

La planificación primero aplica las reglas de sesión, espacio, fechas y
disponibilidad de Reservas. Si hay solapamiento, recomienda otro horario.
Si está libre, consulta el pronóstico de tres días y exige cobertura de todo
el intervalo. Recomienda usar la cancha cuando todas las horas tienen
probabilidad de lluvia inferior al 60 % y viento inferior a 40 km/h.
Estos umbrales son criterios académicos de demostración.

Ejemplo de respuesta comprobado con proveedor HTTP simulado:

```json
{"espacioId":1,"disponible":true,"recomendado":true,"motivo":"Cancha disponible y condiciones favorables."}
```

La integración se probó exitosamente con una clave real mediante el endpoint de
planificación exterior ejecutado en Docker. Esta comprobación funcional se realizó
contra WeatherAPI. El ejemplo anterior corresponde al proveedor simulado.
Las pruebas automatizadas con proveedor simulado cubren respuesta favorable, lluvia, ausencia de clave,
clave rechazada, timeout y JSON sin datos requeridos.

## Requisitos previos para ejecución local sin Docker

- .NET SDK 10.0 (verificado: 10.0.303), acceso a NuGet para restaurar.
- EF Core SQL Server/Design y CLI dotnet-ef 10.0.12.
- SQL Server compatible con EF Core 10 o SQL Server LocalDB en Windows.
- PowerShell 7 y sqlcmd para los comandos locales de esta guía.
- SMTP local o externo para demostrar recepción de activación y recuperación.
  Mailpit y Docker son opcionales, no requisitos del proyecto.
- xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, EF InMemory 10.0.12:
  se restauran según los csproj, sin instalación separada.

Si falta EF CLI, el evaluador puede instalarla manualmente:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
dotnet ef --version
```

## Clon limpio y base nueva

Clonar el repositorio con los comandos anteriores. Para evaluar Práctica 3 seleccionar
`git checkout practica-3`; esa etiqueta ya está publicada. Ejecutar desde
la raíz. No se necesita ninguna base preexistente ni SISReservas_Practica3_Test.
En una terminal PowerShell dedicada:

```powershell
$evaluacionDb='SISReservas_Evaluacion_' + [guid]::NewGuid().ToString('N')
$env:ConnectionStrings__DefaultConnection="Server=(localdb)\MSSQLLocalDB;Database=$evaluacionDb;Trusted_Connection=True;TrustServerCertificate=True;"
$env:AppBaseUrl='http://localhost:5054'
$env:EmailDispatcher__Enabled='false'
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d master -Q "IF DB_ID(N'$evaluacionDb') IS NOT NULL THROW 50000, 'Destino existente; detener.', 1;" -b
if ($LASTEXITCODE -ne 0) { throw 'No migrar: destino no verificado.' }
Write-Output "Destino exclusivo de evaluacion: $evaluacionDb"
dotnet restore SISReservasEspacios.slnx
dotnet build SISReservasEspacios.slnx
dotnet ef database update --project backend/SISReservas.Api/SISReservas.Api.csproj --connection $env:ConnectionStrings__DefaultConnection
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d $evaluacionDb -Q 'SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;' -b
```

Esperado: cinco migraciones, terminando en 20261109203709_AgregarDocumentos.
La nueva migración crea Espacios y Reservas sin alterar tablas anteriores.
La migración de Gestión de permisos es 20261019220956_AgregarGestionPermisos.
El valor predeterminado de conexión está en backend/SISReservas.Api/appsettings.json
y apunta a SISReservasEspacios; esta guía lo sustituye explícitamente mediante
ConnectionStrings__DefaultConnection para no depender de una base local existente.
Conservar el nombre y la terminal para todos los pasos. Para SQL Server externo,
sustituir cadena/servidor por un catálogo nuevo exclusivo y facilitar credenciales
privadamente. No usar la base principal ni guardar secretos en Git.

## Pruebas

Un comando ejecuta **toda** la suite desde un clon limpio, sin filtros:

```powershell
dotnet test SISReservasEspacios.slnx
```

Restaura y compila automáticamente. No requiere API, SQL ni SMTP. Usa servicios
reales sobre EF InMemory con bases independientes. La última ejecución comprobada
registró 159 aprobadas, 0 fallidas y 0 omitidas. La última compilación comprobada
terminó con 0 errores y 0 advertencias. Estos resultados corresponden a la
validación anterior; no se repitieron durante esta actualización documental.
No sustituye persistencia SQL ni recepción de correo.

## Variables de entorno y correo

| Variable | Uso |
| --- | --- |
| ConnectionStrings__DefaultConnection | Catálogo nuevo exclusivo |
| AppBaseUrl | Origen del enlace de activación: HTTPS o HTTP loopback local |
| EmailDispatcher__Enabled | false: solo cola; true: despacho cada diez segundos |
| SMTP_HOST / SMTP_PORT | Host y puerto SMTP |
| SMTP_FROM | Remitente permitido |
| SMTP_ENABLE_SSL | true para TLS; false solo según capturador local |
| SMTP_USE_AUTH | true por defecto; false para captura local sin autenticación |
| SMTP_USERNAME / SMTP_PASSWORD | Credenciales privadas cuando se autentica |

El servicio hospedado resuelve EmailSenderService con scope por ciclo. Un fallo
no detiene la API; el mensaje puede seguir Pendiente. Una sola instancia debe
despachar la cola. Para un capturador local **ya preparado** en 127.0.0.1:1025:

```powershell
$env:SMTP_HOST='127.0.0.1'
$env:SMTP_PORT='1025'
$env:SMTP_FROM='reservas@test.invalid'
$env:SMTP_ENABLE_SSL='false'
$env:SMTP_USE_AUTH='false'
$env:EmailDispatcher__Enabled='true'
```

Para SMTP externo usar host, puerto, remitente y TLS del proveedor, SMTP_USE_AUTH=true
y credenciales privadas en el proceso/gestor de secretos, nunca en Git. AppBaseUrl
debe ser accesible donde se abra el enlace; en producción configurar origen HTTPS.
Preparación opcional de Mailpit en [guía local](docs/control-acceso-local.md).
No se presupone instalado. Captura SMTP local no equivale a entrega externa.
Leer tokens de CorreosEnCola no demuestra recepción del correo.

## Arranque y Swagger

En la terminal configurada:

```powershell
dotnet run --project backend/SISReservas.Api/SISReservas.Api.csproj --launch-profile http
```

API: http://localhost:5054. Swagger: http://localhost:5054/swagger.
Documento OpenAPI: http://localhost:5054/swagger/v1/swagger.json.
El perfil http usa Development. La raíz `/` puede responder 404: no hay interfaz allí.
Las peticiones siguientes se ejecutan con **Try it out** en Swagger, cuerpos JSON.
Esquema interno Session, no JWT: login devuelve sessionToken. En Authorize pegar
el token; verificar `Authorization: Bearer <sessionToken>`. Alternar tokens al
cambiar cuenta. En otra terminal SQL conservar el mismo valor de $evaluacionDb.

## Control de acceso y revisión acumulada

### Registro y activación por correo

1. Con SMTP habilitado, POST `/api/auth/register` con `nombre`, `email` y `password`.
   Usar correo recibible en el entorno elegido y contraseña propia con al menos
   ocho caracteres, letras y números. Esperado: 200 y cuenta Estándar inactiva.
2. Abrir el enlace **recibido**: GET `/api/auth/activate?email=...&token=...`, 200.
   Repetirlo: 400. Antes de activar no debe obtenerse sessionToken válido.
3. POST `/api/auth/resend-activation` con un string JSON del correo permite
   reenviar a una cuenta todavía inactiva. Verificar el nuevo mensaje recibido.

Preparar dos Estándar y una cuenta destinada a Administrador. Si se leen tokens
de la cola para preparar cuentas sin SMTP, declararlo: no acredita activación por correo.

### Preparación del primer Administrador

Registrar y activar primero la cuenta mediante el correo recibido. El operador
con acceso a Docker puede preparar el primer Administrador con el comando local
siguiente. Sustituir el correo por el de esa cuenta activada:

```powershell
docker compose exec -T api dotnet SISReservas.Api.dll --administrador-inicial admin@evaluacion.test.invalid
```

El comando exige una cuenta existente y activada, y rechaza la operación si ya
hay un Administrador. Revoca las sesiones anteriores de esa cuenta. Si no hay
espacios, prepara una cancha de evaluación. Iniciar sesión de nuevo y confirmar
el rol mediante GET `/api/auth/me`. El script automatizado ejecuta este paso
únicamente en su entorno nuevo después de la activación por SMTP.

### Login, logout y rechazo administrativo

- POST `/api/auth/login` con `email`, `password`: 200 y sessionToken no vacío.
- GET `/api/auth/me` con token: 200, identidad y rol.
- Como Estándar, GET `/api/permission-requests/pending`: 403; sin token, 401.
- POST `/api/auth/logout`: 200. Reutilizar token en `/api/auth/me`: 401.
- Volver a iniciar sesión para continuar. Un 200 sin sessionToken de una cuenta
  inactiva o bloqueada no significa login exitoso.

### Recuperación

1. POST `/api/passwords/recovery` con `email`: 200, respuesta genérica.
2. Leer el código **recibido**. No se construye un enlace de recuperación.
3. POST `/api/passwords/reset` con `token` y `nuevaPassword`: 200.
4. Reutilizar código: 400. Sesión anterior en `/api/auth/me`: 401.
   Login con contraseña anterior: 401; nueva: 200 y sessionToken.

### Administración de usuarios y roles

Usar una cuenta Administrador distinta del usuario que se quiere modificar.
Obtener el GUID del destinatario mediante GET /api/users. Los valores JSON de Rol
son Administrador=1 y Estandar=2; la base guarda los nombres como string.

| Operación | Petición y cuerpo | Resultado actual |
| --- | --- | --- |
| Consultar usuarios | GET /api/users | Administrador: 200 con id, nombre, email, rol y activo; Estándar con ConsultarUsuarios: 200 solo id/nombre; sin permiso: 403 |
| Cambiar rol | PUT /api/users/{id}/role, `{ "rol": 1 }` o `{ "rol": 2 }` | Administrador: 204; GUID inexistente: 404; Estándar: 403 |
| Desactivar | POST /api/users/{id}/deactivate, sin cuerpo | Administrador: 204; inexistente: 404; Estándar: 403 |
| Reactivar | POST /api/users/{id}/activate, sin cuerpo | Administrador: 204; inexistente: 404; Estándar: 403 |

Cambiar el rol o desactivar revoca todas las sesiones del destinatario: el token
anterior debe recibir 401 en /api/auth/me. Tras reactivar, iniciar sesión de nuevo;
no se restauran las sesiones revocadas. Un Administrador que intenta quitarse su
propio rol o desactivarse recibe 400. La activación administrativa no sustituye
la demostración de activación mediante el enlace recibido por correo.

### Bloqueo de inicio de sesión

Con una cuenta de prueba activa, realizar cinco POST /api/auth/login consecutivos
con email existente y contraseña incorrecta. Cada respuesta de credenciales
inválidas es 401; el quinto intento establece un bloqueo de 15 minutos. Durante
ese intervalo, incluso la contraseña correcta obtiene actualmente HTTP 200 con
mensaje de bloqueo y sin sessionToken. Verificar la ausencia del token, no solo
el código HTTP. Esperar el intervalo y entrar con la contraseña correcta: se
emite sesión y se reinician contador y bloqueo. No modificar fechas SQL para
acortar esta comprobación ni utilizar una cuenta necesaria para otros escenarios.

### Casos negativos de Control de acceso

Estos son procedimientos manuales contrastados con el código; no se presentan
como una nueva ejecución de todos los casos en esta restauración documental.

| Caso | Cómo provocarlo | Resultado esperado |
| --- | --- | --- |
| Registro duplicado | Registrar otra vez el mismo correo | 409; email se normaliza con Trim y minúsculas |
| Contraseña de registro inválida | Menos de ocho caracteres o sin letras/números | 400 |
| Datos de registro inválidos | Omitir campos requeridos o usar email sin formato válido | 400 por validación del DTO |
| Cuenta sin activar | Login antes de usar enlace | 200 con mensaje de cuenta inactiva y sin sessionToken |
| Enlace inválido | Alterar el token o usar correo distinto | 400, no activa la cuenta |
| Enlace usado | Reabrir el enlace tras activarlo | 400 |
| Enlace expirado | Usar un token después de sus 24 horas de vigencia | 400; solicitar reenvío para continuar |
| Reenvío | POST /api/auth/resend-activation con string JSON del correo inactivo | 200 genérico; invalida tokens anteriores y encola uno nuevo; no revela si la cuenta existe |
| Login desconocido/incorrecto | Email inexistente o contraseña incorrecta de una cuenta activa, fuera del bloqueo | 401 con el mismo mensaje genérico |
| Recuperación de correo inexistente | POST /api/passwords/recovery con email inexistente | 200 con el mismo texto genérico que una cuenta existente, sin correo nuevo para la inexistente |
| Código inválido/usado/expirado | Reset con código alterado, repetido o después de 30 minutos | 400 |
| Nueva contraseña inválida | Reset con contraseña que incumple longitud/letras/números | 400 |

En recuperación, la búsqueda del usuario normaliza el correo a minúsculas y
recorta espacios, igual que registro e inicio de sesión. Envía una dirección
válida en el cuerpo HTTP.
La prueba de expiración requiere esperar la vigencia; no asumir que esté cubierta
por haber comprobado únicamente la reutilización.

Para cambio autenticado, POST /api/passwords/change con passwordActual y
nuevaPassword: contraseña actual incorrecta, nueva igual a actual o nueva inválida
devuelven 400. Un cambio válido devuelve 200 y revoca sesiones; iniciar sesión con
la nueva. Para restablecimiento administrativo, POST /api/passwords/{id}/force-reset
como Administrador: 200 y código en correo; Estándar recibe 403 e inexistente 404.
Ese endpoint solicita recuperación: no fija una contraseña directamente.

Los mensajes de activación, recuperación y restablecimiento forzado se guardan en
CorreosEnCola. Sin SMTP disponible permanecen Pendientes para reintento; Enviado
indica aceptación SMTP, y observar el mensaje en el capturador acredita recepción
local. No registrar códigos, tokens o contraseñas en evidencia pública.

## Gestión de permisos

Enums JSON: GestionarUsuarios=1, RestablecerContrasenas=2, ConsultarUsuarios=3,
ReservarEspacios=4. Solo 3 y 4 son solicitables. Administrador tiene permisos
implícitos. Estados: Pendiente=1, Aprobada=2, Rechazada=3, Aplicada=4.

### Guía HTTP de RF-GP-01 a RF-GP-07

Usar Swagger con tres cuentas activas: Administrador, Estándar A y Estándar B.
Seleccionar el token correspondiente en Authorize para cada paso. No publicar
tokens ni contraseñas. El escenario usa permiso 4 para conectar con Reservas;
para comprobar consulta de usuarios usar una solicitud separada de permiso 3.

#### RF-GP-01: solicitud nace Pendiente

Como A, POST /api/permission-requests con `{ "permiso": 4 }`: 201, estado 1,
permiso 4, resueltaPorId y motivoRechazo null. Guardar id GUID como SOLICITUD_A.
GET /api/permission-requests/mine debe devolverla Pendiente. Antes de aprobar,
POST /api/reservas con el cuerpo válido de la sección de Reservas: 403 y
GET /api/reservas/mine sin reserva creada por ese intento. La solicitud pendiente
no concede autorización.

#### RF-GP-02: pendientes y visibilidad

Como B crear otra solicitud y guardar SOLICITUD_B. Administrador ejecuta
GET /api/permission-requests/pending: 200 e incluye ambas. A y B consultan mine:
cada uno ve solo las suyas. Un Estándar que invoca pending recibe 403; sin sesión,
las rutas protegidas devuelven 401.

#### RF-GP-03: aprobación con resolutor y fecha

Como Administrador, POST /api/permission-requests/{SOLICITUD_A}/approve, sin cuerpo:
200, estado 4, resueltaPorId igual al Administrador, resueltaEn y aplicadaEn
informados. Se recorre Pendiente → Aprobada → Aplicada; Aprobada es intermedio en
memoria y el estado final persistido es Aplicada. Consultar mine como A para
comprobar los mismos datos y crear su reserva: ahora 201/Pendiente.

#### RF-GP-04: rechazo y motivo

Como Administrador, POST /api/permission-requests/{SOLICITUD_B}/reject con
`{ "motivoRechazo": "" }`: 400; B consulta mine y conserva estado 1 sin resolutor.
Con `{ "motivoRechazo": "Prueba de rechazo" }`: 200, estado 3, motivo, resolutor y
resueltaEn. Motivo obligatorio, no solo espacios y máximo 500 caracteres.

#### RF-GP-05: concesión efectiva

Para permiso 4 usar la reserva rechazada antes y permitida después de RF-GP-03.
Para el caso histórico ConsultarUsuarios, A crea otra solicitud con permiso 3.
Antes de aprobarla, GET /api/users como A: 403 si no tenía ese permiso; después
de aprobar como Administrador: 200 solo con id/nombre. Administrador ve la lista
completa. No insertar concesiones manualmente: la aprobación crea PermisosUsuario
y evita duplicados mediante índice único por UsuarioId/Permiso.

#### RF-GP-06: doble aprobación y transiciones prohibidas

Administrador repite approve de SOLICITUD_A: 409; intenta reject con motivo válido:
409. Mine de A mantiene estado 4 y las mismas fechas/resolutor. Intentar approve
de SOLICITUD_B rechazada: 409; mine de B conserva estado 3 y motivo. Una resolución
inválida no debe cambiar ni duplicar la concesión efectiva.

#### RF-GP-07: no resolver solicitud propia

Usar una solicitud NUEVA de A todavía Estándar; no reutilizar una ya resuelta.
El Administrador original obtiene ID_A con GET /api/users y promueve a A mediante
PUT /api/users/{ID_A}/role con `{ "rol": 1 }`: 204, revoca las sesiones de A.
A inicia sesión nuevamente y comprueba rol Administrador en /api/auth/me.
Con su token nuevo intenta approve de SU solicitud: 403, mensaje de resolución
propia. Repetir reject con motivo válido: 403. El Administrador original consulta
pending y comprueba que la solicitud sigue Pendiente sin resolución.

Restaurar el rol usando el Administrador original con PUT /api/users/{ID_A}/role
y `{ "rol": 2 }`: 204. A vuelve a iniciar sesión como Estándar. No usar la cuenta
promovida para quitarse a sí misma el rol: se rechaza con 400. El endpoint de
creación de solicitudes solo admite Estándar; por eso se crea antes de promover.
Pruebas automatizadas relacionadas: AprobarAsync_ResolucionPropia_NoModificaSolicitud
y RechazarAsync_ResolucionPropia_NoModificaSolicitud.

### Errores HTTP de Gestión de permisos

| HTTP | Situación |
| --- | --- |
| 400 | Entrada inválida, permiso inexistente/no solicitable o motivo inválido |
| 401 | Sesión ausente o inválida |
| 403 | Rol no permitido o resolución propia |
| 404 | Usuario o solicitud no encontrado |
| 409 | Estado inválido, concesión duplicada, conflicto concurrente o usuario inactivo según operación |
| 500 | Error de persistencia devuelto por el módulo |

## Espacios de evaluación

No hay endpoint de administración de espacios. Insertar solo en el catálogo nuevo,
sin operaciones concurrentes. Consultar Id reales, no asumir que empiezan en 1:

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d $evaluacionDb -Q "INSERT INTO Espacios (Nombre,Tipo,Activo) VALUES (N'Salon Evaluacion','Salon',1),(N'Cancha Evaluacion','Cancha',1),(N'Equipo Evaluacion','Equipo',1),(N'Equipo Inactivo','Equipo',0); SELECT Id,Nombre,Tipo,Activo FROM Espacios;" -b
```

SQL almacena Tipo string; JSON: Salon=1, Cancha=2, Equipo=3.

## Funcionalidades de Reservas

Todos los endpoints exigen sesión y rol Estandar/Administrador. Solo crear exige
ReservarEspacios al Estándar; Administrador crea sin concesión explícita.
Usar fechas UTC con Z, inicio anterior al fin y no pasado. Los ejemplos de 2035
se sustituyen por fechas futuras al evaluar.

| Funcionalidad separable | Petición | Esperado |
| --- | --- | --- |
| Listar activos | GET `/api/reservas/espacios` | 200, excluye inactivos |
| Disponibilidad | GET `/api/reservas/espacios/{ESPACIO}/disponibilidad?inicio=2035-01-02T12:00:00Z&fin=2035-01-02T13:00:00Z` | disponible true antes y false tras reservar |
| Crear | POST `/api/reservas`, cuerpo inferior | 201/Pendiente; sin permiso aprobado 403 |
| Consultar propias | GET `/api/reservas/mine` | 200; otro Estándar no ve la reserva |
| Confirmar | POST `/api/reservas/{RESERVA}/confirmar`, Administrador | 200/Confirmada; Estándar 403 |
| Cancelar | POST `/api/reservas/{RESERVA}/cancelar`, propietario o Administrador | 200/Cancelada desde Pendiente o Confirmada; ajeno 404 |
| Completar internamente | ReservaService.CompletarAsync, sin endpoint | Solo Confirmada con intervalo finalizado; prueba de servicio |

```json
{
  "espacioId": 1,
  "fechaInicio": "2035-01-02T12:00:00Z",
  "fechaFin": "2035-01-02T13:00:00Z"
}
```

Sustituir espacioId por el consultado y guardar respuesta id como RESERVA. Usar
intervalos distintos o reservas nuevas para probar independientemente cada caso.
No reutilizar una reserva terminal para probar una transición permitida.

- Repetir intervalo: 409. Crear 13:00–14:00: 201 (adyacente).
- Fin igual/anterior al inicio: 400; espacio inexistente/inactivo: 404.
- Confirmar exige espacio activo, inicio < fin, inicio no pasado y ausencia de
  conflicto con otras Pendientes/Confirmadas, excluyendo la propia.
- Para provocar condición incumplida: crear una Pendiente, desactivar solo su
  espacio de prueba con SQL y confirmar como Administrador: 409, mine mantiene
  Pendiente. Reactivar después. Comandos SQL exactos (reemplazar 1 por Id real):

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d $evaluacionDb -Q 'UPDATE Espacios SET Activo=0 WHERE Id=1;' -b
# Ejecutar el intento HTTP y consultar mine antes de reactivar.
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d $evaluacionDb -Q 'UPDATE Espacios SET Activo=1 WHERE Id=1;' -b
```

Cancelar una Pendiente nueva prueba la primera cancelación; confirmar otra reserva
y cancelarla prueba la segunda. Estados JSON: Pendiente=1, Confirmada=2,
Cancelada=3, Completada=4. Solo bloquean Pendientes/Confirmadas del mismo espacio:
inicioNuevo < finExistente && finNuevo > inicioExistente.

### Prohibida y estados terminales

Cancelar una reserva (estado 3). Confirmarla como Administrador: 409; cancelarla
otra vez: 409. Consultar `/api/reservas/mine` como propietario: mismo Id, fechas y
estado 3. Demuestra Cancelada → Confirmada prohibida y que Cancelada es terminal.

Los cuatro pares prohibidos y todas las salidas de Cancelada/Completada se prueban
en TransicionesReservaTests. Completada se prueba en Completar_SoloPermiteIntervaloFinalizado
y Completar_EstadoNoPermitido_NoModifica. El método es interno; **no hay tarea que
lo invoque automáticamente**, endpoint ni comando de producción para hacerlo.
Ver [máquina de estados y cobertura por fila](docs/maquina-de-estados.md).

## Persistencia RD-09

1. Crear una reserva Pendiente por HTTP y guardar respuesta completa.
2. Detener API con Ctrl+C, sin eliminar/recrear base ni reinsertar datos.
3. En la misma terminal y con la misma cadena, repetir el comando de arranque.
4. Iniciar sesión otra vez y GET `/api/reservas/mine`: mismos Id, EspacioId, fechas
   y Estado. No volver a crear la reserva.

Comprobado mediante reinicio de proceso en [fase 6](docs/verificacion-fase-6.md)
y [evidencia de 73 verificaciones](docs/evidencia-fase-6.json). La base histórica
no forma parte de la entrega y el profesor no depende de ella.

Opcional: scripts/Validar-Fase6.ps1 automatiza sin SMTP contra un catálogo nuevo,
vacío y recién migrado con nombre fijo SISReservas_Practica3_Test. No es el comando
de suite ni un requisito. Rechaza bases con usuarios, no borra ni aplica migraciones.
Su preparación de roles y lectura de tokens de cola no demuestran correo real.

## Defectos

Los cuatro defectos de Práctica 3 están **creados, detectados y publicados**.
Las cinco etiquetas anotadas están disponibles en GitHub. Una auditoría en clon local
limpio confirmó 138/138 pruebas en practica-3 y fallos pertinentes en cada mutante.

| Etiqueta publicada | Defecto | Prueba que lo detectó | Comportamiento verificado |
| --- | --- | --- | --- |
| practica-3-defecto-1 | Permitir Cancelada → Confirmada | TransicionesReservaTests.AsegurarPermitida_ProhibidaDocumentada_RechazaSinCambiarReserva(Cancelada, Confirmada) | Rechazo y estado intacto |
| practica-3-defecto-2 | Permitir Completada → Cancelada, salida de un terminal | TransicionesReservaTests.AsegurarPermitida_DesdeTerminal_RechazaCualquierDestino(Completada, Cancelada) | La salida del estado terminal debe rechazarse |
| practica-3-defecto-3 | Eliminar únicamente && e.Activo de CumpleCondicionesConfirmacionAsync durante la confirmación | ReservaServiceTests.Confirmar_CondicionIncumplida_RechazaSinCambiarEstado("inactivo") | Espacio inactivo no se confirma y conserva Pendiente; las demás condiciones no se eliminaron |
| practica-3-defecto-4 | Retirar únicamente consulta de permiso en CrearAsync | ReservasAutorizacionTests.Crear_SolicitudPendienteRechazaYTrasAprobacionRealPermite | Antes de aprobar rechaza sin crear; tras aprobación real permite al mismo Estándar |

Cada commit defecto es hijo directo del mismo commit practica-3, modifica solo
producción, nunca pruebas, y su mensaje indica archivo y línea exacta cambiada.
Sobre practica-3 pasa toda la suite; los defectos 1 y 2 producen 134 aprobadas y
4 fallidas cada uno, el defecto 3 produce 137 aprobadas y 1 fallida, y el defecto 4
produce 136 aprobadas y 2 fallidas, incluida la integración real indicada arriba.
Los cuatro defectos son independientes, no forman una cadena. La base publicada
permanece en be2080d49a0f855112fe9849231e1927f5422966 hasta aprobar la actualización.
El nuevo candidato documental es 5811357e61837924776048db848bd6a95641e00f;
solo modifica README.md y los cuatro mutantes reconstruidos son hijos directos suyos.
Las etiquetas históricas practica-1/practica-2 y defectos P2 no prueban mutantes P3.

## Cierre de evidencia posterior a practica-3

La base evaluable es `practica-3`, commit
`be2080d49a0f855112fe9849231e1927f5422966`. Su README original refleja un estado
documental anterior. Se preparó una corrección exclusivamente documental en
`codex/correccion-base-practica-3`, commit
`5811357e61837924776048db848bd6a95641e00f`, y cuatro ramas
`codex/correccion-defecto-1` a `codex/correccion-defecto-4` con las mutaciones originales.
La base corregida pasó 138/138; los mutantes produjeron respectivamente 4, 4, 1 y 2
fallos pertinentes, incluida la integración del defecto 4. No se han movido las
etiquetas originales ni actualizado las referencias remotas: se requiere aprobación
expresa antes de publicarlas. Los respaldos locales backup/pre-correccion-practica-3
y backup/pre-correccion-defecto-1 a 4 conservan los commits originales.
Las guías de fases anteriores se conservan como registros históricos; las
afirmaciones de correo pendiente describen su estado en aquellas fases.

| Versión o evidencia | Resultado verificado |
| --- | --- |
| practica-3 | 138 aprobadas, 0 fallidas desde clon local limpio, sin iniciar API |
| practica-3-defecto-1 | 134 aprobadas, 4 fallidas: permite Cancelada → Confirmada |
| practica-3-defecto-2 | 134 aprobadas, 4 fallidas: permite Completada → Cancelada |
| practica-3-defecto-3 | 137 aprobadas, 1 fallida: elimina únicamente e.Activo durante confirmación; conserva las demás condiciones |
| practica-3-defecto-4 | 136 aprobadas, 2 fallidas, incluida la integración real: permite crear sin ReservarEspacios |
| SMTP local, 1 de noviembre de 2026 | 30 verificaciones aprobadas; seis mensajes recibidos efectivamente en Mailpit: cuatro antiguos de prueba y dos nuevos |

Los dos mensajes nuevos fueron activación y recuperación de una cuenta ficticia.
Se utilizó el enlace recibido por SMTP para activar (200) y se rechazó su segundo
uso (400). Login devolvió sesión válida. El código recibido en el segundo mensaje
permitió restablecer la contraseña (200), no reutilizarlo (400), rechazar la
contraseña anterior (401), aceptar la nueva (200) y revocar la sesión previa (401).
No se extrajeron esos tokens de la cola para completar esta demostración.

La prueba utilizó exclusivamente `SISReservas_Practica3_Test`, con Mailpit ligado
a `127.0.0.1`, sin reenvío externo ni credenciales personales. Los cuatro mensajes
antiguos no se borraron. API y Mailpit temporales quedaron detenidos; las evidencias
originales se conservaron fuera de Git. Este resumen no contiene secretos ni enlaces
a archivos temporales que el evaluador no podría consultar. Es recepción SMTP
local comprobada, no una afirmación de entrega a un proveedor de correo externo.

Las cinco etiquetas anotadas ya están publicadas y se probaron también desde un
clon nuevo de GitHub: la base pasó 138/138 y los cuatro mutantes produjeron los
fallos pertinentes de la tabla. Desde un clon nuevo se puede comprobar:

```powershell
git tag --list 'practica-3*'
$etiquetas=@('practica-3','practica-3-defecto-1','practica-3-defecto-2','practica-3-defecto-3','practica-3-defecto-4')
foreach ($etiqueta in $etiquetas) { git cat-file -t $etiqueta }
git checkout --detach practica-3
git rev-parse HEAD
dotnet test SISReservasEspacios.slnx
```

Esperado: cinco etiquetas de tipo `tag`, HEAD en la base indicada y 138/138 pruebas.
Mientras esté pendiente la publicación de la corrección, la base indicada sigue
siendo be2080d; una vez aprobada y publicada, será el candidato 5811357 indicado arriba.
Para cada mutante, en un clon limpio cambiar a su etiqueta y ejecutar el mismo
comando de pruebas: debe producir los fallos pertinentes de la tabla. Comprobar
el padre directo con `git log -1 --format="%H %P" practica-3-defecto-1` (repetir
para 2, 3 y 4): siempre debe ser el commit base. Todos modifican solo producción;
sus mensajes identifican archivo y línea. Los cuatro son independientes.

En el simulacro posterior la base pasó 138/138 en nueve ejecuciones, pero durante
dos ejecuciones de mutantes apareció un fallo adicional intermitente en
ConfiguracionCorreoTests.Dispatcher_Deshabilitado_NoResuelveServiciosNiTocaBaseDeDatos.
No se ha corregido ni se afirma resuelto. En la validación de los candidatos
documentales no apareció ese fallo; esto no garantiza que haya desaparecido.

## Práctica 2: defectos históricos de Gestión de permisos

Esta tabla recupera la documentación de la entrega anterior. No se mezcla con
los cuatro defectos de Reservas de Práctica 3. Para reproducir un defecto histórico,
usar un clon limpio, seleccionar su etiqueta y ejecutar el comando de suite de
su README. No aplicar sus cambios a main ni usar sus servicios defectuosos en la API.

| Etiqueta histórica | Prueba detectora documentada | Comportamiento incorrecto introducido |
| --- | --- | --- |
| practica-2-defecto-1 | RechazarAsync_MotivoVacio_NoModificaSolicitud | Permite rechazar con motivo vacío o solo espacios |
| practica-2-defecto-2 | EsPermitida_RespetaLaMaquinaDeEstados | Permite Aprobada → Rechazada |
| practica-2-defecto-3 | AprobarAsync_Estandar_DevuelveRolNoPermitido | Omite verificación de rol al aprobar |
| practica-2-defecto-4 | AprobarAsync_ResolucionPropia_NoModificaSolicitud | Invierte validación de resolución propia |
| practica-2-defecto-5 | EsPermitida_RespetaLaMaquinaDeEstados | Permite Aplicada → Rechazada, saliendo de un terminal |
| practica-2-defecto-6 | AprobarAsync_Pendiente_RegistraResolutorYFecha | Omite resolutor y fecha de aprobación |

La versión correcta de esa entrega está etiquetada practica-2. La tabla y las
pruebas se documentaron en 2f2a056; esta restauración no reejecutó los seis mutantes
históricos ni modifica sus etiquetas. Los resultados de Práctica 3 se encuentran
en las secciones Defectos y Cierre de evidencia, independientemente de esta tabla.

## Evidencia histórica RD-09 de Asignación 2

Registro del README de 2f2a056, correspondiente al 20 de octubre de 2026:

1. Se preparó el catálogo aislado SISReservasEspacios_RD09_20261020_Audit.
2. Se aplicaron migraciones mediante script idempotente de EF y sqlcmd, sin
   ejecutar database update sobre SISReservasEspacios.
3. Se prepararon tres cuentas ficticias solo allí; se creó una solicitud aprobada
   y otra rechazada y se comprobó el permiso ConsultarUsuarios concedido.
4. Tras detener y reiniciar API contra el mismo catálogo, la aprobada seguía
   Aplicada con su permiso y la rechazada conservaba estado y motivo. Mine devolvió
   ambas solicitudes; /api/users respondió 200 para el usuario autorizado y pending
   devolvió cero solicitudes.
5. El catálogo desechable se eliminó al terminar; la base principal no se modificó.

Es evidencia histórica de permisos, no una base que deba existir al clonar ni una
prueba repetida ahora. La demostración posterior de Reservas, también aislada y con
reinicio, permanece en la sección Persistencia RD-09 y docs/verificacion-fase-6.md.
Ninguna de las dos demuestra concurrencia simultánea sobre SQL Server.

## Estructura actual del proyecto

```text
SISReservasEspacios/
├── SISReservasEspacios.slnx
├── README.md
├── backend/
│   └── SISReservas.Api/
│       ├── SISReservas.Api.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Controllers/      # Acceso, usuarios, contraseñas, permisos y reservas
│       ├── DTOs/             # Contratos HTTP de cada módulo
│       ├── Data/             # SISReservasDbContext
│       ├── Migrations/       # Esquema y snapshot EF Core
│       ├── Models/           # Entidades y enums/máquinas de estados
│       ├── Properties/       # Perfiles de arranque y acceso interno para pruebas
│       ├── Security/         # Autenticación Session, tokens, hash y cola de correo
│       └── Services/         # Lógica de acceso, permisos, reservas y despacho SMTP
├── tests/
│   └── SISReservas.Api.Tests/
├── scripts/
│   └── Validar-Fase6.ps1
└── docs/
    ├── bitacora-asignacion-1.md
    ├── bitacora-asignacion-2.md
    ├── maquina-de-estados.md
    ├── control-acceso-local.md
    ├── verificacion-fase-4.md
    ├── verificacion-fase-6.md
    ├── evidencia-fase-6.json
    └── auditoria-practica-3.md
```

## Límites y entrega

- Activación y recuperación comprobadas desde mensajes recibidos por SMTP local;
  entrega a buzones externos no comprobada. El escenario anterior de fase 6 usaba
  tokens de cola y no sustituye la evidencia SMTP posterior descrita arriba.
- Finalización automática y concurrencia simultánea SQL pendientes; seis
  funcionalidades públicas separables sí están implementadas y verificadas.
- Los archivos necesarios de producción, migración, pruebas y documentación ya
  están versionados en la base. No versionar bin/obj, tokens, contraseñas, entorno
  ni secretos SMTP. main y las cinco etiquetas de Práctica 3 ya fueron publicados;
  cualquier ajuste documental posterior requiere su propia revisión antes de publicarse.
- Notificaciones/reportes de Reservas: semana 10. Adjuntos: opcionales; no implementados.

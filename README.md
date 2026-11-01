# SISReservasEspacios

Sistema de reservas y disponibilidad de salones, canchas y equipos, con Control
de acceso, Gestión de permisos y módulo de Reservas. Proyecto académico de
Programación III · TDS-007 · ITLA · 2026-C-3. Esta guía reúne la configuración,
el uso de los módulos y las entregas acumulativas; los resultados históricos se
distinguen de las instrucciones para ejecutar la versión actual.

Estado: 138 pruebas aprobadas desde un clon local limpio y 73 verificaciones HTTP/SQL
históricas. Las cinco etiquetas anotadas practica-3 y practica-3-defecto-1 a 4
están publicadas en GitHub; los cuatro defectos fueron creados y detectados por la suite.
La publicación se contrastó mediante git ls-remote origin: main remoto apunta a
61f1311bb5e91ae79d79a0b7639ee37279de4375 y practica-3 a
be2080d49a0f855112fe9849231e1927f5422966. La recepción SMTP local y los
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

## Requisitos previos

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

Esperado: cuatro migraciones, terminando en 20261101193103_AgregarEspaciosYReservas.
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
reales sobre EF InMemory con bases independientes. Resultado actual: 138 aprobadas,
0 fallidas, 0 omitidas. No sustituye persistencia SQL ni recepción de correo.

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

No hay endpoint público de promoción inicial. Registrar y activar la cuenta y,
solo en el catálogo nuevo, promoverla antes del login. Sustituir el correo por el
real de esa cuenta de evaluación:

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d $evaluacionDb -Q "UPDATE Usuarios SET Rol='Administrador' WHERE Email='admin@evaluacion.test.invalid'; SELECT Email,Rol,Activo FROM Usuarios;" -b
```

Ese correo es un identificador de ejemplo, no un buzón externo. No usarlo sin
sustituir si se evalúa correo externo. Confirmar rol mediante GET `/api/auth/me`.

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

En recuperación usar exactamente el email guardado, en minúsculas, sin espacios:
RequestRecoveryAsync no aplica la normalización que sí usa registro/login.
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
| practica-3-defecto-3 | Eliminar únicamente e.Activo de CumpleCondicionesConfirmacionAsync durante la confirmación | ReservaServiceTests.Confirmar_CondicionIncumplida_RechazaSinCambiarEstado("inactivo") | Espacio inactivo no se confirma y conserva Pendiente; las demás condiciones no se eliminaron |
| practica-3-defecto-4 | Retirar únicamente consulta de permiso en CrearAsync | ReservasAutorizacionTests.Crear_SolicitudPendienteRechazaYTrasAprobacionRealPermite | Antes de aprobar rechaza sin crear; tras aprobación real permite al mismo Estándar |

Cada commit defecto es hijo directo del mismo commit practica-3, modifica solo
producción, nunca pruebas, y su mensaje indica archivo y línea exacta cambiada.
Sobre practica-3 pasa toda la suite; los defectos 1 y 2 producen 134 aprobadas y
4 fallidas cada uno, el defecto 3 produce 137 aprobadas y 1 fallida, y el defecto 4
produce 136 aprobadas y 2 fallidas, incluida la integración real indicada arriba.
Los cuatro defectos son independientes, no forman una cadena. La etiqueta base
permanece en be2080d49a0f855112fe9849231e1927f5422966; este README actualizado se
mantiene en un commit documental posterior de main y no cambia el contenido etiquetado.
Las etiquetas históricas practica-1/practica-2 y defectos P2 no prueban mutantes P3.

## Cierre de evidencia posterior a practica-3

La base evaluable es `practica-3`, commit
`be2080d49a0f855112fe9849231e1927f5422966`. Su README refleja el estado documental
anterior a crear y verificar las etiquetas y a demostrar SMTP. Este cierre está
en un commit posterior de `main`: no mueve etiquetas ni cambia el código evaluable.
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
Para cada mutante, en un clon limpio cambiar a su etiqueta y ejecutar el mismo
comando de pruebas: debe producir los fallos pertinentes de la tabla. Comprobar
el padre directo con `git log -1 --format="%H %P" practica-3-defecto-1` (repetir
para 2, 3 y 4): siempre debe ser el commit base. Todos modifican solo producción;
sus mensajes identifican archivo y línea. Los cuatro son independientes.

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
- Avisos NU1510 y NU1902/NU1903 (System.Security.Cryptography.Pkcs 9.0.11 en pruebas).
- Los archivos necesarios de producción, migración, pruebas y documentación ya
  están versionados en la base. No versionar bin/obj, tokens, contraseñas, entorno
  ni secretos SMTP. main y las cinco etiquetas de Práctica 3 ya fueron publicados;
  cualquier ajuste documental posterior requiere su propia revisión antes de publicarse.
- Notificaciones/reportes de Reservas: semana 10. Adjuntos: opcionales; no implementados.

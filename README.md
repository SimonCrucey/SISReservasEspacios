# SISReservasEspacios

Sistema web para gestionar reservas de espacios, usuarios y control de acceso.

## Tecnologías

* C#
* ASP.NET Core Web API
* .NET 10
* Entity Framework Core
* SQL Server / LocalDB

## Requisitos previos

* .NET SDK 10.0
* SQL Server LocalDB o una instancia compatible de SQL Server
* Herramienta `dotnet ef` para aplicar migraciones
* Visual Studio 2022/2026 o VS Code

## Ejecución del proyecto

Desde la raíz del repositorio:

```bash
dotnet restore
dotnet build
dotnet run --project backend/SISReservas.Api/SISReservas.Api.csproj --launch-profile http
```

La API se ejecuta en `http://localhost:5054` con el perfil `http` y el entorno `Development`.

## Base de datos

La configuración por defecto usa LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=SISReservasEspacios;Trusted_Connection=True;TrustServerCertificate=True;
```

La cadena se encuentra en `backend/SISReservas.Api/appsettings.json`. No se deben versionar credenciales reales.

Para aplicar las migraciones:

```bash
dotnet ef database update --project backend/SISReservas.Api/SISReservas.Api.csproj
```

La migración de la Asignación 2 es `20261019220956_AgregarGestionPermisos`.

## Swagger

Con el perfil `http` y el entorno `Development`, Swagger está disponible en:

* `http://localhost:5054/swagger`
* `http://localhost:5054/swagger/v1/swagger.json`

La API usa autenticación por token de sesión con el esquema `Bearer` (no JWT). Para obtener un token:

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "usuario@ejemplo.com",
  "password": "contraseña"
}
```

La respuesta incluye `sessionToken`. Las rutas protegidas esperan:

```http
Authorization: Bearer <sessionToken>
```

No se incluyen credenciales de prueba en el repositorio.

## Variables de entorno

Para el envío independiente de correos mediante SMTP se utilizan las siguientes variables:

| Variable          | Propósito                          |
| ----------------- | ---------------------------------- |
| `SMTP_HOST`       | Servidor SMTP                      |
| `SMTP_PORT`       | Puerto del servidor SMTP           |
| `SMTP_USERNAME`   | Usuario de autenticación SMTP      |
| `SMTP_PASSWORD`   | Contraseña de autenticación SMTP   |
| `SMTP_FROM`       | Dirección utilizada como remitente |
| `SMTP_ENABLE_SSL` | Indica si se utiliza SSL/TLS       |

Los valores reales de estas variables **no deben almacenarse en el repositorio**.

## Control de acceso

La Práctica 1 implementa:

* Registro de usuarios y validación de correo.
* Activación mediante token de un solo uso.
* Inicio y cierre de sesión.
* Control de sesiones.
* Bloqueo después de cinco intentos fallidos consecutivos.
* Roles Administrador y Estándar.
* Administración de usuarios por parte del Administrador.
* Recuperación y cambio de contraseña.
* Invalidación de sesiones después de cambios de contraseña.
* Registro de correos en `CorreoEnCola`.
* Máquina de estados para las reservas.

## Gestión de permisos (Asignación 2)

La Asignación 2 implementa el flujo de solicitudes de permisos para usuarios Estándar y su resolución por Administradores.

### Roles y permisos

* **Administrador:** tiene acceso implícito a todos los permisos.
* **Estándar:** solo recibe los permisos concedidos explícitamente.
* Permisos disponibles: `GestionarUsuarios`, `RestablecerContrasenas` y `ConsultarUsuarios`.
* Solo `ConsultarUsuarios` está habilitado para solicitudes de usuarios Estándar.
* Los enums se serializan como enteros: `ConsultarUsuarios = 3`.
* Estados de solicitud: `Pendiente = 1`, `Aprobada = 2`, `Rechazada = 3`, `Aplicada = 4`.

### Rutas

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| `POST` | `/api/permission-requests` | Estándar | Crea una solicitud. Cuerpo: `{ "permiso": 3 }`. Responde `201` si se crea. |
| `GET` | `/api/permission-requests/mine` | Autenticado | Devuelve las solicitudes del usuario autenticado. |
| `GET` | `/api/permission-requests/pending` | Administrador | Devuelve las solicitudes pendientes. |
| `POST` | `/api/permission-requests/{id}/approve` | Administrador | Aprueba una solicitud pendiente. |
| `POST` | `/api/permission-requests/{id}/reject` | Administrador | Rechaza una solicitud. Cuerpo: `{ "motivoRechazo": "..." }`. |
| `GET` | `/api/users` | Autenticado | El Administrador obtiene la lista completa; un Estándar con `ConsultarUsuarios` obtiene el resumen. Sin el permiso responde `403`. |

Respuestas de error esperadas:

* `400`: datos inválidos, permiso inexistente o permiso no solicitable.
* `401`: falta autenticación.
* `403`: rol no permitido o intento de resolver la propia solicitud.
* `404`: usuario o solicitud inexistente.
* `409`: estado inválido, concesión duplicada, conflicto de concurrencia o usuario inactivo.
* `500`: error de persistencia no controlado.

### Flujo de aprobación y rechazo

1. Un usuario Estándar crea una solicitud; queda en estado `Pendiente`.
2. Un Administrador consulta las solicitudes pendientes.
3. Al aprobar, la solicitud recorre `Pendiente → Aprobada → Aplicada`. `Aprobada` es un estado intermedio en memoria; el estado persistido final es `Aplicada`, con `ResueltaEn` y `AplicadaEn`.
4. La aprobación crea la concesión efectiva en `PermisosUsuario`. El índice único por usuario y permiso evita duplicados.
5. Al rechazar, la solicitud pasa de `Pendiente` a `Rechazada` y exige un motivo no vacío de hasta 500 caracteres.
6. Un usuario no puede aprobar ni rechazar su propia solicitud.
7. Una solicitud ya resuelta no puede aprobarse ni rechazarse nuevamente.

### Prueba manual de RF-GP-01 a RF-GP-07

#### Preparación local

Para un clon limpio se necesitan tres cuentas activas: un Administrador y dos usuarios Estándar. Regístralas con `POST /api/auth/register` usando `nombre`, `email` y `password`, y actívalas. Si estás trabajando en LocalDB y no vas a usar el enlace de activación, puedes preparar el entorno local así:

```sql
UPDATE Usuarios
SET Activo = 1
WHERE Email IN ('admin@ejemplo.com', 'estandar.a@ejemplo.com', 'estandar.b@ejemplo.com');

UPDATE Usuarios
SET Rol = 'Administrador'
WHERE Email = 'admin@ejemplo.com';
```

Inicia sesión con cada cuenta y guarda el `sessionToken` devuelto:

```bash
BASE=http://localhost:5054
TOKEN_ADMIN="<sessionToken de admin>"
TOKEN_A="<sessionToken de estandar.a>"
TOKEN_B="<sessionToken de estandar.b>"
```

Comprueba el rol con `GET /api/auth/me`. Los enums se envían como enteros: `{"rol":1}` es Administrador y `{"rol":2}` es Estándar. Los estados son `Pendiente=1`, `Aprobada=2`, `Rechazada=3` y `Aplicada=4`.

#### RF-GP-01: la solicitud nace en `Pendiente`

**Rol:** Estándar A.

```bash
curl -i -X POST "$BASE/api/permission-requests" \
  -H "Authorization: Bearer $TOKEN_A" \
  -H "Content-Type: application/json" \
  -d '{"permiso":3}'
```

**Esperado:** `201 Created`, con `estado: 1`, `permiso: 3`, `resueltaPorId: null` y `motivoRechazo: null`. Guarda el `id` como `SOLICITUD_A`. `GET /api/permission-requests/mine` con `TOKEN_A` debe devolver la misma solicitud en estado `Pendiente`.

#### RF-GP-02: visibilidad de solicitudes pendientes

**Roles:** Administrador, Estándar A y Estándar B.

1. Con `TOKEN_B`, crea otra solicitud con el mismo `POST` y `{"permiso":3}`. Guarda su `id` como `SOLICITUD_B`.
2. Con `TOKEN_ADMIN`, ejecuta `GET /api/permission-requests/pending`: debe responder `200` e incluir `SOLICITUD_A` y `SOLICITUD_B`.
3. Con `TOKEN_A`, ejecuta `GET /api/permission-requests/mine`: debe incluir solo `SOLICITUD_A`, no `SOLICITUD_B`.
4. Con `TOKEN_B`, `GET /api/permission-requests/mine` debe incluir solo `SOLICITUD_B`.
5. Con `TOKEN_A`, ejecuta `GET /api/permission-requests/pending`: debe responder `403` porque el rol Estándar no puede consultar la bandeja administrativa.

#### RF-GP-03: aprobación con resolutor y fecha

**Rol:** Administrador.

```bash
curl -i -X POST "$BASE/api/permission-requests/$SOLICITUD_A/approve" \
  -H "Authorization: Bearer $TOKEN_ADMIN"
```

**Esperado:** `200 OK`, con `estado: 4`, `resueltaPorId` igual al `id` del Administrador, `resueltaPorNombre` con su nombre, y `resueltaEn` y `aplicadaEn` con fecha. `Aprobada=2` es un estado intermedio en memoria; el estado persistido final es `Aplicada`. `GET /api/permission-requests/mine` con `TOKEN_A` debe mostrar los mismos datos.

#### RF-GP-04: rechazo con motivo obligatorio

**Rol:** Administrador.

1. Intenta rechazar `SOLICITUD_B` sin motivo:

```bash
curl -i -X POST "$BASE/api/permission-requests/$SOLICITUD_B/reject" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -H "Content-Type: application/json" \
  -d '{"motivoRechazo":""}'
```

**Esperado:** `400 Bad Request`. Después, `GET /api/permission-requests/mine` con `TOKEN_B` debe seguir mostrando `estado: 1`, `resueltaPorId: null` y `motivoRechazo: null`.

2. Rechaza la misma solicitud con un motivo válido:

```bash
curl -i -X POST "$BASE/api/permission-requests/$SOLICITUD_B/reject" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -H "Content-Type: application/json" \
  -d '{"motivoRechazo":"No cumple los requisitos"}'
```

**Esperado:** `200 OK`, con `estado: 3`, `motivoRechazo` igual al texto enviado, `resueltaPorId` y `resueltaEn` informados.

#### RF-GP-05: el permiso se aplica tras la aprobación

**Rol:** Estándar A.

Antes de aprobar `SOLICITUD_A`, `GET /api/users` con `TOKEN_A` debe responder `403`. Después de ejecutar RF-GP-03, el mismo `GET /api/users` debe responder `200` y devolver la lista resumida de usuarios (`id` y `nombre`). No se requiere insertar nada manualmente en `PermisosUsuario`: la aprobación crea la concesión efectiva.

#### RF-GP-06: doble aprobación y transición prohibida

**Rol:** Administrador.

1. Repite la aprobación de `SOLICITUD_A`, que ya está `Aplicada`. Debe responder `409` con el mensaje `Solo se pueden aprobar solicitudes Pendientes.`
2. Intenta rechazar `SOLICITUD_A`. Debe responder `409` con el mensaje `Solo se pueden rechazar solicitudes Pendientes.`
3. `GET /api/permission-requests/mine` con `TOKEN_A` debe seguir mostrando `estado: 4`, con los mismos `resueltaPorId`, `resueltaEn` y `aplicadaEn`.
4. Opcionalmente, comprueba que no se duplicó la concesión:

```sql
SELECT COUNT(*)
FROM PermisosUsuario
WHERE UsuarioId = '<id de estandar.a>' AND Permiso = 'ConsultarUsuarios';
```

El resultado debe ser `1`.

#### RF-GP-07: un Administrador no resuelve su propia solicitud

**Roles:** Administrador original y Estándar A.

La API no permite que un Administrador cree una solicitud: `POST /api/permission-requests` está restringido a `Estandar`. Para probar la resolución propia por HTTP, crea la solicitud como Estándar y después promueve temporalmente a ese usuario:

1. Con `TOKEN_A`, crea `SOLICITUD_A2` con `{"permiso":3}`.
2. Con `TOKEN_ADMIN`, ejecuta `GET /api/users` y obtén el `id` de Estándar A. Guárdalo como `ID_A`.
3. Con `TOKEN_ADMIN`, promueve a A:

```bash
curl -i -X PUT "$BASE/api/users/$ID_A/role" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -H "Content-Type: application/json" \
  -d '{"rol":1}'
```

**Esperado:** `204 No Content`. El cambio de rol revoca las sesiones de A.

4. Inicia sesión de nuevo como A y guarda el nuevo token como `TOKEN_A_ADMIN`. `GET /api/auth/me` debe mostrar el rol `Administrador`.
5. Con `TOKEN_A_ADMIN`, intenta aprobar su propia solicitud:

```bash
curl -i -X POST "$BASE/api/permission-requests/$SOLICITUD_A2/approve" \
  -H "Authorization: Bearer $TOKEN_A_ADMIN"
```

**Esperado:** `403 Forbidden` con el mensaje `No puedes aprobar tu propia solicitud.`

6. Opcionalmente, repite con `reject` y un motivo válido; debe responder `403` con `No puedes rechazar tu propia solicitud.`
7. Comprueba que `SOLICITUD_A2` sigue pendiente con `GET /api/permission-requests/pending` usando `TOKEN_ADMIN`, o con `GET /api/permission-requests/mine` usando `TOKEN_A_ADMIN`.
8. Restaura el rol de A con el Administrador original:

```bash
curl -i -X PUT "$BASE/api/users/$ID_A/role" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -H "Content-Type: application/json" \
  -d '{"rol":2}'
```

**Esperado:** `204 No Content`. No uses la misma cuenta para promover y degradar a A, porque un Administrador no puede quitarse su propio rol.

La suite automatizada también cubre este requisito con `AprobarAsync_ResolucionPropia_NoModificaSolicitud` y `RechazarAsync_ResolucionPropia_NoModificaSolicitud`.

## Pruebas

Desde la raíz del repositorio, ejecutar la suite automatizada con:

```bash
dotnet test SISReservasEspacios.slnx --filter "FullyQualifiedName!~PasswordPolicyTests"
```

La suite utiliza EF Core InMemory y no inicia la API ni modifica LocalDB. Las pruebas siguen el patrón Arrange–Act–Assert y verifican, entre otros aspectos, las transiciones permitidas y prohibidas, la autorización por rol y la gestión de solicitudes de permisos.

Entre las pruebas automatizadas se encuentran:

* `AprobarAsync_SolicitudRechazada_NoCambiaEstado`
* `AprobarAsync_MismaSolicitudDosVeces_NoDuplicaPermiso`
* `AprobarAsync_Estandar_DevuelveRolNoPermitido`
* `AprobarAsync_ResolucionPropia_NoModificaSolicitud`
* `RechazarAsync_MotivoVacio_NoModificaSolicitud`
* `AprobarAsync_Pendiente_RegistraResolutorYFecha`
* `EsPermitida_RespetaLaMaquinaDeEstados`

## Defectos

Cada etiqueta `practica-2-defecto-N` identifica un commit independiente que introduce un defecto intencional sobre la base `practica-2`. Para verificarlo, cambiar a la etiqueta correspondiente y ejecutar el comando de pruebas indicado en la sección anterior. La prueba asociada debe fallar al detectar el comportamiento incorrecto.

| Etiqueta | Prueba que detecta el defecto | Comportamiento detectado |
|---|---|---|
| `practica-2-defecto-1` | `RechazarAsync_MotivoVacio_NoModificaSolicitud` | Permite rechazar una solicitud con un motivo vacío o compuesto por espacios en blanco. |
| `practica-2-defecto-2` | `EsPermitida_RespetaLaMaquinaDeEstados` | Permite la transición de `Aprobada` a `Rechazada`. |
| `practica-2-defecto-3` | `AprobarAsync_Estandar_DevuelveRolNoPermitido` | Omite la validación del rol y permite que un usuario Estándar apruebe solicitudes. |
| `practica-2-defecto-4` | `AprobarAsync_ResolucionPropia_NoModificaSolicitud` | Invierte la validación de resolución propia: bloquea solicitudes ajenas y permite resolver solicitudes propias. |
| `practica-2-defecto-5` | `EsPermitida_RespetaLaMaquinaDeEstados` | Permite la transición de `Aplicada` a `Rechazada`. |
| `practica-2-defecto-6` | `AprobarAsync_Pendiente_RegistraResolutorYFecha` | No registra el identificador del administrador que resuelve la solicitud ni la fecha de resolución. |

La etiqueta `practica-2` representa la versión base, en la que la suite debe pasar sin fallos. Cada defecto se mantiene en su propio commit y su respectiva etiqueta.


### Estado de verificación

Comprobado anteriormente:

* Swagger UI respondió `200`.
* `/swagger/v1/swagger.json` respondió `200`.
* Sin autenticación, `GET /api/permission-requests/mine`, `GET /api/permission-requests/pending` y `GET /api/users` respondieron `401`.
* La migración `20261019220956_AgregarGestionPermisos` fue aplicada y sus tablas fueron verificadas en LocalDB.

### Demostración de RD-09

El 20 de octubre de 2026 se demostró RD-09 con una base LocalDB aislada y desechable:

1. Se creó la base `SISReservasEspacios_RD09_20261020_Audit`.
2. Se aplicaron las migraciones con un script idempotente de `dotnet ef` y `sqlcmd`, sin ejecutar `dotnet ef database update` sobre la base principal.
3. Se registraron tres usuarios de prueba y se activaron o promovieron únicamente en la base desechable.
4. Se crearon dos solicitudes: una fue aprobada y la otra rechazada.
5. Se detuvo la API y se inició de nuevo sobre la misma base.
6. Tras el reinicio se comprobó que:
   * La solicitud aprobada seguía en estado `Aplicada` y el permiso `ConsultarUsuarios` seguía concedido.
   * La solicitud rechazada seguía en estado `Rechazada` con su motivo.
   * `GET /api/permission-requests/mine` devolvió ambas solicitudes.
   * `GET /api/users` respondió `200` para el usuario con el permiso concedido.
   * `GET /api/permission-requests/pending` devolvió `0`.
7. La base de prueba se eliminó al finalizar. La base `SISReservasEspacios` no se modificó.

Pendiente:

* Probar la matriz HTTP completa de errores y transiciones.
* Probar concurrencia real contra SQL Server.

Las comprobaciones HTTP de solo lectura no equivalen por sí solas a una prueba HTTP completa; la demostración de RD-09 sí incluyó escrituras, reinicio y consulta posterior en una base aislada.

## Cómo probar los criterios de aceptación de la Práctica 1

### Registro y activación

1. Registrar un usuario con un correo válido.
2. Intentar registrar nuevamente el mismo correo.
3. Intentar registrar una contraseña de menos de 8 caracteres o sin letras y números.
4. Intentar registrar un correo con formato inválido.
5. Verificar que la cuenta queda inactiva antes de la activación.
6. Utilizar el enlace de activación recibido.
7. Intentar utilizar nuevamente el mismo enlace.
8. Verificar que el token no puede reutilizarse.

### Sesión

1. Intentar iniciar sesión con credenciales incorrectas.
2. Verificar que la respuesta no indica si falló el correo o la contraseña.
3. Iniciar sesión correctamente.
4. Consultar el usuario autenticado.
5. Cerrar sesión.
6. Intentar utilizar nuevamente la credencial de sesión.
7. Realizar cinco intentos consecutivos con contraseña incorrecta.
8. Intentar iniciar sesión con la contraseña correcta durante el bloqueo.

### Roles y administración

1. Iniciar sesión como usuario Estándar.
2. Intentar acceder manualmente a una operación exclusiva del Administrador.
3. Intentar cambiar un rol siendo Estándar.
4. Iniciar sesión como Administrador.
5. Consultar la lista de usuarios y verificar rol y estado.
6. Cambiar el rol de un usuario.
7. Desactivar y reactivar un usuario.
8. Intentar desactivar la propia cuenta del Administrador.

### Recuperación y contraseñas

1. Solicitar recuperación utilizando un correo existente.
2. Solicitar recuperación utilizando un correo inexistente y comprobar que la respuesta sea equivalente.
3. Utilizar el código recibido.
4. Intentar reutilizar el mismo código.
5. Intentar utilizar un código expirado.
6. Comprobar que la contraseña anterior deja de funcionar después del cambio.
7. Comprobar que las sesiones anteriores quedan invalidadas.
8. Cambiar la contraseña proporcionando la contraseña actual incorrecta y verificar el rechazo.

### Correos en cola

Los correos de activación, recuperación y restablecimiento forzado deben registrarse en `CorreoEnCola`.

Si el servidor SMTP no está disponible, la operación principal debe poder completarse y el correo debe permanecer pendiente en la cola.

Las credenciales SMTP se proporcionan mediante variables de entorno y nunca deben almacenarse en el repositorio.

### Máquina de estados

La máquina de estados de `Reserva` se encuentra documentada en:

```text
docs/maquina-de-estados.md
```

Los estados se encuentran centralizados en `EstadoReserva` y las transiciones en `TransaccionesReserva`.

## Estructura principal

```text
SISReservasEspacios/
├── backend/
│   └── SISReservas.Api/
│       ├── Controllers/
│       ├── Data/
│       ├── DTOs/
│       ├── Migrations/
│       ├── Models/
│       ├── Services/
│       └── Program.cs
├── docs/
│   ├── bitacora-asignacion-1.md
│   ├── bitacora-asignacion-2.md
│   └── maquina-de-estados.md
├── tests/
│   └── SISReservas.Api.Tests/
├── SISReservasEspacios.slnx
└── README.md
```

## Seguridad

No incluir en commits:

* Contraseñas reales.
* Credenciales SMTP.
* Tokens de activación o recuperación.
* Archivos generados o secretos de configuración.

## Práctica 1

El punto de entrega de la Práctica 1 corresponde al tag:

```text
practica-1
```

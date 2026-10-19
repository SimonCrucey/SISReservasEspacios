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

## Pruebas

Desde la raíz del repositorio:

```bash
dotnet test SISReservasEspacios.slnx
```

Resultado de la última ejecución: **52 aprobadas, 0 fallidas, 0 omitidas**.

Las pruebas usan EF Core InMemory y no inician la API ni modifican LocalDB. La suite incluye, entre otras, las pruebas de transiciones prohibidas:

* `AprobarAsync_SolicitudRechazada_NoCambiaEstado`
* `AprobarAsync_MismaSolicitudDosVeces_NoDuplicaPermiso`

### Estado de verificación

Comprobado anteriormente:

* Swagger UI respondió `200`.
* `/swagger/v1/swagger.json` respondió `200`.
* Sin autenticación, `GET /api/permission-requests/mine`, `GET /api/permission-requests/pending` y `GET /api/users` respondieron `401`.
* La migración `20261019220956_AgregarGestionPermisos` fue aplicada y sus tablas fueron verificadas en LocalDB.

Pendiente:

* Demostrar RD-09 mediante un reinicio real de la API y una consulta posterior.
* Probar por HTTP los flujos autenticados de creación, aprobación, rechazo y consulta.
* Probar concurrencia real contra SQL Server.

Las comprobaciones HTTP anteriores fueron de solo lectura y no equivalen a una prueba HTTP completa.

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

# SISReservasEspacios

Sistema web para gestionar reservas de espacios, usuarios y control de acceso.

## Tecnologías

* C#
* ASP.NET Core Web API
* .NET 8
* Entity Framework Core
* SQL Server

## Requisitos previos

* .NET SDK 8.0 o superior
* SQL Server
* Visual Studio 2022/2026 o VS Code

## Ejecución del proyecto

Desde la raíz del repositorio:

```bash
dotnet restore
dotnet build
dotnet run --project backend/SISReservas.Api/SISReservas.Api.csproj
```

La API se ejecuta mediante el proyecto `SISReservas.Api`.

## Base de datos

Configurar la cadena de conexión de SQL Server en la configuración correspondiente del proyecto antes de ejecutar la aplicación.

Si existen migraciones pendientes:

```bash
dotnet ef database update --project backend/SISReservas.Api/SISReservas.Api.csproj
```

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

## Cómo probar los criterios de aceptación

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
│       ├── Data/
│       ├── Models/
│       ├── Services/
│       └── Program.cs
├── docs/
│   └── maquina-de-estados.md
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

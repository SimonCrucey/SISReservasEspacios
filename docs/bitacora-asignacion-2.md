# Bitácora de Interacción con Agente de IA — Asignación 2

**Asignatura:** Programación III (TDS-007) — ITLA
**Período:** 2026-C-3
**Estudiante:** Wuanny Simon Crucey Vasquez
**Repositorio:** SISReservasEspacios
**Herramienta de IA:** OpenCode CLI

---

## 1. Objetivo de la asignación

Implementar la gestión de permisos de usuarios dentro de SISReservasEspacios, incluyendo:

- Solicitudes de permisos realizadas por usuarios con rol Estándar.
- Consulta de solicitudes propias y consulta administrativa de solicitudes pendientes.
- Aprobación y rechazo de solicitudes por parte de Administradores.
- Registro del resolutor, la fecha de resolución y el motivo de rechazo.
- Concesión automática del permiso efectivo cuando una solicitud es aprobada.
- Protección contra transiciones de estado inválidas y resolución de solicitudes propias.
- Persistencia de solicitudes y permisos en SQL Server.

---

## 2. Requisitos implementados

| Requisito | Implementación |
|---|---|
| RF-GP-01 | La solicitud nace en estado `Pendiente`. |
| RF-GP-02 | El Administrador consulta solicitudes pendientes; el Estándar solo consulta las propias. |
| RF-GP-03 | La aprobación registra el Administrador resolutor y la fecha de resolución. |
| RF-GP-04 | El rechazo exige un motivo y registra el estado, el resolutor y la fecha. |
| RF-GP-05 | La aprobación concede automáticamente el permiso efectivo. |
| RF-GP-06 | Las transiciones prohibidas se rechazan y no alteran el estado persistido. |
| RF-GP-07 | Un usuario no puede aprobar ni rechazar su propia solicitud. |
| RD-09 | La persistencia usa SQL Server; fue demostrada mediante un reinicio real de la API el 20 de octubre de 2026. |

La implementación se encuentra en `SolicitudPermisoService`, `PermisoService`, `PermissionRequestsController`, `TransicionesSolicitudPermiso` y los modelos `SolicitudPermiso` y `UsuarioPermiso`.

---

## 3. Trabajo delegado a la IA

No se conserva una transcripción literal completa de todos los prompts y respuestas dentro del repositorio. El siguiente resumen se basa en las tareas que realmente se ejecutaron durante el desarrollo y la auditoría.

### 3.1 Tareas solicitadas

Se solicitó al agente de IA apoyo para:

- Inspeccionar la API, controladores, servicios, modelos, DTOs, migración y pruebas existentes.
- Preparar y ejecutar verificaciones HTTP de solo lectura sobre Swagger y rutas protegidas.
- Auditar la implementación contra los requisitos RF-GP-01 a RF-GP-07 y RD-09.
- Auditar la máquina de estados de las solicitudes de permisos.
- Revisar la independencia, nombres y cobertura de la suite de pruebas.
- Añadir pruebas unitarias para cubrir transiciones prohibidas y doble aprobación.
- Revisar y corregir la documentación de la asignación.

### 3.2 Resultados obtenidos

- Se verificó que la suite utiliza EF Core InMemory y no levanta la aplicación completa.
- Se ejecutó `dotnet test SISReservasEspacios.slnx` y se obtuvieron **52 pruebas aprobadas, 0 fallidas y 0 omitidas**.
- Se añadieron las pruebas:
  - `AprobarAsync_SolicitudRechazada_NoCambiaEstado`
  - `AprobarAsync_MismaSolicitudDosVeces_NoDuplicaPermiso`
- Se documentaron las rutas HTTP, roles, flujo de aprobación/rechazo y limitaciones.
- Se demostró RD-09 con una base LocalDB aislada, dos solicitudes y un reinicio real de la API.
- Se mantuvo la lógica de producción sin cambios durante la etapa de documentación.

---

## 4. Error real detectado y corregido

### 4.1 Error documental

El `README.md` indicaba que el proyecto utilizaba **.NET 8**, mientras que los archivos de proyecto (`SISReservas.Api.csproj` y `SISReservas.Api.Tests.csproj`) declaran `net10.0`.

- **Detección:** comparación entre el README y los archivos `.csproj` durante la auditoría documental.
- **Corrección:** el README se actualizó para indicar .NET 10.
- **Alcance:** corrección exclusivamente documental; no se modificó código de producción.

### 4.2 Brecha de cobertura corregida

La auditoría de RF-GP-06 detectó que faltaba una prueba directa del servicio para intentar aprobar una solicitud previamente `Rechazada`, y otra para comprobar una doble aprobación real.

- **Detección:** revisión de `SolicitudPermisoServiceTests.cs` y de la matriz de requisitos.
- **Corrección:** se añadieron dos pruebas unitarias a `SolicitudPermisoServiceTests.cs`.
- **Resultado:** la suite pasó de 50 a 52 pruebas aprobadas.

No se identificó ni se atribuye al agente un defecto ficticio de lógica de producción.

### 4.3 Cambio de producción detectado: `DbSet<Reserva>`

La auditoría detectó que el árbol de trabajo eliminó `DbSet<Reserva> Reservas`, presente en `HEAD`. La investigación se realizó con una copia temporal fuera del repositorio, sin alterar LocalDB.

- **Evidencia histórica:** el commit `6994458` añadió `DbSet<Reserva>`, `Reserva.cs` y la documentación de la máquina de estados, pero no una migración. El snapshot de EF Core y LocalDB nunca incluyeron la tabla `Reservas`.
- **Prueba de restauración:** al restaurar la propiedad en la copia temporal, `dotnet ef migrations has-pending-model-changes` reportó cambios y una migración temporal generó `CreateTable("Reservas")` con `Id int`, `UsuarioId int`, `FechaInicio`, `FechaFin` y `Estado int`, sin clave foránea. La migración temporal no se aplicó.
- **Decisión:** mantener la eliminación (opción B) para conservar la consistencia entre el modelo, el snapshot y LocalDB. Restaurar la propiedad introduciría una tabla y una migración ajenas a la asignación; la asignación no requiere `Reserva`.
- **Alcance:** no se eliminaron `Reserva.cs`, `TransicionesReserva` ni la documentación de reservas. No se modificó `Reserva.UsuarioId` ni se creó una migración para reservas.
- **Verificación:** build sin errores, 52/52 pruebas aprobadas y `git diff --check` sin errores.

Esta decisión debe revisarse si el responsable del proyecto decide completar la integración de `Reserva` con una migración y la corrección de tipos correspondiente.

---

## 5. Pruebas ejecutadas

### 5.1 Comando

Desde la raíz del repositorio:

```bash
dotnet test SISReservasEspacios.slnx
```

### 5.2 Resultado

```text
Passed! - Failed: 0, Passed: 52, Skipped: 0, Total: 52
```

### 5.3 Pruebas nuevas de transiciones prohibidas

- `AprobarAsync_SolicitudRechazada_NoCambiaEstado`
  - Verifica que una solicitud `Rechazada` no puede aprobarse.
  - Comprueba `EstadoInvalido`.
  - Verifica que el estado persistido sigue siendo `Rechazada`.
  - Verifica que no se crea una concesión.
  - Verifica que no se alteran el resolutor, la fecha ni el motivo originales.

- `AprobarAsync_MismaSolicitudDosVeces_NoDuplicaPermiso`
  - Verifica que la primera aprobación termina en `Aplicada`.
  - Verifica que existe una concesión efectiva.
  - Verifica que la segunda aprobación devuelve `EstadoInvalido`.
  - Verifica que el estado persistido sigue en `Aplicada`.
  - Verifica que sigue existiendo una sola concesión para el usuario y permiso.

### 5.4 Tipo de pruebas

Las pruebas utilizan `Microsoft.EntityFrameworkCore.InMemory` mediante `TestDatabase`. No levantan la aplicación completa, no usan HTTP y no modifican LocalDB.

### 5.5 Demostración de RD-09

La persistencia se comprobó con una base LocalDB aislada y desechable, sin usar la base principal del proyecto:

1. Se creó `SISReservasEspacios_RD09_20261020_Audit`.
2. Se aplicaron las migraciones con `dotnet ef migrations script --idempotent` y `sqlcmd`, sin ejecutar `dotnet ef database update`.
3. Se registraron tres usuarios de prueba mediante `POST /api/auth/register` y se activaron o promovieron con SQL únicamente en la base desechable.
4. Se crearon dos solicitudes con `POST /api/permission-requests`: una para aprobar y otra para rechazar.
5. El Administrador aprobó una solicitud y rechazó la otra con el motivo `Prueba RD-09`.
6. Se detuvo la API y se inició de nuevo sobre la misma base, en el puerto `5088`.
7. Después del reinicio:
   - `GET /api/permission-requests/mine` devolvió la solicitud aprobada en estado `Aplicada` y la rechazada en estado `Rechazada`.
   - El motivo de rechazo persistió.
   - `GET /api/users` respondió `200` para el usuario con `ConsultarUsuarios`.
   - `GET /api/permission-requests/pending` devolvió `0`.
   - La tabla `PermisosUsuario` conservó la concesión efectiva.
8. La base de prueba se eliminó al finalizar. La base `SISReservasEspacios` no se modificó.

---

## 6. Migración aplicada y verificaciones

La migración de la asignación es:

```text
20261019220956_AgregarGestionPermisos
```

En una etapa anterior se aplicó sobre `(localdb)\MSSQLLocalDB`, base `SISReservasEspacios`, y se verificó que:

- El historial de migraciones contenía la migración aplicada.
- Existían las tablas `SolicitudesPermiso` y `PermisosUsuario`.
- `PermisosUsuario` tiene un índice único por `UsuarioId` y `Permiso`.
- `SolicitudesPermiso` tiene `RowVersion` para control de concurrencia.
- La clave foránea de `ResueltaPorId` usa `DeleteBehavior.Restrict`.
- La base contenía 4 usuarios, 16 sesiones, 0 permisos y 0 solicitudes al momento de la verificación.

La migración no se reaplicó durante la etapa de documentación. Para RD-09 se aplicó un script idempotente sobre la base desechable `SISReservasEspacios_RD09_20261020_Audit`; esa base se eliminó después de la prueba y la base principal no se modificó.

---

## 7. Comprobaciones HTTP y demostración de persistencia

En una etapa anterior se inició la API en el perfil HTTP, puerto `5054`, y se realizaron únicamente comprobaciones de solo lectura:

- `GET /swagger` respondió **200**.
- `GET /swagger/v1/swagger.json` respondió **200**.
- Sin cabecera `Authorization`:
  - `GET /api/permission-requests/mine` respondió **401**.
  - `GET /api/permission-requests/pending` respondió **401**.
  - `GET /api/users` respondió **401**.

Estas comprobaciones confirman que Swagger está disponible en Development y que las rutas protegidas rechazan solicitudes sin autenticación.

### 7.1 Demostración RD-09

La prueba de persistencia se ejecutó el 20 de octubre de 2026 con la API en `http://127.0.0.1:5088` y la base desechable `SISReservasEspacios_RD09_20261020_Audit`:

- Antes de la aprobación, el usuario Estándar recibió `403` en `GET /api/users`.
- Se crearon dos solicitudes con `POST /api/permission-requests`; ambas quedaron en estado `Pendiente`.
- El Administrador aprobó una solicitud y rechazó la otra con el motivo `Prueba RD-09`.
- La aprobación produjo el estado `Aplicada` y una concesión en `PermisosUsuario`; el rechazo produjo el estado `Rechazada`.
- Se detuvo la API y se inició de nuevo sobre la misma base.
- Después del reinicio, `GET /api/permission-requests/mine` devolvió la solicitud aprobada en estado `Aplicada` y la rechazada en estado `Rechazada` con su motivo.
- `GET /api/users` respondió `200` para el usuario con `ConsultarUsuarios`.
- `GET /api/permission-requests/pending` devolvió `0`.
- La base de prueba se eliminó al finalizar; la base principal `SISReservasEspacios` no se modificó.

---

## 8. Decisiones de diseño

- **Estado intermedio:** durante una aprobación se recorren `Pendiente → Aprobada → Aplicada`. `Aprobada` es un estado intermedio en memoria y no se persiste. El estado final persistido es `Aplicada`, junto con `ResueltaEn` y `AplicadaEn`.
- **Rechazo:** una solicitud `Pendiente` pasa directamente a `Rechazada` y requiere un motivo no vacío de hasta 500 caracteres.
- **Concesión efectiva:** la aprobación crea un registro en `PermisosUsuario` si no existe; el índice único evita duplicados.
- **Administrador:** tiene acceso implícito a todos los permisos.
- **Estándar:** solo recibe los permisos concedidos explícitamente; `ConsultarUsuarios` es el permiso solicitable.
- **Resolución propia:** el servicio bloquea que el solicitante sea el mismo usuario que aprueba o rechaza.
- **Concurrencia:** `SolicitudesPermiso.RowVersion` permite detectar conflictos de actualización en SQL Server.
- **Pruebas:** se usa EF Core InMemory para pruebas unitarias rápidas e independientes.

---

## 9. Limitaciones y comprobaciones pendientes

- **RD-09 demostrado:** una solicitud aprobada y otra rechazada siguieron disponibles después de reiniciar la API, junto con la concesión efectiva del permiso.
- Se ejecutaron por HTTP la creación, aprobación, rechazo, consulta de solicitudes propias y consulta de usuarios con permiso. No se cubrió la matriz completa de errores y transiciones.
- No hay una prueba de concurrencia real contra SQL Server; la prueba de fallo de persistencia usa un interceptor y la suite usa InMemory.
- Las cuentas de prueba se crearon únicamente en la base desechable y se eliminaron con ella; no se versionaron credenciales.
- La base principal `SISReservasEspacios` no se modificó durante la demostración de RD-09.
- El cierre de la asignación se registra con el commit y el tag `asignacion-2`; la demostración de RD-09 se documenta en el `README.md` y en esta bitácora.

---

## 10. Evidencia disponible en el repositorio

- `backend/SISReservas.Api/Controllers/PermissionRequestsController.cs`
- `backend/SISReservas.Api/Services/PermisoService.cs`
- `backend/SISReservas.Api/Services/SolicitudPermisoService.cs`
- `backend/SISReservas.Api/Models/SolicitudPermiso.cs`
- `backend/SISReservas.Api/Models/UsuarioPermiso.cs`
- `backend/SISReservas.Api/Models/Enums/Permiso.cs`
- `backend/SISReservas.Api/Models/Enums/EstadoSolicitudPermiso.cs`
- `backend/SISReservas.Api/Models/Enums/TransicionesSolicitudPermiso.cs`
- `backend/SISReservas.Api/Migrations/20261019220956_AgregarGestionPermisos.cs`
- `tests/SISReservas.Api.Tests/SolicitudPermisoServiceTests.cs`
- `tests/SISReservas.Api.Tests/PermisoServiceTests.cs`
- `tests/SISReservas.Api.Tests/TransicionesSolicitudPermisoTests.cs`
- `tests/SISReservas.Api.Tests/UsersControllerPermissionTests.cs`

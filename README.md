# SISReservasEspacios

Sistema de reservas de salones, canchas y equipos desarrollado para **Programación III · TDS-007 · ITLA · 2026-C-3**.

Incluye Control de acceso, Gestión de permisos, reservas con máquina de estados, documentos con autorización y planificación exterior mediante WeatherAPI. La última versión publicada es Práctica 4. Práctica 5 se prepara localmente para revisión. Práctica 4 corresponde a la etiqueta `practica-4`, commit `64125eb9c74ab5811edb47cfaabf88dc23f32ddb`.

## Tecnologías y arquitectura

- C# y ASP.NET Core Web API sobre .NET 10.
- Entity Framework Core y SQL Server 2022.
- Docker Compose, con SQL Server y API comunicados por la red interna.
- Swagger para consultar contratos y ejecutar operaciones.
- Sesiones con token Bearer y roles Administrador/Estándar.
- Cola de correo en SQL, despachador SMTP y Mailpit para captura local.
- WeatherAPI para recomendaciones de uso de canchas exteriores.
- xUnit y EF InMemory para pruebas de servicios y reglas.

La API usa controladores, DTO, servicios y un DbContext. Las reglas de transición de Reservas están centralizadas en `TransicionesReserva`. Los archivos se almacenan fuera de SQL y sus metadatos se guardan en la base.

## Inicio rápido con Docker

Requiere Git, Docker Desktop con contenedores Linux y Compose. La imagen de SQL Server necesita un equipo compatible y recursos suficientes. Docker realiza la compilación y ejecuta SQL Server.

Desde PowerShell:

```powershell
git clone https://github.com/SimonCrucey/SISReservasEspacios.git
cd SISReservasEspacios
Copy-Item .env.example .env
docker compose up --build
```

Abre **http://localhost:5054/swagger**. El puerto se configura con `APP_PORT`. Compose espera el healthcheck de SQL y la API aplica las migraciones antes de atender peticiones. Mailpit arranca dentro de la red de Compose.

El healthcheck de `master` no garantiza que el catálogo de la aplicación haya
terminado de recuperarse tras un reinicio. El arranque comprueba su estado y el
acceso efectivo antes de migrar; un bloqueo de sesión en `master` serializa la
creación y las migraciones entre instancias. La espera total está limitada a
120 segundos, configurable mediante `Database__StartupTimeoutSeconds` (1–300).
Los errores conocidos de credenciales en `master`, permisos de migración,
esquema incompatible o catálogo offline producen un error explícito. El acceso
al catálogo puede seguir indisponible después de autenticar en `master`; ese
caso se espera hasta el límite, sin confundirlo con una contraseña inválida. No se usa
`EnsureCreated`, ni se borra o recrea una base existente. Este mecanismo requiere
permiso de conexión a `master` y los permisos de migración habituales.

En otra terminal puedes comprobar los servicios:

```powershell
docker compose ps
docker compose logs --tail=50 api
```

Para detener y volver a iniciar conservando los datos:

```powershell
docker compose down
docker compose up --build
```

Después de cambiar variables de la API en `.env`, recrea su contenedor:

```powershell
docker compose up -d --force-recreate api
```

Conserva las credenciales SQL del volumen existente. Cambiar la contraseña en `.env` no modifica automáticamente la contraseña de una instancia SQL ya inicializada.

## Configuración

Copia `.env.example` antes del primer arranque. Sus valores públicos de demostración permiten iniciar el entorno local. La clave meteorológica queda vacía y debe configurarse para recibir un pronóstico real.

| Variable | Uso |
| --- | --- |
| `MSSQL_SA_PASSWORD` | Contraseña SQL de la instalación. El ejemplo es público y solo sirve para demostración local. |
| `APP_PORT` | Puerto de la API en el host, por defecto 5054. |
| `EmailDispatcher__Enabled` | Activa el despacho de correos en cola. |
| `SMTP_HOST`, `SMTP_PORT`, `SMTP_FROM` | Servidor y remitente. En Compose se usa `mailpit:1025`. |
| `SMTP_ENABLE_SSL`, `SMTP_USE_AUTH` | STARTTLS y autenticación del servidor SMTP. Mailpit local usa ambos en false. |
| `SMTP_USERNAME`, `SMTP_PASSWORD` | Credenciales privadas si el servidor exige autenticación. |
| `WEATHER_API_KEY` | Clave propia de WeatherAPI. |
| `WEATHER_LOCATION` | Ubicación del recinto. El ejemplo usa coordenadas de Santo Domingo. |

Compose configura además `ConnectionStrings__DefaultConnection`, `Database__ApplyMigrations=true`, `AppBaseUrl` y `Documentos__Directorio=/app/data/documentos`.

`.env` está ignorado por Git y excluido de la imagen. Guarda claves y credenciales privadas únicamente en el entorno o configuración local ignorada. La composición publicada es de evaluación local, expone la API en loopback y habilita Swagger mediante Development. Un despliegue público requiere configuración propia de HTTPS, secretos y SMTP.

## Autenticación y correo

### Registro, activación y sesión

En Swagger:

1. Ejecuta `POST /api/auth/register` con `nombre`, `email` y `password`. La contraseña requiere al menos ocho caracteres, letras y números. La cuenta nace Estándar e inactiva.
2. Consulta el mensaje capturado en Mailpit y abre el enlace recibido de activación. Se consume una sola vez.
3. Ejecuta `POST /api/auth/login` con `email` y `password`.
4. Copia `sessionToken` en **Authorize**. Swagger usa el esquema Bearer. Para otros clientes, envía `Authorization: Bearer TOKEN`.
5. Consulta `GET /api/auth/me` y cierra con `POST /api/auth/logout`. La sesión revocada recibe 401 al reutilizarse.

Un HTTP 200 sin `sessionToken` en login puede indicar una cuenta inactiva o bloqueada. Para reenviar activación usa `POST /api/auth/resend-activation` con el correo como string JSON, por ejemplo `"persona@evaluacion.test.invalid"`. El reenvío invalida los enlaces anteriores.

### Consultar mensajes en Mailpit

Mailpit escucha en la red interna. El auxiliar `mail-inspector` permite consultar el buzón sin publicar otro puerto:

```powershell
docker compose run --rm --no-deps mail-inspector -fsS http://mailpit:8025/api/v1/messages
docker compose run --rm --no-deps mail-inspector -fsS http://mailpit:8025/api/v1/message/ID
```

Sustituye `ID` por el identificador del mensaje nuevo. Su texto contiene el enlace de activación o el código de recuperación. Conserva ese contenido en una terminal privada. El log de aceptación SMTP y el mensaje recibido son evidencias distintas. Al habilitar el despachador también se procesan los mensajes pendientes del catálogo conectado.

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

## Funcionalidades

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

Ejemplo de cuerpo. Ajusta el ID y las fechas a un espacio activo y un intervalo futuro:

```json
{"espacioId":1,"fechaInicio":"2027-01-20T14:00:00Z","fechaFin":"2027-01-20T15:00:00Z"}
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

## Notificaciones

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

### Cola de correo y reintentos

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
$env:EmailDispatcher__Enabled='false'
docker compose up -d --force-recreate api
docker compose stop mailpit
# Provocar el evento HTTP y ejecutar --procesar-correos tres veces.
# Consultar GET /api/correos desde Swagger.
docker compose start mailpit
Remove-Item Env:EmailDispatcher__Enabled
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

## Reportes

Ambos reportes requieren sesión. Administrador consulta información general,
Estándar únicamente la propia. Sus DTO JSON contienen grupos y totales, sin
devolver las entidades individuales.

| Reporte | Endpoint | Pregunta y fecha utilizada |
| --- | --- | --- |
| Core | GET `/api/reportes/solicitudes?inicio=...&fin=...` | ¿Cuántas solicitudes se crearon por estado en el periodo? Filtra por CreadaEn. |
| Reservas | GET `/api/reportes/reservas?inicio=...&fin=...` | ¿Qué espacios reciben más reservas y cuántas horas se reservan en cada uno? Filtra por intersección del intervalo. |

Ejemplos de consulta, con fechas UTC y datos de ese periodo:

```text
GET /api/reportes/solicitudes?inicio=2026-11-01T00:00:00Z&fin=2026-12-01T00:00:00Z
GET /api/reportes/reservas?inicio=2026-11-01T00:00:00Z&fin=2026-12-01T00:00:00Z
```

- Rango `[inicio, fin)`, UTC obligatorio, inicio anterior al fin y máximo diez años.
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

El contexto delegado y los criterios previos se encuentran en [AGENTS.md](AGENTS.md)
y [criterios de aceptación](docs/criterios-notificaciones-reportes.md).
La verificación HTTP/SQL reproducible está en
`tests/integracion/Validar-Practica5.ps1`, ejecutable con PowerShell 5.1 o posterior
y Docker Desktop. Crea una copia aislada y volúmenes nuevos, usa exclusivamente
configuración ficticia de .env.example y genera resultados sanitizados en una
carpeta temporal. No utiliza el .env privado del proyecto principal.

```powershell
.\tests\integracion\Validar-Practica5.ps1
```

## Persistencia

- `sql-data` conserva usuarios, sesiones, tokens, cola de correo, permisos, reservas y metadatos de documentos.
- `documentos-data` conserva los archivos físicos en `/app/data/documentos`.

Para comprobarla, registra y activa una cuenta, inicia sesión y sube un archivo. Comprueba listado y descarga. Ejecuta `docker compose down` y luego `docker compose up --build` desde el mismo proyecto. Inicia sesión, lista el documento y compara la descarga con el original.

**Evita `docker compose down -v`**, porque elimina los volúmenes. Conserva el nombre del proyecto Compose y las credenciales SQL para reutilizar los datos. El buzón de Mailpit no tiene un volumen persistente configurado.

## Pruebas

Con .NET SDK 10, desde la raíz:

```powershell
dotnet test SISReservasEspacios.slnx
```

En la rama `fix/practica5-auditoria-final`, la última ejecución comprobada terminó con **207 aprobadas, 0 fallidas y 0 omitidas**, con **0 errores y 0 advertencias** de compilación. Incluye 15 casos nuevos de arranque y 9 de serialización y compatibilidad UTC. La etiqueta `practica-5` no se ha movido: su ejecución anterior tras H-01 fue de 183 pruebas. La suite prueba reglas y servicios con bases EF InMemory y contiene pruebas SMTP de loopback. No exige iniciar la API ni disponer de SQL Server. Estas ejecuciones no demuestran que se haya resuelto la intermitencia histórica de ConfiguracionCorreoTests.

La validación funcional de Práctica 4 en una copia de trabajo aislada de Docker completó **114 comprobaciones**, incluidas **96 peticiones HTTP** y **31 combinaciones de ruta y método**. Incluyó correo recibido en Mailpit y persistencia tras down/up. Esa ejecución fue sobre una copia del trabajo, no un clon remoto. La consulta real a WeatherAPI se confirmó por separado. La verificación inicial de Práctica 5 desde un clon Git local completó 178 comprobaciones (139 HTTP). La regresión posterior de H-01, en una copia Docker aislada con SQL Server real, completó **188 comprobaciones (145 HTTP), 0 fallos y las 38 rutas/métodos cubiertos**, incluyendo precisión de fracciones de segundo. Detalles en [verificación de Práctica 5](docs/verificacion-practica-5.md).

La corrección posterior de arranque y UTC, solo en `fix/practica5-auditoria-final`,
pasó **243 comprobaciones, 172 peticiones HTTP y 38 rutas/métodos, sin fallos**.
Incluye SQL Server real, recepción SMTP en Mailpit, entradas JSON inválidas con
HTTP 400 y cinco ciclos down/up sobre los mismos volúmenes conservando datos.
No se volvió a probar entrega a buzones externos ni WeatherAPI con una clave real.

Para una revisión manual, usa Swagger y verifica registro/activación, login/logout, rechazo administrativo a Estándar, recuperación, aprobación/rechazo de permisos, reserva válida y transición prohibida. Prueba también Documentos y persistencia con dos cuentas para comprobar aislamiento.

## Desarrollo local

Docker es el camino de evaluación. Para ejecución local se requieren .NET SDK 10, SQL Server o LocalDB y un servidor SMTP si se desea completar activación y recuperación.

La configuración base usa LocalDB y el perfil `http` usa `http://localhost:5054`. SQL de Docker y LocalDB son instancias diferentes. Sus cuentas, sesiones y colas son independientes. Antes de migrar una base existente, verifica la conexión y respalda sus datos.

Configura `ConnectionStrings__DefaultConnection` para un catálogo nuevo y `Database__ApplyMigrations=true` para aplicar las migraciones al arrancar. Ajusta `AppBaseUrl`, `Documentos__Directorio` y las variables SMTP al entorno local. El `.env` de Compose no se carga automáticamente al ejecutar dotnet.

```powershell
dotnet run --project backend/SISReservas.Api --launch-profile http
```

.NET 10 requiere Visual Studio 2026 18.0 o posterior para soporte del IDE. En Visual Studio 2022 se puede utilizar la terminal integrada con el SDK instalado. La [guía acumulada](docs/entregas/guia-acumulada-practica-4.md) conserva los procedimientos extensos de LocalDB y acceso SQL de desarrollo. El archivo opcional `docker-compose.dev.yml` publica SQL en loopback para inspección local. La composición principal mantiene SQL interno.

## Errores comunes

| Síntoma | Comprobación |
| --- | --- |
| Puerto 5054 ocupado | Cambia `APP_PORT` en `.env` y vuelve a levantar Compose. Usa el nuevo puerto en Swagger. |
| SQL no pasa a healthy | Revisa `docker compose logs sqlserver`, recursos de Docker y credenciales del volumen existente. |
| Swagger aún no responde | Espera la inicialización y revisa `docker compose logs api`. Comprueba puerto y migraciones. |
| No llega el correo | Confirma que el despachador está habilitado y consulta el mensaje nuevo en Mailpit. |
| Enlace de activación rechazado | Usa el último mensaje. Reenviar invalida el enlace anterior. Verifica que accedes a la API de la misma base. |
| HTTP 401/403 | Comprueba token vigente, rol y permiso concedido. |
| WeatherAPI devuelve 503 | Comprueba clave, cuota, ubicación y que el intervalo esté cubierto por el pronóstico. |

## Historial de entregas

| Entrega | Etiqueta Git | Funcionalidades | Documentación |
| --- | --- | --- | --- |
| Asignación 1 | `asignacion-1` | Control de acceso | [Bitácora](docs/bitacora-asignacion-1.md) |
| Práctica 1 | `practica-1` | Estructura del proyecto y entidades | [Guía histórica](docs/entregas/guia-acumulada-practica-4.md) y README de la etiqueta |
| Asignación 2 | `asignacion-2` | Gestión de permisos y persistencia | [Bitácora](docs/bitacora-asignacion-2.md) |
| Práctica 2 | `practica-2`, defectos 1 a 6 | Pruebas y mutantes de permisos | [Defectos históricos](docs/entregas/guia-acumulada-practica-4.md#práctica-2-defectos-históricos-de-gestión-de-permisos) |
| Práctica 3 | `practica-3`, defectos 1 a 4 | Reservas, estados e integración con permisos | [Defectos y evidencias](docs/entregas/guia-acumulada-practica-4.md#defectos) |
| Práctica 4 | `practica-4` | Documentos, Docker, persistencia y WeatherAPI | [Guía publicada y evidencias acumuladas](docs/entregas/guia-acumulada-practica-4.md) |
| Práctica 5 | practica-5 (local, sin publicar) | Notificaciones, cola robusta y reportes | [Criterios](docs/criterios-notificaciones-reportes.md), [verificación](docs/verificacion-practica-5.md) |

Los nombres completos de los mutantes son `practica-2-defecto-N` (N=1…6) y `practica-3-defecto-N` (N=1…4). Cada etiqueta conserva su versión evaluable. La guía archivada mantiene tablas detectoras, hashes, auditorías, evidencia SMTP y limitaciones de sus respectivas etapas. Sus afirmaciones históricas se interpretan en ese contexto.

## Documentación adicional

- [Máquina de estados y cobertura](docs/maquina-de-estados.md).
- [Control de acceso y SMTP local](docs/control-acceso-local.md).
- [Auditoría de Práctica 3](docs/auditoria-practica-3.md).
- [Verificación de fase 4](docs/verificacion-fase-4.md).
- [Verificación de fase 6](docs/verificacion-fase-6.md).
- [Evidencia funcional HTTP/SQL de fase 6](docs/evidencia-fase-6.json).
- [Archivo de entregas y procedimientos anteriores](docs/entregas/guia-acumulada-practica-4.md).

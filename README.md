# SISReservasEspacios — Práctica 3

Programación III · TDS-007 · ITLA · 2026-C-3. Reservas de salones, canchas y
equipos con Control de acceso y Gestión de permisos.

Estado: 138 pruebas aprobadas y 73 verificaciones HTTP/SQL históricas. Las cinco
etiquetas anotadas practica-3 y practica-3-defecto-1 a 4 ya existen y están
publicadas. Esta revisión documental y los mutantes reconstruidos requieren
aprobación antes de sustituir las referencias publicadas anteriores.
Activación y recuperación se verificaron mediante mensajes realmente recibidos
por SMTP local en Mailpit; no se verificó entrega a buzones externos.
La [auditoría](docs/auditoria-practica-3.md) es un registro histórico anterior.

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

Clonar la URL entregada en Moodle y seleccionar
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

## Gestión de permisos

Enums JSON: GestionarUsuarios=1, RestablecerContrasenas=2, ConsultarUsuarios=3,
ReservarEspacios=4. Solo 3 y 4 son solicitables. Administrador tiene permisos
implícitos. Estados: Pendiente=1, Aprobada=2, Rechazada=3, Aplicada=4.

1. Como Estándar, POST `/api/permission-requests` con `{ "permiso": 4 }`:
   201/Pendiente. Guardar su id GUID como SOLICITUD.
2. GET `/api/permission-requests/mine`: solo propias. Administrador consulta
   GET `/api/permission-requests/pending`. Estándar en esta última ruta recibe 403.
3. Antes de aprobar, crear reserva según la sección siguiente: 403 y mine sin
   reserva del intento. Con solicitud Pendiente sigue sin poder reservar.
4. Como Administrador, POST `/api/permission-requests/{SOLICITUD}/approve`:
   200/Aplicada, resolutor y fechas. Aprobada es intermedio; se concede permiso.
5. Repetir approve o intentar reject de esa solicitud: 409; mine conserva estado 4.
6. Otro Estándar solicita permiso 3. Administrador ejecuta POST
   `/api/permission-requests/{id}/reject` con `{ "motivoRechazo": "" }`: 400 y
   sigue Pendiente. Con motivo no vacío de hasta 500 caracteres: 200/Rechazada.
   Aprobar la rechazada: 409; mine mantiene estado 3 y motivo.

GET `/api/users`: Administrador ve lista completa; Estándar sin permiso 3 recibe
403 y, después de aprobarlo, recibe resumen de id/nombre.
No se permite resolver solicitud propia: pruebas AprobarAsync_ResolucionPropia_NoModificaSolicitud
y RechazarAsync_ResolucionPropia_NoModificaSolicitud.

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

Los cuatro defectos de Práctica 3 están implementados y detectados. Las cinco
etiquetas anotadas ya están publicadas; su revisión documental conserva las
mutaciones y debe volver a validarse antes de actualizar las referencias remotas.

| Etiqueta | Defecto implementado | Prueba detectora | Comportamiento verificado |
| --- | --- | --- | --- |
| practica-3-defecto-1 | Permitir Cancelada → Confirmada | TransicionesReservaTests.AsegurarPermitida_ProhibidaDocumentada_RechazaSinCambiarReserva(Cancelada, Confirmada) | Rechazo y estado intacto |
| practica-3-defecto-2 | Permitir Completada → Cancelada | TransicionesReservaTests.AsegurarPermitida_DesdeTerminal_RechazaCualquierDestino(Completada, Cancelada) | Rechazo de la salida del estado terminal |
| practica-3-defecto-3 | Eliminar únicamente && e.Activo de CumpleCondicionesConfirmacionAsync durante la confirmación | ReservaServiceTests.Confirmar_CondicionIncumplida_RechazaSinCambiarEstado("inactivo") | Espacio inactivo no se confirma y conserva Pendiente; las demás condiciones permanecen |
| practica-3-defecto-4 | Retirar únicamente consulta de permiso en CrearAsync | ReservasAutorizacionTests.Crear_SolicitudPendienteRechazaYTrasAprobacionRealPermite | Antes de aprobar rechaza sin crear; tras aprobación real permite al mismo Estándar |

Cada commit defecto debe ser hijo directo del mismo commit practica-3, tocar solo
producción, nunca pruebas, y su mensaje indicar archivo y línea exacta cambiada.
Sobre practica-3 pasa toda la suite; sobre cada defecto falla al menos una prueba,
y el defecto 4 debe fallar en integración. Los cuatro defectos no forman una cadena.
Los mutantes originales produjeron cuatro fallos pertinentes para el defecto 1,
cuatro para el 2, uno para el 3 y dos para el 4, incluida la integración real.
Las etiquetas históricas practica-1/practica-2 y defectos P2 no prueban mutantes P3.

## Evidencia SMTP y estabilidad de pruebas

La verificación SMTP local completó 30 comprobaciones: seis mensajes recibidos
en Mailpit, cuatro antiguos de prueba y dos nuevos de activación y recuperación.
Se usaron el enlace y el código recibidos para completar ambos flujos; se rechazó
su reutilización y se verificaron el cambio de contraseña y la revocación de sesión.
No se comprobó entrega a buzones externos. No se publican secretos ni mensajes completos.

En el simulacro posterior desde un clon publicado, la base pasó 138/138 en nueve
ejecuciones. Se observaron fallos adicionales intermitentes en
ConfiguracionCorreoTests.Dispatcher_Deshabilitado_NoResuelveServiciosNiTocaBaseDeDatos
durante dos ejecuciones de mutantes. No se ha corregido ni se afirma resuelto;
los fallos pertinentes de cada mutación se verificaron por separado.

## Límites y entrega

- Recepción SMTP local comprobada en Mailpit; entrega externa no comprobada.
  La evidencia anterior de fase 6 con tokens de cola no demuestra correo real.
- Finalización automática y concurrencia simultánea SQL pendientes; seis
  funcionalidades públicas separables sí están implementadas y verificadas.
- Avisos NU1510 y NU1902/NU1903 (System.Security.Cryptography.Pkcs 9.0.11 en pruebas).
- Los archivos necesarios de producción, migración, pruebas y documentación están
  versionados. No versionar bin/obj, tokens, contraseñas, entorno ni secretos SMTP.
- Notificaciones/reportes de Reservas: semana 10. Adjuntos: opcionales; no implementados.

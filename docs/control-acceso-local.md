# Control de acceso: correo local y verificación manual

La fase 6 verificó posteriormente los flujos HTTP y RD-09 sin SMTP sobre una base
nueva aislada. Resultados y comandos en [verificacion-fase-6.md](verificacion-fase-6.md).
La activación y recuperación usaron tokens extraídos de la cola: la entrega SMTP
de esta guía sigue pendiente y no debe confundirse con esos resultados.

## Cambios mínimos

EmailSenderService ya sabía despachar la cola, pero no estaba registrado en DI ni
tenía un invocador. EmailDispatcherService lo resuelve en un scope nuevo cada diez
segundos, sin solapar ciclos. Si falla, registra el error y vuelve a intentar sin
detener la API. Activarlo es explícito: EmailDispatcher:Enabled es false por
defecto. No hay migraciones ni cambios al esquema.

AuthService usa AppBaseUrl para registro y reenvío. Se valida antes de guardar:
origen HTTPS absoluto o HTTP loopback local, sin credenciales, ruta, query ni
fragmento. No se deriva del Host de una petición. Producción requiere configurar
su origen HTTPS. Recuperación ya envía un código de un solo uso, no un enlace;
se conserva ese contrato.

## Preparación de correo local

Requiere un servidor SMTP de captura. No se presupone instalado. Una alternativa
es Docker con Mailpit, con puertos limitados al loopback del equipo:

```powershell
docker run --rm --name sisreservas-mailpit -p 127.0.0.1:8025:8025 -p 127.0.0.1:1025:1025 axllent/mailpit
```

Dejar esa terminal abierta. Consultar las instrucciones oficiales de
[Docker](https://mailpit.axllent.org/docs/install/docker/) y
[SMTP](https://mailpit.axllent.org/docs/configuration/smtp/).
Mailpit captura mensajes; no demuestra entrega a una bandeja externa.

En otra terminal PowerShell, desde la raíz:

```powershell
$env:SMTP_HOST='127.0.0.1'
$env:SMTP_PORT='1025'
$env:SMTP_FROM='reservas@test.invalid'
$env:SMTP_ENABLE_SSL='false'
$env:SMTP_USE_AUTH='false'
$env:EmailDispatcher__Enabled='true'
$env:AppBaseUrl='http://localhost:5054'
```

Antes de arrancar, configurar `ConnectionStrings__DefaultConnection` con una base
aislada ya preparada y confirmada como desechable. No usar las bases existentes
durante esta verificación. Esta fase no crea ni migra bases; consultar el
procedimiento RD-09 de [fase 4](verificacion-fase-4.md) para preparar posteriormente
el entorno con autorización explícita. No guardar cadenas con credenciales en Git.

```powershell
$env:ConnectionStrings__DefaultConnection='<cadena de la base desechable ya preparada>'
dotnet run --project backend/SISReservas.Api/SISReservas.Api.csproj --launch-profile http
```

Abrir http://localhost:8025 para la captura y http://localhost:5054/swagger para
las peticiones. Cada intento puede tardar un ciclo de diez segundos en aparecer.
Para SMTP autenticado, usar SMTP_USE_AUTH=true, SMTP_ENABLE_SSL=true y proporcionar
SMTP_USERNAME/SMTP_PASSWORD de forma privada al proceso. No habilitar despacho
contra una cola existente con destinatarios reales para estas pruebas.

## Verificación exacta usando Swagger

Usar únicamente cuentas ficticias en la base desechable. Introducir una contraseña
de prueba propia con al menos ocho caracteres, letras y números; no guardarla en
documentación ni capturas.

1. POST /api/auth/register: cuerpo con `nombre`, `email` (por ejemplo
   `estandar@test.invalid`) y `password`. Esperar 200. Confirmar que aparece el
   mensaje de activación en Mailpit, no solo una fila Pendiente en la base.
2. Abrir el enlace recibido; debe empezar por http://localhost:5054/api/auth/activate
   y devolver 200. Abrirlo de nuevo: debe rechazarlo con 400.
   POST /api/auth/resend-activation con un string JSON del correo permite probar
   reenvío antes de activar otra cuenta.
3. POST /api/auth/login con `email` y `password`: esperar 200 y un sessionToken
   no vacío. En Authorize pegar el token según la UI Bearer; verificar que envía
   `Authorization: Bearer <token>`. GET /api/auth/me debe devolver 200 y rol Estandar.
4. Como ese Estándar, GET /api/permission-requests/pending debe devolver 403.
   Sin token debe devolver 401. Esto comprueba el filtro HTTP, no solo el servicio.
5. POST /api/auth/logout con el token: esperar 200. GET /api/auth/me reutilizando
   ese token debe devolver 401. Volver a iniciar sesión para los pasos siguientes.
6. POST /api/passwords/recovery con `{ "email": "estandar@test.invalid" }`:
   esperar 200 y un correo de recuperación capturado en Mailpit. Copiar el código.
7. POST /api/passwords/reset con `token` (código recibido) y `nuevaPassword`:
   esperar 200. Repetir el mismo código: 400. El token de sesión anterior debe
   recibir 401 en /api/auth/me; login con contraseña antigua no debe devolver
   sessionToken; login con la nueva debe devolver uno válido.
8. Para Gestión de permisos usar las instrucciones RF-GP del README, con un
   Administrador preparado únicamente en la base de prueba. Comprobar crear,
   aprobar, rechazar y repetir una resolución inválida con estado intacto.
   ReservarEspacios tiene valor 4; ConsultarUsuarios conserva valor 3.

No confundir los estados: Pendiente significa encolado; Enviado indica que SMTP
aceptó el mensaje. Solo verlo en Mailpit acredita captura SMTP local; una entrega
externa requiere comprobar por separado la bandeja de destino.

## Evidencia de esta fase

Se arrancó temporalmente la API en loopback, puerto 15054, despacho deshabilitado
y cadena dirigida a un catálogo no utilizado, sin crear base ni ejecutar migraciones.
Swagger respondió 200; sin credencial /api/auth/me, /api/users,
/api/permission-requests/pending y /api/reservas/mine respondieron 401.
No se ejecutaron escrituras HTTP ni se usaron datos de las bases existentes.
Registro/activación, login/logout y recuperación se comprueban en servicios con
EF InMemory; 403 HTTP y los flujos completos con SMTP siguen pendientes del entorno
aislado. No se ha enviado ni capturado correo real en esta fase.

El despacho actual es adecuado para una sola instancia local: no tiene reclamación
distribuida de mensajes. Si SMTP acepta y luego falla SaveChanges, puede reenviar
en el siguiente ciclo. Evitar múltiples instancias despachando la misma cola.

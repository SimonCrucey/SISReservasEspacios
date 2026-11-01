# Verificación de fase 4

## Alcance comprobado

La integración `Crear_SolicitudPendienteRechazaYTrasAprobacionRealPermite` usa
SolicitudPermisoService, PermisoService y ReservaService reales, con EF InMemory.
Verifica ausencia de reservas antes de solicitar y durante la solicitud pendiente;
después de la aprobación comprueba el permiso efectivo y una reserva del mismo
usuario. Retirar únicamente la comprobación de permiso en CrearAsync dejaría
crear la primera reserva y fallaría la aserción Prohibido (defecto 4). Es una
conclusión por inspección; aún no se ha plantado ni ejecutado el mutante.

La regresión de acceso prueba servicios reales de registro/activación, login y
revocación de sesión, recuperación y cambio de contraseña con invalidación de
sesiones. Los correos se inspeccionan en cola: no se prueba entrega SMTP.
La comprobación de restricciones administrativas por atributos no sustituye una
petición HTTP real como Estándar. La suite existente prueba aprobación, rechazo,
transiciones inválidas y roles en los servicios de Gestión de permisos.

## RD-09 pendiente en SQL Server

La consulta de inventario de LocalDB encontró únicamente GastosPersonalesDb y
SISReservasEspacios. Ninguna está identificada como desechable; no se modificaron.
El README registra una base RD09 histórica que fue eliminada. InMemory no prueba
durabilidad frente al reinicio de la aplicación ni restricciones reales de SQL.
La migración nueva es aditiva: Up crea Espacios, Reservas, claves e índices;
Down solo elimina esas dos tablas nuevas. No se aplicó en esta fase.

### Procedimiento seguro para una ejecución posterior

1. Preparar una instancia SQL Server dedicada a pruebas, sin bases personales, y
   una base vacía identificada como desechable, por ejemplo
   SISReservasEspacios_RD09_Reservas_<identificador_unico>. Confirmar por separado
   servidor y catálogo; nunca reutilizar SISReservasEspacios.
2. Configurar una cadena exclusiva mediante `ConnectionStrings__DefaultConnection`
   en una terminal dedicada. No guardarla en appsettings ni en Git.
3. Aplicar únicamente a ese catálogo las migraciones con
   `dotnet ef database update --project backend/SISReservas.Api/SISReservas.Api.csproj --connection "<cadena_exclusiva_de_pruebas>"`.
   Verificar que __EFMigrationsHistory incluye
   20261101193103_AgregarEspaciosYReservas, además de las migraciones anteriores.
4. Ejecutar un comprobador .NET que use UseSqlServer con esa cadena y las entidades
   y servicios del proyecto: crear un usuario ficticio activo Administrador y un
   Espacio activo; guardar y llamar ReservaService.CrearAsync con fechas UTC
   futuras. Registrar el Id devuelto y comprobar Exito y estado Pendiente.
5. Disponer el contexto y cerrar el proceso. Abrir un proceso y contexto nuevos,
   con la misma cadena y sin EnsureCreated, recreación ni reinserción. Consultar
   Reservas.AsNoTracking por el Id registrado y comprobar UsuarioId, EspacioId,
   fechas y estado Pendiente. También verificar relaciones e historial de migración.
6. Conservar salida del comprobador y comando como evidencia. No introducir esta
   comprobación SQL silenciosamente en la suite InMemory: debe tener un comando
   separado y requerir configuración explícita, sin depender de datos compartidos.
   Cualquier limpieza posterior se limita al catálogo desechable confirmado.

## Pendientes para practica-3

- Ejecutar RD-09 y concurrencia sobre SQL Server aislado.
- Verificar HTTP real (401/403), los cinco frentes completos y entrega de correo.
- El enlace de activación está fijado a https://localhost:7014 en AuthService,
  mientras README ejecuta el perfil http en http://localhost:5054; comprobar el
  perfil HTTPS o corregir la configuración antes de la demostración por correo.
- EmailSenderService existe, pero Program no registra ni programa su ejecución;
  el README no proporciona un comando ejecutable para despachar la cola.
- Actualizar README: permiso 4, funcionalidades de Reservas, preparación de espacios,
  comando de suite completa, instrucciones de correo y sección Defectos.
- Finalización automática interna aún sin invocador del sistema.
- Plantar y demostrar los cuatro defectos y preparar commits/etiquetas solo cuando
  se autorice. Las advertencias NU1510, NU1902 y NU1903 siguen pendientes.

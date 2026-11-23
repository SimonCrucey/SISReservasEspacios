# SISReservasEspacios

Proyecto académico de reservas de espacios de Programación III, ITLA. Backend C#,
ASP.NET Core .NET 10, EF Core SQL Server, Docker Compose, SMTP/Mailpit y xUnit.

## Ejecución

- Compilar: `dotnet build SISReservasEspacios.slnx`.
- Probar: `dotnet test SISReservasEspacios.slnx`.
- Contenedor: el operador copia .env.example a .env y ejecuta `docker compose up --build`.
- Local: `dotnet run --project backend/SISReservas.Api --launch-profile http`, con conexión y SMTP en variables del proceso. Swagger: http://localhost:5054/swagger.

## Estructura y convenciones

Controllers expone HTTP, DTOs define contratos, Services aplica reglas por módulo,
Models representa entidades, Data configura EF y Migrations mantiene el esquema.
Security contiene los componentes de sesión y credenciales. Pruebas en tests/.
Usar async, nombres claros, DTO específicos, fechas UTC, validación controlada y
consultas agregadas en SQL. Mantener los namespaces existentes al mover archivos.
Todas las operaciones nuevas requieren Session Bearer y rol válido. Comprobar
propiedad y autorización también en servicios. No aceptar destinatarios arbitrarios
desde el cliente para modificar bandejas o preferencias.

## Restricciones

- Nunca leer .env ni archivos de secretos privados. Usar .env.example y variables ficticias.
- Nunca escribir credenciales, tokens ni cuerpos de correos privados en código, logs o documentación.
- No publicar nada ni ejecutar push, PR o releases sin autorización expresa.
- No mover etiquetas de prácticas anteriores ni reescribir historial.
- No borrar bases, tablas, documentos ni volúmenes existentes.
- Migraciones nuevas y aditivas, sin modificar migraciones históricas. Aplicarlas solo en entornos aislados autorizados.
- Preservar contratos, autenticación, permisos, estados de reservas, documentos y WeatherAPI.
- Reutilizar cola, emisor y despachador existentes. SMTP siempre fuera de la operación de negocio.
- Crear pruebas AAA independientes para las reglas nuevas y ejecutar regresión.
- Documentar endpoints, reintentos, filtros y evidencias realmente ejecutadas.
- El commit de criterios debe preceder a cualquier commit de implementación de Práctica 5.
- El panel HTML y scripts internos siguen fuera de la entrega.
- No modificar autenticación, registro, activación, sesiones, recuperación de contraseñas ni roles, salvo integración estrictamente necesaria.
- No alterar las reglas de negocio existentes de permisos, reservas y documentos.
- No modificar ni eliminar migraciones históricas de Entity Framework Core.
- No introducir cambios incompatibles en los endpoints existentes.
- No eliminar, deshabilitar ni modificar pruebas para ocultar fallos.
- No modificar etiquetas académicas ni reescribir el historial Git.
- No modificar módulos ajenos a las funcionalidades delegadas salvo necesidad justificada.
- Toda modificación excepcional debe ser mínima, justificarse y documentarse, y acompañarse de pruebas de regresión.

# SISReservasEspacios - Version 0.1

Sistema de Reservas de Espacios — Sistema web para gestionar la reserva y disponibilidad de diferentes espacios según fecha y horario. Permite administrar espacios, usuarios y reservas, validando conflictos de horarios y controlando el estado de cada reserva.

**Tecnologías:** C#, ASP.NET Core Web API, React, Entity Framework Core y SQL Server.

## Requisitos previos

- .NET SDK (8.0 o superior)
- Node.js y npm (para el frontend en React)
- SQL Server (local o instancia accesible)
- Visual Studio 2022 o VS Code

## Clonar el repositorio

```bash
git clone https://github.com/USUARIO/SISReservasEspacios.git
cd SISReservasEspacios
```

## Backend (ASP.NET Core Web API)

Restaurar dependencias:

```bash
dotnet restore
```

Configurar la cadena de conexión a SQL Server en `appsettings.json` (o `appsettings.Development.json`).

Aplicar migraciones (si el proyecto las tiene):

```bash
dotnet ef database update
```

Ejecutar el backend:

```bash
dotnet build
dotnet run --project NombreDelProyectoBackend
```

## Frontend (React)

Instalar dependencias:

```bash
cd nombre-carpeta-frontend
npm install
```

Ejecutar en modo desarrollo:

```bash
npm start
```

## Estructura del proyecto

Backend en ASP.NET Core Web API, frontend en React, persistencia con Entity Framework Core sobre SQL Server.
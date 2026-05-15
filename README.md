# Mobile Remote Toolkit API

Backend ASP.NET Core 8 para administracion remota de dispositivos moviles, con foco actual en Android (ADB + scrcpy), endpoints REST, SignalR y capa Business separada.

## Resumen

- API Web en .NET 8
- Arquitectura por capas: Api, Business, Domain
- Casos Android implementados con MediatR
- Soporte inicial iOS (stubs)
- Notificaciones en tiempo real con SignalR
- Swagger habilitado en Development

## Estructura de la solucion

```text
Mobile.Remote.Toolkit.Api/
  Mobile.Remote.Toolkit/                # Proyecto API (ASP.NET Core)
    Controllers/
      Android/AndroidController.cs
      iOS/IOSController.cs
      MonitoringController.cs
      FilesController.cs
    Hubs/AndroidDeviceHub.cs
    Program.cs
  Mobile.Remote.Toolkit.Business/       # Logica de negocio, comandos y queries
  Mobile.Remote.Toolkit.Domain/         # Modelos/contratos de dominio
  Tools/                                # Binarios externos (adb/scrcpy, etc.)
  publish/                              # Salida de dotnet publish
```

## Requisitos

- Windows 10/11
- .NET SDK 8.0+
- Herramientas Android disponibles en:
  - Tools/Android/adb/adb.exe
  - Tools/Android/scrcpy/scrcpy.exe

## Inicio rapido

Desde la raiz del backend:

```bash
dotnet restore Mobile.Remote.Toolkit.sln
dotnet build Mobile.Remote.Toolkit.sln
dotnet run --project Mobile.Remote.Toolkit/Mobile.Remote.Toolkit.Api.csproj --configuration Debug
```

Por defecto en Development (launchSettings):

- https://localhost:59399
- http://localhost:59400

Swagger UI disponible en Development al iniciar la API.

## Configuracion relevante

### CORS

Program.cs define una politica AllowVueApp para:

- http://localhost:3000
- http://localhost:8080
- http://localhost:5173
- https://localhost:3000
- https://localhost:8080
- https://localhost:5173

Si la variable ELECTRON_HOSTED=1, se habilita una politica permisiva para permitir origen null (renderer file:// en Electron).

### Logging

Configuracion de consola con formato simple y nivel:

- Default: Information
- Mobile.Remote.Toolkit: Debug
- Microsoft.AspNetCore: Warning

## Endpoints principales

### Android

Base: /api/android

- GET /devices
- GET /devices/active
- GET /devices/{serial}/status
- POST /devices/{serial}/mirror/start
- POST /devices/{serial}/mirror/stop
- POST /devices/{serial}/screenshot
- POST /devices/{serial}/action
- POST /devices/{serial}/adb

### Monitoring

Base: /api/monitoring

- GET /status
- POST /start
- POST /stop

### iOS (estado actual)

Base: /api/ios

Endpoints disponibles como placeholders (no implementados funcionalmente).

## SignalR

Hub:

- /hubs/android

Capacidades:

- Unirse/salir de grupos por serial de dispositivo
- Solicitar estado de dispositivo al hub

## Publicacion

Publicacion manual (ejemplo win-x64):

```bash
dotnet publish Mobile.Remote.Toolkit/Mobile.Remote.Toolkit.Api.csproj -c Release -r win-x64 --self-contained true -o publish
```

El cliente Electron del workspace incluye scripts para automatizar este publish dentro del flujo de distribucion.

## Notas de integracion con el frontend

El cliente scrcpy-manager-vue consume por defecto:

- http://localhost:59399/api (configurable con VITE_API_BASE_URL)

Para desarrollo de punta a punta en este workspace, se recomienda usar el script dev:full del proyecto Electron.

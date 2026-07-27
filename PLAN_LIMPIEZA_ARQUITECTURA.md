# Plan de limpieza y reestructuración a Clean Architecture — Mobile.Remote.Toolkit.Api

## Contexto

El sistema nació como un mirror/control remoto de Android e iOS, pensado originalmente para
consumirse desde la web por cualquier cliente. Con el tiempo, por temas de seguridad y
limitaciones encontradas, quedó acoplado a Windows (WMI para monitoreo USB, scrcpy abriendo
una ventana nativa que Electron detecta por título), y el soporte de iOS quedó a medio camino:
la capa de negocio está construida y funcional, pero el controller HTTP de iOS nunca se conectó
a ella.

Una primera pasada de este plan proponía solo borrar código muerto y dejar la estructura de
3 proyectos (Api/Business/Domain) básicamente como está. Se rechazó ese enfoque: **no hay que
asumir que lo que existe hoy ya está en la capa correcta.** El hallazgo real es más profundo
que "Domain está vacío, hay que borrarlo": el proyecto que hoy se llamaba **"Business" mezclaba
dos capas distintas** —
1. Casos de uso reales (Commands/Queries/Handlers de MediatR) — esto sí es capa de Aplicación.
2. Adaptadores de infraestructura (`AndroidDeviceService`/`IOSDeviceService` invocando procesos
   externos, `ProcessHelper` haciendo `Process.Start`, `DeviceMonitoringService` usando WMI,
   `FileService` tocando el filesystem) — esto es **Infraestructura**, no debería vivir junto a
   los casos de uso ni ser lo que la capa de Aplicación expone.

Este plan reorganiza el sistema en capas de verdad (Domain → Application → Infrastructure →
Api/Presentación), con dependencias apuntando siempre hacia adentro, siguiendo Clean
Architecture / inversión de dependencias de forma consistente para cada pieza — no solo para
las que ya "parecían" mal ubicadas.

Se mantiene la decisión ya confirmada: el transporte del mirror (ventana nativa de scrcpy) no
se toca en este plan — solo se prepara el terreno de monitoreo/herramientas para ser
multiplataforma. Reemplazar el mirror por streaming web (WebRTC/ws-scrcpy) queda como
iniciativa de roadmap futura, fuera de alcance aquí.

## Progreso

- ✅ **Fase 1 — Eliminar código muerto** (commit `c9e38a1`): proyecto VB.NET huérfano,
  `WeatherForecastController`/`WeatherForecast`, `DeviceMonitoringBackgroundService`,
  `GetToolsStatusQuery`/`AndroidController.GetToolsStatus` roto, todos eliminados.
- ✅ **Fase 2 — Renombrar Business → Application** (commit `ee450da`): proyecto, carpetas,
  namespaces, `.sln`, `Mobile.Remote.Toolkit.Api.csproj` y README actualizados. Se ejecutó antes
  de extraer Infrastructure, adelantando lo que en la numeración original era el paso 13 de la
  Fase 3 — se prioriza aquí porque simplifica el resto del trabajo (todo lo que se mueva después
  ya usa el namespace final).
- ⬜ Todo lo demás (Domain con contenido real, extracción de Infrastructure, composition root,
  simetría Android/iOS, conectar el controller de iOS, preparar monitoreo multiplataforma) sigue
  pendiente — ver fases 3 a 7 abajo.

## Arquitectura objetivo

```
Mobile.Remote.Toolkit.Domain            (Entidades + eventos de dominio. CERO dependencias externas)
        ↑
Mobile.Remote.Toolkit.Application       (Commands/Queries/Handlers + Puertos/Interfaces + DTOs.
                                          Depende solo de Domain + MediatR abstractions.
                                          CERO System.Management, CERO System.Diagnostics.Process,
                                          CERO acceso a filesystem/WMI directo)
        ↑
Mobile.Remote.Toolkit.Infrastructure    (NUEVO proyecto. Implementaciones concretas de los puertos:
                                          adb/scrcpy/libimobiledevice, WMI, filesystem, registries
                                          de procesos. Depende de Application + Domain)
        ↑
Mobile.Remote.Toolkit (Api)             (Controllers, Hubs, composition root. Depende de
                                          Application + Infrastructure)
```

Regla de oro para todo el plan: si un archivo toca `Process`, `System.Management`, el
filesystem, o una herramienta externa (adb/scrcpy/idevice*) → Infrastructure. Si define un
contrato/interfaz que otra capa implementa, o orquesta un caso de uso → Application. Si es un
concepto de negocio puro (una entidad, un evento de dominio) → Domain.

## Fase 3 — Domain con contenido real (no un proyecto vacío de adorno)

1. Resolver la duplicación real encontrada — `Models/Android/AndroidDevice.cs` y
   `Models/Device.cs` en Application definen la **misma clase `Device`** campo por campo —
   moviendo la versión canónica a `Mobile.Remote.Toolkit.Domain/Entities/Device.cs` como la
   entidad de dominio real (Serial, Platform, Name, Alias, FirstSeen, Active), y borrando ambos
   duplicados de Application. Es un concepto de negocio compartido entre Android e iOS:
   pertenece a Domain, no a un Models suelto de la capa de aplicación.
2. Mover los eventos de dominio hoy alojados como "detalle de implementación" de
   `DeviceMonitoringService` (`Application/Services/DeviceEventArgs.cs`,
   `DeviceStatusChangedEventArgs.cs`) a `Domain/Events/` — representan hechos de negocio
   (dispositivo conectado/desconectado/cambio de estado), no deberían depender de dónde vive el
   servicio que los dispara.
3. Domain sigue sin ningún paquete NuGet (ni MediatR, ni Logging, ni System.Management) — se
   verifica explícitamente al final de la fase (hoy ya está así: solo tiene el
   `Android/AndroidDevice.cs` viejo, que se reemplaza por la entidad `Device` unificada).

## Fase 4 — Separar puertos (Application) de adaptadores (Infrastructure)

Este es el cambio estructural central del plan.

4. Crear el proyecto `Mobile.Remote.Toolkit.Infrastructure` (net10.0), con `ProjectReference` a
   Application y Domain.
5. Mover las implementaciones concretas que tocan SO/proceso/filesystem/WMI/config externa,
   sacándolas de Application hacia Infrastructure:
   - `Services/Android/AndroidDeviceService.cs`, `Services/Android/MirrorProcessRegistry.cs` →
     `Infrastructure/Android/`
   - `Services/iOS/IOSDeviceService.cs`, `Services/iOS/IOSMirrorProcessRegistry.cs` →
     `Infrastructure/iOS/`
   - `Utils/ProcessHelper.cs` → `Infrastructure/Processes/`
   - `Utils/FileService.cs` → `Infrastructure/Files/`
   - `Services/DeviceMonitoringService.cs` → `Infrastructure/Monitoring/` (ver Fase 7 para el
     split Windows/multiplataforma)
   - `Services/LogNotificationService.cs` → `Infrastructure/Notifications/` (el notificador
     base que solo loguea; el que usa SignalR se queda en Api — depende de
     `IHubContext<AndroidDeviceHub>`, un detalle de hosting de ASP.NET, correcto que viva ahí)
6. Lo que se queda en Application (con su rol real ya acotado a casos de uso + contratos):
   - Commands/Queries/Handlers tal como están (esto siempre fue capa de Aplicación de verdad).
   - Interfaces/puertos: `IAndroidDeviceService`, `IIOSDeviceService`, `IProcessHelper`,
     `IFileService`, `INotificationService`, `IDeviceMonitoringService`, más sus DTOs
     (`ProcessResult`, Requests/Responses) — el contrato que los casos de uso consumen, sin
     saber cómo se implementa.
7. Mover `System.Management` y cualquier lectura de configuración específica de infraestructura
   (p. ej. `IOS:Mirror:Executable`) del `.csproj` de Application al de Infrastructure — la capa
   de Aplicación no debe referenciar paquetes atados a SO.
8. Actualizar `Mobile.Remote.Toolkit.sln` y `Mobile.Remote.Toolkit.Api.csproj` con la nueva
   referencia a Infrastructure.

## Fase 5 — Composition root limpio

9. Agregar `Mobile.Remote.Toolkit.Application/DependencyInjection.cs` con
   `AddApplication(this IServiceCollection services)` que envuelve el registro de MediatR.
10. Agregar `Mobile.Remote.Toolkit.Infrastructure/DependencyInjection.cs` con
    `AddInfrastructure(this IServiceCollection services, IConfiguration config)` que registra
    todo lo que hoy está disperso en `Program.cs` (`AndroidDeviceService`, `IOSDeviceService`,
    ambos registries, `ProcessHelper`, `FileService`, `DeviceMonitoringService` + su watcher).
11. `Program.cs` queda reducido a construir CORS/Controllers/Swagger/SignalR (lo que es
    genuinamente de la capa Api) y dos llamadas:
    `builder.Services.AddApplication(); builder.Services.AddInfrastructure(builder.Configuration);`

## Fase 6 — Simetría estructural Android/iOS

12. Crear `IOSBaseCommandHandler`/`IOSBaseQueryHandler` (mismo patrón que los de Android,
    inyectando `IIOSDeviceService`) y migrar los handlers anidados de iOS para heredar de ellos
    en vez de implementar `IRequestHandler<>` directo.
13. Unificar controllers bajo `BaseController` (`FilesController`, `MonitoringController`,
    `StatsController`, `iOSController`) para reusar `Mediator`/`Logger`/`ApiError`, igual que ya
    hace `AndroidController`.
14. Limpieza menor de consistencia: quitar el `using ...Services.Android` sobrante en el
    `BaseCommandHandler<>` genérico (no debería conocer Android en absoluto), y el cast
    redundante `(IRequest<ActionResponse>)request` en `AndroidController.ExecuteAdb`.

## Fase 7 — Conectar el pipeline de iOS ya construido + preparar terreno multiplataforma

15. Reescribir `Controllers/iOS/iOSController.cs` para inyectar `IMediator` y despachar los
    commands/queries ya existentes (`GetIOSDevicesQuery`, `GetIOSDeviceInfoQuery`,
    `GetIOSDeviceStatusQuery`, `StartIOSMirrorCommand`, `StopIOSMirrorCommand`,
    `TakeIOSScreenshotCommand`, `ExecuteIOSActionCommand`), replicando el patrón de
    `AndroidController`. No se escribe negocio nuevo, solo se cierra el circuito (hoy sigue
    devolviendo "iOS no implementado aún" hardcodeado en cada endpoint).
16. Documentar explícitamente que faltan los binarios compilados de `libimobiledevice` (solo
    está el código fuente vendorizado) — bloqueo funcional real para iOS, fuera de alcance de
    esta limpieza.
17. Dentro de `Infrastructure/Monitoring/`, extraer `IUsbHardwareWatcher` (puerto declarado en
    Application) desde `DeviceMonitoringService`:
    - `WindowsUsbHardwareWatcher`: la lógica WMI actual (`Win32_DeviceChangeEvent`), movida tal
      cual.
    - `PollingUsbHardwareWatcher`: fallback multiplataforma (timer periódico disparando el mismo
      `RefreshDeviceListAsync`) para que el monitoreo funcione también fuera de Windows en vez
      de quedar inactivo.
    - Selección de implementación por `RuntimeInformation.IsOSPlatform` dentro de
      `AddInfrastructure(...)`, no dentro del servicio.
18. Confirmar (sin tocar) que `ProcessHelper` ya resuelve adb/scrcpy en Windows/Linux/macOS
    correctamente.

## Fase 8 (opcional, requiere decisión aparte) — Contrato HTTP de errores

`BaseController.ApiError` siempre responde `200 OK` con `Success=false` en el cuerpo, incluso
para errores reales. La mejor práctica REST sería devolver códigos de estado correctos (400/404/
409/500) manteniendo el cuerpo `ActionResponse`. No se incluye como obligatorio en este plan
porque cambia el contrato que el front Vue/Electron ya consume — se deja marcado para decidir
aparte, coordinando el cambio con el cliente.

## Verificación

- `dotnet build Mobile.Remote.Toolkit.sln` sin errores después de cada fase — especialmente
  crítico tras la Fase 4 (creación de Infrastructure + movimiento masivo de archivos).
- Grep final de `Mobile.Remote.Toolkit.Business` en todo el repo (fuera de bin/obj) para
  confirmar que no queda ninguna referencia al namespace/proyecto viejo (ya debería estar limpio
  desde la Fase 2).
- Levantar la API (`dotnet run`) y probar por Swagger o `Mobile.Remote.Toolkit.http`:
  - Endpoints Android existentes (`devices`, `mirror/start`, `mirror/stop`, `screenshot`)
    responden igual que antes.
  - Nuevos endpoints iOS (`devices`, `devices/{udid}/info`, `mirror/start`, `mirror/stop`,
    `action`) responden vía Mediator (sin dispositivo físico deben fallar con un
    `ActionResponse`/error controlado, no crashear).
  - Log de arranque sigue mostrando "Monitoreo de dispositivos auto-iniciado."
- Confirmar que `Domain.csproj` sigue sin ningún `PackageReference` al terminar el plan.

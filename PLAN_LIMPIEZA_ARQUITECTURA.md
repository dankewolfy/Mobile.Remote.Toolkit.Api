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
- ✅ **Fase 3 — Domain con contenido real**: entidad `Device` unificada movida a
  `Domain/Entities/Device.cs` (se borraron los dos duplicados de Application y el
  `Domain/Android/AndroidDevice.cs` viejo). `DeviceEventArgs`/`DeviceStatusChangedEventArgs`
  movidos a `Domain/Events/`. Ajuste no anticipado en el texto original del plan: `DeviceEventArgs`
  usaba `AndroidDeviceResponse` (un DTO de Application) como tipo de su propiedad `Device` — de
  haberlo movido tal cual a Domain se habría creado una dependencia Domain→Application, invirtiendo
  la regla de oro. Se cambió esa propiedad para usar la entidad `Domain.Entities.Device`, y
  `DeviceMonitoringService` ahora mapea `AndroidDeviceResponse` → `Device` al disparar los eventos
  (método privado `ToDomainDevice`). Se agregó `ProjectReference` de Application → Domain (no
  existía). Verificado: `Domain.csproj` sigue sin ningún `PackageReference`. Build de la solución
  sin errores.
- ✅ **Fase 4 — Separar puertos (Application) de adaptadores (Infrastructure)**: creado el
  proyecto `Mobile.Remote.Toolkit.Infrastructure` (net10.0, `ProjectReference` a Application y
  Domain). Movidos a Infrastructure: `AndroidDeviceService`/`MirrorProcessRegistry` →
  `Infrastructure/Android/`, `IOSDeviceService`/`IOSMirrorProcessRegistry` → `Infrastructure/iOS/`,
  `ProcessHelper` → `Infrastructure/Processes/`, `FileService` → `Infrastructure/Files/`,
  `DeviceMonitoringService` → `Infrastructure/Monitoring/`, `LogNotificationService` →
  `Infrastructure/Notifications/`. Cada clase movida cambió de namespace
  (`Application.Services.*`/`Application.Utils` → `Infrastructure.*`) y ahora referencia sus
  puertos vía `using` en vez de compartir namespace. Las interfaces/puertos y DTOs
  (`IAndroidDeviceService`, `IIOSDeviceService`, `IProcessHelper`, `IFileService`,
  `INotificationService`, `IDeviceMonitoringService`, `ProcessResult`, Requests/Responses)
  se quedaron en Application, namespace sin cambios. `System.Management` se movió del
  `.csproj` de Application al de Infrastructure (Application ya no referencia ningún paquete
  atado a SO). `Mobile.Remote.Toolkit.sln` y `Mobile.Remote.Toolkit.Api.csproj` actualizados con
  la referencia a Infrastructure; `Program.cs`/`SignalRNotificationService.cs` actualizados con
  los nuevos `using`. Verificado: `dotnet build` sin errores y `dotnet run` levanta la API,
  detecta un dispositivo Android real conectado end-to-end a través de la nueva capa de
  Infrastructure, y sigue mostrando "Monitoreo de dispositivos auto-iniciado." `Domain.csproj`
  confirmado sin ningún `PackageReference` tras el cambio.
- ✅ **Fase 5 — Composition root limpio** (pendiente de commit): agregado
  `Mobile.Remote.Toolkit.Application/DependencyInjection.cs` con `AddApplication(this
  IServiceCollection services)`, que registra MediatR escaneando únicamente el assembly de
  Application (antes `Program.cs` escaneaba también su propio assembly sin necesidad real: no
  hay ningún `IRequestHandler`/`INotificationHandler` en la capa Api). Agregado
  `Mobile.Remote.Toolkit.Infrastructure/DependencyInjection.cs` con `AddInfrastructure(this
  IServiceCollection services, IConfiguration configuration)`, que registra
  `MirrorProcessRegistry`, `IAndroidDeviceService`, `IOSMirrorProcessRegistry`,
  `IIOSDeviceService`, `IProcessHelper`, `IFileService` e `IDeviceMonitoringService` (todo lo
  que antes estaba disperso en `Program.cs`). `INotificationService`/`SignalRNotificationService`
  se queda registrado en `Program.cs` (capa Api) tal como ya preveía el texto original de la
  Fase 4 — depende de `IHubContext<AndroidDeviceHub>`, un detalle de hosting de ASP.NET.
  `Program.cs` quedó reducido a Logging/CORS/Controllers/Swagger/SignalR (lo genuinamente Api) +
  el registro de `SignalRNotificationService` + `builder.Services.AddApplication();
  builder.Services.AddInfrastructure(builder.Configuration);`. Verificado: `dotnet build` sin
  errores y `dotnet run` levanta la API, detecta el mismo dispositivo Android real end-to-end y
  sigue mostrando "Monitoreo de dispositivos auto-iniciado."
- ✅ **Fase 6 — Simetría estructural Android/iOS**: creados
  `Application/Commands/Base/IOSBaseCommandHandler.cs` y
  `Application/Queries/Base/IOSBaseQueryHandler.cs`, mismo patrón que
  `AndroidBaseCommandHandler`/`AndroidBaseQueryHandler` (inyectan `IIOSDeviceService` +
  `ILogger`/`IMediator` respectivamente). Migrados los 7 handlers de iOS
  (`ExecuteIOSActionCommandHandler`, `StartIOSMirrorCommandHandler`,
  `StopIOSMirrorCommandHandler`, `TakeIOSScreenshotCommandHandler`,
  `GetIOSDeviceInfoQueryHandler`, `GetIOSDevicesQueryHandler`,
  `GetIOSDeviceStatusQueryHandler`) para heredar de estas bases en vez de implementar
  `IRequestHandler<>` directo — cada uno reemplazó su campo privado `_iosService` por el
  miembro protegido heredado (`IOSDeviceService`/`IOSService`). Unificados bajo
  `BaseController` los cuatro controllers que aún extendían `ControllerBase` directamente:
  `FilesController`, `MonitoringController`, `StatsController` y `Controllers/iOS/iOSController.cs`
  (clase `IOSController`, sin tocar su lógica interna — sigue devolviendo los stubs "iOS no
  implementado aún", eso es la Fase 7) — ahora todos comparten `Mediator`/`Logger`/`ApiError`.
  Aprovechado en `FilesController.OpenFolder`: el catch que devolvía `StatusCode(500, new {
  success, message })` ahora usa `ApiError(...)`, igual que ya hacía `AndroidController.ExecuteAction`.
  Limpieza menor: quitado el `using Mobile.Remote.Toolkit.Application.Services.Android;`
  sobrante en el `BaseCommandHandler<>` genérico (no debe conocer Android). El cast
  `(IRequest<ActionResponse>)request` en `AndroidController.ExecuteAdb` no era realmente
  redundante — `ExecuteAdbCommandRequest` implementa tanto `IRequest` (vía `BaseRequest`) como
  `IRequest<ActionResponse>`, y sin el cast `Mediator.Send(request)` resuelve de forma ambigua
  al overload de request sin respuesta (falla en build con `CS0815: Cannot assign void to an
  implicitly-typed variable`); lo único genuinamente redundante era el argumento de tipo
  explícito `Send<ActionResponse>(...)`, ya inferible del cast — se dejó el cast y se quitó solo
  ese argumento de tipo. Verificado: `dotnet build` sin errores; `dotnet run` (puerto alterno,
  sin pisar una instancia ya corriendo en la máquina) levanta la API, detecta el mismo
  dispositivo Android real end-to-end, sigue mostrando "Monitoreo de dispositivos
  auto-iniciado.", y se probaron por HTTP `GET /api/ios/devices`, `GET /api/monitoring/status`
  y `GET /api/Android/devices` — los tres responden 200 a través de la nueva base común.
- ✅ **Fase 7 — Conectar el pipeline de iOS + preparar monitoreo multiplataforma**:
  reescrito `Controllers/iOS/iOSController.cs` para inyectar `IMediator` (vía `BaseController`) y
  despachar los 7 commands/queries ya construidos en Application
  (`GetIOSDevicesQuery`, `GetIOSDeviceInfoQuery`, `GetIOSDeviceStatusQuery`, `StartIOSMirrorCommand`,
  `StopIOSMirrorCommand`, `TakeIOSScreenshotCommand`, `ExecuteIOSActionCommand`), replicando el
  patrón de `AndroidController` — cada endpoint arma su command/query con el `udid` de ruta y el
  cuerpo recibido, y devuelve el resultado de `Mediator.Send(...)` en vez del hardcode "iOS no
  implementado aún". Se agregó el endpoint `GET devices/{udid}/status` (no existía antes en
  `iOSController`) para exponer `GetIOSDeviceStatusQuery`, tal como pide el punto 15 del plan —
  espejo exacto de `AndroidController.GetDeviceStatus`. El endpoint `GET mirror/sessions` se dejó
  sin tocar (stub `{ success: true, sessions: [] }`): no está entre los 7 commands/queries que el
  plan pide cerrar, y aunque `IIOSDeviceService.GetMirrorSessionsAsync()` ya existe, envolverlo
  hubiera requerido crear una Query nueva — eso es "negocio nuevo", fuera del alcance explícito de
  este punto ("no se escribe negocio nuevo, solo se cierra el circuito").
  Bug real encontrado al probar por HTTP (no al leer el código): `IOSActionRequest.Udid` era
  `string` no-nullable: `[ApiController]` de ASP.NET valida implícitamente como requerido cualquier
  propiedad de referencia no-nullable en el body, así que `POST devices/{udid}/action` fallaba con
  400 "The Udid field is required" aunque el controller ignora ese campo del body y usa el `udid`
  de la ruta. `AndroidActionRequest.Serial` (su equivalente en Android) ya es `string?` por esta
  misma razón — se alineó `IOSActionRequest.Udid` a `string?` para que el contrato sea consistente
  con Android y el endpoint funcione. Verificado: `dotnet build` sin errores; `dotnet run` levanta
  la API, detecta el mismo dispositivo Android real end-to-end, sigue mostrando "Monitoreo de
  dispositivos auto-iniciado.", y se probaron por HTTP los 8 endpoints de `/api/ios` sin
  dispositivo físico — todos responden un `ActionResponse`/DTO controlado (nunca un crash ni un
  400 de validación inesperado): `GET devices` → `[]`; `GET devices/{udid}/info` → `IOSDeviceResponse`
  stub; `GET devices/{udid}/status` → dict con `capabilities`; `POST mirror/start` → error
  controlado pidiendo configurar `IOS:Mirror:Executable`; `POST mirror/stop` → éxito informando que
  no hay mirror activo; `POST action` → error controlado "Acción iOS no soportada todavía"; `GET
  screenshot` → error controlado "`idevicescreenshot`... no se encontró el archivo especificado"
  (confirma el punto 16, ver abajo); `GET mirror/sessions` → `{ success: true, sessions: [] }`.

  **Punto 16 — bloqueo de binarios de libimobiledevice, confirmado y documentado**: bajo
  `Tools/iOS/` solo existe el código fuente vendorizado de
  [`libimobiledevice-1.4.0`](https://github.com/libimobiledevice/libimobiledevice) (headers, `.c`,
  autotools) — no hay ningún `.exe`/`.dll` compilado (`find Tools/iOS -iname "*.exe" -o -iname
  "*.dll"` no devuelve nada). El repo del usuario coincide exactamente con el proyecto vendorizado,
  confirmando que la fuente ya está ahí pero sin compilar/instalar. `IOSDeviceService` (Infrastructure)
  ya invoca por nombre las herramientas CLI de ese proyecto vía `IProcessHelper.ExecuteCommandAsync`:
  `idevice_id -l` (listar dispositivos), `ideviceinfo -u {udid} -k {clave}` (info de dispositivo) e
  `idevicescreenshot -u {udid} {ruta}` (captura). A diferencia de adb/scrcpy, `ProcessHelper` NO
  resuelve rutas completas para estos tres nombres — los pasa tal cual al SO, que los busca en PATH
  o en el `WorkingDirectory` (`Tools/`); como no hay binarios compilados en ningún lado, cualquier
  llamada real falla en runtime con "no se encontró el archivo especificado" (confirmado en vivo
  con `idevicescreenshot` arriba). Esto es el bloqueo funcional real de iOS: falta compilar
  `libimobiledevice` (o obtener binarios ya compilados de terceros) e instalarlos en
  `Tools/iOS/<herramienta>/`, análogo a como ya están `Tools/Android/adb` y `Tools/Android/scrcpy`.
  Sigue fuera de alcance de esta limpieza — el circuito de Mediator/Application/Infrastructure ya
  está cerrado end-to-end; lo único que falta es el binario externo, exactamente como preveía este
  punto del plan.

  **Actualización (2026-07-28) — el bloqueo de binarios se resolvió parcialmente, probado con un
  iPad real conectado por USB**: se encontraron binarios de libimobiledevice ya compilados para
  Windows dentro del SDK `Microsoft.iOS.Windows.Sdk` de .NET (usado por MAUI/Xamarin.iOS,
  `C:\Program Files\dotnet\packs\Microsoft.iOS.Windows.Sdk.net9.0_<version>\...\imobiledevice-x64\`)
  — no hizo falta compilar nada del código fuente vendorizado. El bloqueo real no era el binario
  sino el driver USB: Windows tenía el iPad enlazado al driver genérico `usbccgp`, no `WinUSB`;
  se resolvió instalando **"Apple Devices"** desde Microsoft Store (reemplazo moderno de iTunes,
  `winget install --id 9NP83LWLPZ9K --source msstore`) y lanzándola una vez, tras lo cual Windows
  re-enumeró el iPad con una interfaz atada a `WinUSB`. Con `dumpbin /dependents` se identificó el
  set mínimo real de dependencias de `idevice_id.exe`/`ideviceinfo.exe`/`idevicescreenshot.exe`
  (7 DLLs: `getopt`, `imobiledevice`, `plist`, `usbmuxd`, `LIBEAY32`, `SSLEAY32`, `vcruntime140` —
  nada de `libcurl`/`libxml2`/`libusb`, esas son de otras herramientas del pack que no se usan) y
  se copiaron esos 3 `.exe` + 7 `.dll` (~3.1 MB) a `Tools/iOS/libimobiledevice/`, mismo patrón que
  `Tools/Android/adb`/`Tools/Android/scrcpy` (se copian solos al build output vía el
  `<Content Include="..\Tools\**\*">` ya existente en el `.csproj`, sin tocar el `.csproj`).
  `ProcessHelper` ahora resuelve `idevice_id`/`ideviceinfo`/`idevicescreenshot` a esa ruta en
  Windows (antes solo mapeaba `adb`/`scrcpy`); en Linux/macOS esos tres quedan como nombre de
  comando sin resolver, dependiendo de que estén instalados vía el gestor de paquetes del sistema,
  ya que no hay build vendorizado para esos SO. De paso se generalizó la validación de "archivo no
  encontrado": antes chequeaba por lista de nombres (`"adb" or "scrcpy"`), ahora chequea
  `actualFileName != fileName` — se aplica automáticamente a cualquier nombre que sí se resolvió a
  una ruta vendorizada, sin necesidad de mantener la lista de nombres actualizada a mano.
  Verificado con la API real (no solo el CLI suelto) y el iPad conectado: `GET /api/ios/devices`,
  `.../info` y `.../status` devuelven datos reales (`"iPad de Desarrollo"`, `iPad14,1`, iPadOS
  `26.5.2`, `connected: true`) resolviendo los binarios desde `Tools/iOS/libimobiledevice/`, sin
  ningún cambio de `PATH` del sistema. `idevicescreenshot` sigue fallando, pero por un motivo
  *distinto y más específico* al que preveía este punto: pide montar la Developer Disk Image del
  dispositivo (desde iOS 17 son "Personalized DDIs" que requieren Xcode) — no arreglado, sigue
  pendiente. El mirror de video en sí (`StartIOSMirrorCommand`) tampoco se probó con una
  herramienta real todavía — sigue necesitando un ejecutable externo tipo UxPlay o
  pymobiledevice3/IosScreenCaptureTool, ninguno instalado en esta máquina.

  **Punto 17 — `IUsbHardwareWatcher` extraído como puerto**: agregada la interfaz
  `Mobile.Remote.Toolkit.Application/Services/IUsbHardwareWatcher.cs` (`Start(Func<Task>
  onHardwareChanged)` / `Stop()` / `IDisposable`). Dos implementaciones nuevas en
  `Infrastructure/Monitoring/`: `WindowsUsbHardwareWatcher` (la lógica WMI de
  `Win32_DeviceChangeEvent` que antes vivía dentro de `DeviceMonitoringService`, movida tal cual) y
  `PollingUsbHardwareWatcher` (fallback multiplataforma nuevo: `System.Threading.Timer` cada 5s que
  dispara el mismo callback de refresco, para que el monitoreo funcione también fuera de Windows en
  vez de quedar inactivo con solo un `LogWarning` como antes). `DeviceMonitoringService` ya no
  conoce WMI ni `RuntimeInformation`: recibe `IUsbHardwareWatcher` por constructor y solo llama a
  `_usbWatcher.Start(RefreshDeviceListAsync)` / `.Stop()` / `.Dispose()`. La selección de
  implementación (`RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`) se agregó dentro de
  `Infrastructure/DependencyInjection.cs` (`AddInfrastructure`), no en el servicio — igual que pide
  el plan. Verificado en el `dotnet run` de arriba: log `WindowsUsbHardwareWatcher[0] WMI USB
  watcher iniciado` confirma que la resolución de DI y el comportamiento en Windows no cambiaron.

  **Punto 18 — confirmado sin tocar**: `ProcessHelper.ExecuteCommandAsync`/`StartBackgroundProcessAsync`
  mapean explícitamente `"adb"`/`"scrcpy"` a rutas completas bajo `Tools/Android/` según
  `RuntimeInformation.IsOSPlatform` (Windows: `adb.exe`/`scrcpy.exe`; Linux/macOS: `adb`/`scrcpy` sin
  extensión) — confirmado correcto para los tres SO, no se modificó nada de esa clase.

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

### Anexo Fase 7 — Investigación mirror/control real de iOS (2026-07-27)

El punto 16 marcaba el mirror/control de iOS como bloqueo fuera de alcance sin más detalle.
Se investigó el espacio de soluciones (repos de referencia del usuario + búsqueda propia) para
dejar una base concreta de cara a cuando se aborde. Decisión clave: **mirror y control son dos
piezas independientes, no un solo "IOSMirrorService"** — cada una es su propio puerto/adaptador
en Infrastructure, igual que ya lo son `IAndroidDeviceService`/mirror de Android, y pueden
resolverse con proyectos externos distintos sin acoplarse entre sí.

**Mirror (video, sin Mac de por medio) — dos candidatos viables:**
- [`UxPlay`](https://github.com/FDH2/UxPlay) (C/C++, GPLv3, compila nativo en Windows vía
  MSYS2/MinGW): receptor AirPlay real (WiFi), mismo patrón de invocación que scrcpy — binario
  externo spawneado por un registry análogo a `MirrorProcessRegistry`.
- `pymobiledevice3` / [`IosScreenCaptureTool`](https://github.com/BieleckiLtd/IosScreenCaptureTool)
  (MIT, .NET): captura de pantalla por cable USB usando el mismo servicio DVT/CoreMediaIO que usa
  QuickTime en Mac para grabar el iPhone como cámara — no requiere WiFi ni AirPlay, requiere
  Developer Mode en el dispositivo + drivers de Apple Mobile Device en el PC.

**Control (touch real, sin Mac permanente) — candidato recomendado:**
- [`go-ios`](https://github.com/danielpaulus/go-ios) (Go, MIT, binario standalone) +
  `WebDriverAgent`: `go-ios` maneja pairing/devmode/instalación de WDA en el dispositivo desde
  Windows sin ningún macOS en el loop; una vez WDA corre en el iPhone se expone como servidor
  HTTP reenviado por USB (`iproxy`), y se controla con un `HttpClient` normal contra su API REST
  — taps/swipes reales vía XCUITest (el framework de automatización oficial de Apple), no
  emulación de puntero. Única atadura a macOS: firmar el `.ipa` de WDA con Xcode, tarea periódica
  (cada 7 días con Apple ID gratis, cada año con cuenta de desarrollador pagada) y no una máquina
  Mac corriendo en producción.

**Opciones evaluadas y descartadas** (con el motivo, para no re-investigarlas):
- `1PhoneMirror`: sí mirrorea iOS en Windows sin Mac (vía AirPlay), pero es una app GUI completa
  sin hooks de automatización/CLI confirmados — no es una librería para embeber.
- `ios_video_stream`: muerto desde 2020, dependía de un componente on-device que requería
  jailbreak y que Apple bloqueó en updates posteriores.
- `mirroir-mcp` y la propuesta discutida en `pymobiledevice3#1216`: ambos asumen macOS 15+ con
  "iPhone Mirroring" nativo ya corriendo — no aplican a un host Windows sin Mac.
- `Maestro` (mobile-dev-inc): su driver iOS solo soporta simuladores, no dispositivos físicos
  (confirmado en su propio repo, sin plan de agregarlo).
- `idb`/`idb_companion` (Facebook): el companion que habla con el dispositivo requiere macOS
  corriendo de forma permanente (usa frameworks privados de Apple), no es un requisito de una
  sola vez como con WDA.
- Bluetooth HID + AssistiveTouch (técnica de ApowerMirror): viable en teoría (el PC se anuncia
  como mouse Bluetooth y AssistiveTouch traduce los eventos en toques), pero sin ninguna base
  OSS madura para adoptar en Windows, requiere activar Accesibilidad a mano en el dispositivo, y
  da control simulado (puntero) en vez de eventos táctiles reales — inferior a la ruta
  `go-ios`/WDA en casi todo.

Esto sigue sin ser parte de esta limpieza (fuera de alcance por diseño, ver punto 16) — se deja
documentado como insumo para cuando se decida encarar el mirror/control real de iOS.

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

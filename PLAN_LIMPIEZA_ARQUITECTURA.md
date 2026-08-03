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
- ✅ **Fase 5 — Composition root limpio** (commit `d9066d3`): agregado
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
- ✅ **Fase 6 — Simetría estructural Android/iOS** (commit `721323f`): creados
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
- ✅ **Fase 7 — Conectar el pipeline de iOS + preparar monitoreo multiplataforma** (commit `865b430`):
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
- ✅ **Fase 8 — Mirror real de iOS** (cerrada end-to-end el 2026-07-29, verificado con un iPad
  real): dos caminos disponibles según la conectividad del entorno — AirPlay vía UxPlay (código y
  binario listos, pendiente de WiFi compartido con el dispositivo en este entorno puntual) y
  **mirror por USB vía go-ios** (el que efectivamente funciona acá y quedó activo por default,
  reemplazando a `IosScreenCaptureTool` — ver Fase 8c). Ver Fase 8b para el camino original y Fase
  8c para por qué y cómo se reemplazó.
  **Binario**: no hay build oficial de UxPlay para Windows (solo fuente) ni un build de terceros
  utilizable — se probó [`leapbtw/uxplay-windows`](https://github.com/leapbtw/uxplay-windows) (única
  alternativa de terceros encontrada) y se descartó: es una app de bandeja Qt que ignora los
  argumentos de línea de comandos (siempre lee `%APPDATA%\leapbtw\uxplay-windows\arguments.txt`) y
  no abre AirPlay hasta un click manual en su ícono — incompatible con el patrón spawn/kill de
  `IOSMirrorProcessRegistry`. Se optó por compilar UxPlay v1.73.6 desde fuente: se instaló MSYS2
  (`winget install MSYS2.MSYS2`) y el toolchain UCRT64 (`cmake`, `gcc`, `ninja`, `libplist`,
  `gstreamer` + `plugins-base/good/bad`). Hallazgo no anticipado: pacman fallaba con
  `SSL certificate ... self-signed certificate in certificate chain` — la red tiene un proxy de
  inspección TLS (Netskope, CA `certadmin@netskope.com` ya en el almacén de certificados de
  Windows); se exportó ese CA y se agregó al bundle de MSYS2
  (`/etc/pki/ca-trust/source/anchors/`, luego regenerado con `update-ca-trust extract` +
  añadido a mano a `usr/ssl/certs/ca-bundle.crt` porque `update-ca-trust`/`p11-kit` fallaban con
  "field is read-only" en este entorno). Compilado con `cmake -G Ninja -DCMAKE_BUILD_TYPE=Release`
  + `ninja` desde una copia del código en `C:\build\uxplay-src` (no en una ruta profunda de
  scratchpad: CMake avisó que el path original excedía `CMAKE_OBJECT_PATH_MAX`).
  **Audio, hallazgo real solo detectado probando el binario**: `uxplay.exe` no trata el decoder de
  audio ALAC/AAC (`gst-libav`, que envuelve FFmpeg) como opcional — `check_plugins()` en
  `renderers/audio_renderer.c`, llamada desde `main()`, hace `exit(1)` si falta el plugin `libav`,
  aunque el código de decodificación en sí (`avdec_aac`/`avdec_alac`) tiene su propio chequeo blando
  aparte. Sin parchear ese gate no hay forma de arrancar un uxplay.exe "solo video"; parchearlo
  divergía del binario upstream. Se optó por vendorizar `gst-libav` completo en vez de parchear —
  decisión del usuario, sabiendo que arrastra toda la cadena de FFmpeg de MSYS2 (x264, x265, aom,
  dav1d, whisper.cpp, etc., ninguno usado realmente por UxPlay, pero el paquete `ffmpeg` de MSYS2
  no tiene una variante liviana).
  **Vendorizado en `Tools/iOS/mirror/uxplay/`** (mismo patrón que `Tools/Android/scrcpy/`, sin tocar
  el `.csproj`): `uxplay.exe` + el cierre real de dependencias resuelto con `dumpbin /dependents`
  recursivo (mismo método que `libimobiledevice` en la Fase 7), partiendo no solo del `.exe` sino
  también de los plugins de GStreamer que sus pipelines (`gst_parse_launch`) piden por nombre
  (`app`, `playback`, `autodetect`, `videoconvertscale`, `audioconvert`/`resample`,
  `videoparsersbad` para `h264parse`/`h265parse`, `videofilter` para `videoflip`, `volume`, `level`,
  `d3d11`/`d3d12`/`wasapi`/`wasapi2`/`directsound` para hardware-decode y salida en Windows, y
  `libav` para audio) — cada nombre de elemento se resolvió a su plugin real con
  `gst-inspect-1.0.exe <elemento>` en vez de adivinar. Resultado: 126 DLLs en la raíz +
  20 plugins en `gstreamer-1.0/` (subcarpeta nueva), ~173 MB — mucho más que el resto de `Tools/`
  junto, pero bastante menos que los ~267 MB del build de terceros descartado (que traía Qt +
  todo el árbol de GStreamer sin filtrar). Sin este trabajo de cierre de dependencias, con solo el
  `.exe` (que es lo que pedía dumpbin directamente) `uxplay.exe` arrancaba pero fallaba con
  "Required gstreamer plugin 'x' not found" al no encontrar sus plugins en runtime (GStreamer los
  carga dinámicamente, no vía import table — el patrón de Fase 7 con `dumpbin` alcanzaba para
  `libimobiledevice` porque esas herramientas son binarios estáticos sin plugins).
  **Cableado nuevo, no anticipado por el texto original de la Fase 7**: GStreamer no encuentra los
  plugins vendorizados solo con la carpeta al lado del `.exe` — hace falta la variable de entorno
  `GST_PLUGIN_PATH` (y `GST_REGISTRY` para que cachee su propio registro dentro de
  `Tools/iOS/mirror/uxplay/` en vez de `%LOCALAPPDATA%`). `IProcessHelper.StartBackgroundProcessAsync`
  no soportaba pasar variables de entorno al proceso hijo — se agregó un parámetro opcional
  `IDictionary<string, string>? environmentVariables = null` (interfaz en Application, implementación
  en `ProcessHelper` seteando `startInfo.EnvironmentVariables`), y `IOSDeviceService.StartMirrorAsync`
  las lee de una sección nueva `IOS:Mirror:EnvironmentVariables` en configuración — genérico, no
  hardcodea nada de UxPlay (cualquier herramienta de mirror futura puede necesitar variables propias).
  Segundo hallazgo probando por HTTP: `IOS:Mirror:Executable` con una ruta relativa
  (`iOS\mirror\uxplay\uxplay.exe`) fallaba con "system cannot find the file specified" pese a que
  `StartBackgroundProcessAsync` ya seteaba `WorkingDirectory = Tools/` — `Process.Start` de .NET
  busca un nombre de archivo relativo usando el directorio actual del **proceso padre** (la API),
  no el `WorkingDirectory` que se le configura al hijo (ese solo aplica una vez que el proceso ya
  arrancó). Se generalizó `ProcessHelper.StartBackgroundProcessAsync` para combinar cualquier
  `fileName` relativo con `Tools/` antes de lanzar el proceso (antes solo lo hacía para `adb`/`scrcpy`
  vía diccionario hardcodeado); la validación de "herramienta no encontrada" se extendió igual, sin
  romper el caso de rutas absolutas arbitrarias enviadas por el caller (que siguen sin validarse,
  dejando que el SO falle naturalmente, como ya hacía `ExecuteCommandAsync`).
  **`appsettings.json`** configurado con `IOS:Mirror:Executable = iOS\mirror\uxplay\uxplay.exe`,
  `Mode = airplay`, `Arguments = -n Mobile-Remote-Toolkit -nh`, y las dos variables de entorno de
  arriba.
  **Verificado por HTTP** (sin iPad todavía, solo para confirmar el circuito): `POST
  /api/ios/devices/{udid}/mirror/start` devuelve `success: true` con el PID real; `tasklist`
  confirma `uxplay.exe` corriendo; `GET .../status` refleja `mirror_active: true` con el PID y
  modo correctos; `POST mirror/stop` mata el proceso (confirmado con `tasklist` que ya no existe) y
  `status` vuelve a `mirror_active: false`. El resto de la API sigue funcionando igual que en fases
  previas (detecta el Android real, detecta el iPad real conectado por USB vía libimobiledevice).
  **Pendiente real (punto 21) — bloqueado por conectividad de red, confirmado en vivo el 2026-07-29**:
  se intentó la prueba con un iPad mini (iPad14,1, iPadOS 26.5.2) real. Se descubrió que la PC de
  desarrollo no tiene forma de unirse a una red WiFi (`netsh wlan show interfaces` → adaptador
  Intel Wi-Fi 6E AX211 presente pero sin uso posible en este entorno; según el usuario, la máquina
  no tiene adaptador WiFi utilizable) — solo tiene Ethernet cableado a la LAN corporativa. El iPad
  está en la red WiFi de la misma empresa; hay ruteo L3 entre ambas redes (un `ping` desde la PC a la
  IP del iPad responde), pero **AirPlay se descubre por mDNS/Bonjour, que es multicast
  (UDP a 224.0.0.251:5353) y los routers/switches no lo reenvían entre subredes distintas por
  defecto**, aunque haya ruteo unicast normal — confirmado en el propio iPad: el selector de
  "Duplicar pantalla" del Centro de Control solo mostraba "Bocina del iPad" (salida de audio local),
  sin ningún receptor AirPlay en la lista, pese a que `uxplay.exe` estaba corriendo y escuchando
  (PID confirmado con `tasklist`) en el momento del intento.
  **Alternativa evaluada y pospuesta, no descartada**: UxPlay 1.73+ soporta descubrimiento vía
  baliza Bluetooth LE (`uxplay -ble` + `uxplay-beacon.py`, ver Anexo de la Fase 7) pensado
  exactamente para redes que no dejan correr mDNS — el video en sí seguiría viajando por la ruta IP
  ya confirmada (el ping funciona), solo el descubrimiento cambiaría de transporte. Requiere
  Bluetooth 4.0+ en la PC (parece tener adaptador, no confirmado en profundidad), dependencias
  Python/winrt adicionales, y que el iPad esté físicamente cerca (alcance Bluetooth, no WiFi) — se
  decidió no perseguirlo ahora y dejar el circuito de AirPlay como código+vendorizado completos
  (sigue siendo la opción para deployments donde sí haya WiFi compartido), y pivotar a mirror por
  USB — ver Fase 8b.

  ### Fase 8b — Mirror real de iOS por USB vía IosScreenCaptureTool (2026-07-29, funcionando end-to-end)

  Dado que el entorno de esta PC no tiene WiFi utilizable pero sí tiene cable USB siempre
  disponible, se buscó una alternativa que no dependiera de AirPlay. Se encontró
  [`IosScreenCaptureTool`](https://github.com/BieleckiLtd/IosScreenCaptureTool) (BieleckiLtd, MIT,
  .NET): mirror de pantalla de iOS por cable, usando por dentro `pymobiledevice3` para hablar el
  mismo protocolo DVT/CoreMediaIO que usa Xcode/QuickTime — no requiere AirPlay ni WiFi para nada.
  Se compiló desde fuente (no se usó el binario prearmado de terceros, dado que corre con
  privilegios elevados — más control sobre qué corre exactamente) con
  `dotnet publish -r win-x64 --self-contained true`, vendorizado en
  `Tools/iOS/mirror/iosscreencapture/` (~171 MB, incluye el runtime .NET+WPF completo para no
  depender de que la máquina destino tenga el Desktop Runtime instalado — mismo criterio que se usó
  para no depender de gestores de paquetes del sistema en el resto de `Tools/`).

  **Hallazgo estructural, relevante también para la Fase 9**: `IosScreenCaptureTool` se
  autoeleva a Administrador en cada arranque (`WindowsElevation.cs`/`ElevationRelauncher.cs`) porque
  ejecuta `python -m pymobiledevice3 lockdown start-tunnel --script-mode --udid <udid>` — desde
  iOS 17, Apple movió los servicios de developer (screen capture, y lo que va a necesitar
  WebDriverAgent en la Fase 9) detrás de un túnel cifrado "Remote Service Discovery", y crear ese
  túnel implica levantar una interfaz de red virtual, lo cual requiere admin en Windows por diseño
  de Apple/pymobiledevice3 — no es una limitación puntual de esta herramienta. **Cualquier enfoque
  de la Fase 9 que use `pymobiledevice3`/WDA sobre un iPhone/iPad con iOS 17+ muy probablemente
  choque con el mismo requisito.**

  **Decisión de seguridad (el usuario explícitamente rechazó correr toda la API elevada)**: en vez
  de correr `Mobile.Remote.Toolkit.Api` como servicio con privilegios de administrador (soluciona
  todo pero expone toda la superficie de la API con privilegios elevados), se usan **dos Windows
  Scheduled Tasks acotadas**, creadas una sola vez por máquina desde una PowerShell elevada
  (`Tools/iOS/mirror/iosscreencapture/setup-scheduled-tasks.ps1`):
  - `MobileRemoteToolkit_IosMirror_Start`: ejecuta `IosScreenCaptureTool.exe --start-minimized` con
    `RunLevel=Highest`.
  - `MobileRemoteToolkit_IosMirror_Stop`: ejecuta `taskkill /IM IosScreenCaptureTool.exe /F`, mismo
    `RunLevel=Highest`.

  Ninguna tiene disparador automático — la API (corriendo sin privilegios) las dispara con
  `schtasks /run /tn <nombre>` cuando llega `mirror/start`/`mirror/stop`. Windows no pide UAC al
  disparar una tarea ya configurada así (ese es el mecanismo estándar para este problema en
  Windows) — confirmado en vivo, cero prompts interactivos.

  Se evaluó y **descartó** forkear el tool para agregar un comando "exit" a su named pipe existente
  (`IosScreenCaptureTool.CommandPipe.v1`, hoy solo entiende `"capture-frame"` — confirmado leyendo
  `MainWindow.xaml.cs:617`) como alternativa a la segunda Scheduled Task: técnicamente viable, pero
  implica mantener un fork de un proyecto de terceros contra el upstream indefinidamente a cambio de
  ahorrarse una tarea programada — no vale la pena.

  **Blocker real #1 — no es de red esta vez**: al intentar `--capture-frame` contra el proceso ya
  corriendo, devolvía "No frame available yet." sin crashear. La causa: `pip install
  pymobiledevice3` (que el propio tool corre en su primer arranque) había fallado a mitad de camino
  por un archivo cacheado de pip con permisos restrictivos (`PermissionError` en un wheel bajo
  `AppData\Local\pip\cache`), producto de haber corrido `pip` alguna vez con un token elevado —
  se resolvió borrando el directorio de caché de pip completo y reinstalando.
  **Blocker real #2**: `pymobiledevice3` depende de un módulo nativo (`lzfse`) sin wheel
  precompilado para Python 3.14 (la versión que ya estaba instalada en esta máquina, vía
  `msstore`/`winget`) — falla al compilar sin Visual C++ Build Tools bien configurado. El propio
  bootstrapper del tool (`PymobiledeviceBootstrapper.cs`) ya prioriza buscar Python en
  `%LocalAppData%\Programs\Python\Python312\python.exe` antes que en el PATH del sistema — se
  instaló Python 3.12 ahí explícitamente (`winget install --id Python.Python.3.12 --scope user`,
  el mismo paquete que el propio tool intentaría instalar si no encontrara Python en absoluto) y
  `pymobiledevice3` instaló limpio en esa versión (sí tiene wheels precompilados para 3.12).

  **Cambios de código**: además de requerir activar **Modo Desarrollador** una vez en el dispositivo
  (Ajustes → Privacidad y Seguridad → Modo Desarrollador — el propio tool dispara que aparezca esa
  opción en el primer intento de conexión, hay que activarla a mano y reiniciar el iPad una vez),
  se agregó una rama nueva en `IOSDeviceService`: si `IOS:Mirror:Mode = "scheduled-task"`,
  `StartMirrorAsync` no spawnea un proceso directo (como sí hace para UxPlay) sino que dispara
  `IOS:Mirror:StartTaskName` vía `schtasks /run` y luego resuelve el `Process` real buscándolo por
  `IOS:Mirror:ProcessName` (`Process.GetProcessesByName`, reintentando hasta 5 segundos) para
  registrarlo en el mismo `IOSMirrorProcessRegistry` de siempre. `StopMirrorAsync` rama de la misma
  forma: si el modo de la sesión es `scheduled-task`, dispara `IOS:Mirror:StopTaskName` en vez de
  `session.Process.Kill()` (que hubiera fallado — Windows no deja matar un proceso más privilegiado
  que el que lo pide). Bug real encontrado recién al probar por HTTP:
  `IOSMirrorProcessRegistry.Register` hacía `process.EnableRaisingEvents = true` incondicionalmente,
  y esa llamada necesita abrir un handle de espera sobre el proceso que Windows deniega si el
  proceso es más privilegiado que quien lo pide (`Win32Exception: Access is denied`, confirmado en
  el log con el stack trace completo) — se envolvió esa suscripción en un `try/catch` (loguea un
  warning y sigue sin el auto-cleanup-al-salir, pero el tracking por `Id`/`HasExited` sigue andando
  porque esas lecturas solo necesitan `PROCESS_QUERY_LIMITED_INFORMATION`, un permiso que sí cruza
  el límite de privilegio). `appsettings.json` quedó con `IOS:Mirror:Mode = "scheduled-task"` como
  modo activo por defecto (es el que efectivamente funciona en este entorno); las claves de UxPlay
  (`Executable`/`Arguments`/`EnvironmentVariables`) se dejaron configuradas al lado, listas para
  volver a `Mode = "airplay"` el día que haya WiFi compartido entre el host y un dispositivo iOS.

  **Verificado end-to-end con el iPad real conectado por USB**: `POST mirror/start` → dispara la
  Scheduled Task → proceso elevado real aparece (confirmado con `tasklist`) → `GET status` refleja
  `mirror_active: true` con el PID correcto → `--capture-frame` devuelve una imagen real de
  1 MB+ que es la pantalla actual del iPad en ese momento exacto (confirmado visualmente, con hora
  coincidente) → `POST mirror/stop` → dispara la segunda Scheduled Task → proceso muerto
  (confirmado con `tasklist`) → `status` vuelve a `mirror_active: false`. Sin ningún UAC visible en
  todo el flujo.

  ### Fase 8c — Reemplazo de la Fase 8b por go-ios (2026-07-31, superseded)

  Motivo del reemplazo: leyendo el código fuente real de `IosScreenCaptureTool` (clonado a un
  scratchpad para confirmar el protocolo exacto del pipe de comandos) se confirmó que, por dentro,
  **tampoco era un mirror de video real** — `StreamLoopAsync` en `MainWindow.xaml.cs` ejecuta
  `python -m pymobiledevice3 developer dvt screenshot --rsd <host> <port> <archivo>` en un loop, es
  decir un proceso Python nuevo por cada frame sobre el servicio DVT de captura de pantalla de
  Apple, no un feed de video continuo (nada de H.264). El "mirror real" que se pensaba haber
  logrado en la Fase 8b en realidad seguía siendo capturas repetidas, solo que automatizadas.

  Se investigó `danielpaulus/quicktime_video_hack` (el reverse-engineering del protocolo real que
  usa QuickTime en Mac) como alternativa — **descartado**: nunca tuvo soporte Windows (issue
  abierto sin respuesta desde 2022), roto en iOS 18, y necesitaría vendorizar GStreamer igual que
  UxPlay.

  En su lugar se adoptó **`go-ios`** (`danielpaulus/go-ios`, MIT, binario Go standalone, ya
  contemplado para la Fase 9 de control táctil), que trae mirror integrado:
  - `ios screenshot --stream --port=<puerto>` levanta un servidor HTTP MJPEG nativo — consumible
    directo por un `<img src="http://host:puerto/">` desde cualquier navegador, no solo por una
    ventana nativa dockeada en Electron. Por dentro llama al mismo servicio DTX de screenshot que
    usaba `IosScreenCaptureTool`, así que el techo de fps es similar (~3-8 fps medidos en vivo con
    el iPad de prueba), pero sin el bootstrap de Python/pip y sin la ventana WPF.
  - `ios tunnel start --userspace` **no pide privilegios de administrador** — confirmado en vivo
    (túnel negociado sin ningún prompt de UAC) — a diferencia del túnel "Remote Service Discovery"
    de `pymobiledevice3`, que sí lo exigía y fue la razón de ser de las dos Scheduled Tasks
    elevadas de la Fase 8b.

  Verificado end-to-end contra la API real (no solo el CLI suelto) con el iPad conectado:
  `POST mirror/start` → arranca el túnel compartido (una vez, vía `GoIosTunnelManager`) + el
  proceso `ios screenshot --stream` (sin elevación) → `GET status` refleja `mirror_active: true` y
  `mirror_url` → un `GET` HTTP normal contra esa URL devuelve multipart MJPEG con frames JPEG reales
  → `POST mirror/stop` mata el proceso directo (`Process.Kill()`, ya no hace falta ninguna
  Scheduled Task para pararlo, porque el proceso no corre elevado).

  Limpieza asociada: se eliminó `Tools/iOS/mirror/iosscreencapture/` (vendor de
  `IosScreenCaptureTool`, ~173 MB) y `IOS:Mirror:StartTaskName`/`StopTaskName`/`ProcessName` de
  `appsettings.json`. Las dos Scheduled Tasks (`MobileRemoteToolkit_IosMirror_Start`/`_Stop`)
  quedan pendientes de borrar en esta máquina — requiere una PowerShell elevada
  (`schtasks /delete /tn "MobileRemoteToolkit_IosMirror_Start" /f`, ídem `_Stop`), no se pudo hacer
  desde esta sesión sin privilegios. El código de docking de Electron
  (`scrcpy-manager-vue/electron/main/windowManager.ts`, busca una ventana titulada "iOS Screen
  Capture Tool") quedó como deuda pendiente — `go-ios` no abre ninguna ventana, así que ese código
  ya no tiene efecto y habría que reemplazarlo por una vista que consuma `mirror_url` en vez de
  dockear una ventana nativa; no se tocó en esta sesión.

- ⬜ **Fase 9 — Control táctil + mirror h264 real vía go-ios/DeviceKit** (investigada y confirmada
  viable a mano contra hardware real el 2026-08-03; falta la integración en código — ver detalle
  completo más abajo, reemplaza por completo el esbozo especulativo original de esta fase).
  Resumen: **sí se puede**, sin Mac corriendo en producción, con cuenta de Apple Developer paga;
  requiere un paso manual **una sola vez por dispositivo** (parear con una Mac+Xcode reales) que no
  se puede evitar con las herramientas actuales.

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

## Fase 8 — Mirror real de iOS vía UxPlay (video, sin Mac)

Objetivo: que `POST /api/ios/devices/{udid}/mirror/start` levante video real de AirPlay, no un
error de configuración. **No se escribe lógica de negocio nueva** — `StartIOSMirrorCommand` /
`IOSDeviceService.StartMirrorAsync` ya son agnósticos de herramienta (spawnean cualquier
ejecutable configurado, ver `Mobile.Remote.Toolkit.Infrastructure/iOS/IOSDeviceService.cs:140-223`)
desde la Fase 7; el trabajo de esta fase es 100% obtener el binario externo, probarlo manualmente,
y configurarlo — igual patrón que ya existe para scrcpy con Android.

19. Compilar [`UxPlay`](https://github.com/FDH2/UxPlay) para Windows (su propio build doc indica
    MSYS2/MinGW) o conseguir un release ya compilado si el proyecto publica uno para Windows.
20. Vendorizar el resultado en `Tools/iOS/mirror/uxplay/` (mismo patrón que
    `Tools/Android/scrcpy/`) — el `.csproj` de Api ya copia todo `Tools/**/*` al output, no hace
    falta tocarlo (confirmado en Fase 7 con `Tools/iOS/libimobiledevice/`).
21. **Probar manualmente, fuera de esta API**, que el iPad puede iniciar "Screen Mirroring"/AirPlay
    hacia esta PC en la misma red WiFi y que UxPlay efectivamente muestra video — recién ahí tiene
    sentido integrarlo al backend. Si esto no funciona a mano, no tiene caso wirearlo.
22. Configurar `appsettings.json` (`IOS:Mirror:Executable`, `IOS:Mirror:Mode=airplay`,
    `IOS:Mirror:Arguments`) apuntando al binario vendorizado, para que
    `POST mirror/start` funcione sin tener que mandar `options.executable` en cada request.
23. Verificar por HTTP: `POST /api/ios/devices/{udid}/mirror/start` sin body levanta UxPlay
    (`IOSMirrorProcessRegistry` lo registra, igual que ya hace con Android); `POST mirror/stop` lo
    mata correctamente; `GET .../status` refleja `mirror_active: true` con el PID real mientras
    corre.
24. Nota para una fase de cliente (fuera de este repo de API): UxPlay abre una ventana nativa igual
    que scrcpy — el Electron actual ya detecta la ventana de mirror de Android por título; si se
    quiere el mismo comportamiento para iOS habrá que enseñarle a reconocer también la ventana de
    UxPlay. Anotado como pendiente del lado del cliente, no se resuelve en este repo.

## Fase 9 — Control táctil + mirror h264 real vía go-ios/DeviceKit

Objetivo: reemplazar el stub actual (`capabilities.touch` siempre `false`,
ver `IOSDeviceService.GetDeviceStatusAsync`) por control táctil real, y de paso reemplazar el
mirror MJPEG actual (Fase 8c, ~3-8 fps porque por dentro sigue siendo el servicio de screenshot de
Apple) por video h264 real de hardware. A diferencia de la Fase 8, esta sí requiere escribir código
nuevo — hoy no existe ningún puerto/adapter de control, solo el mirror y las queries de info/estado.

### Qué se confirmó a mano contra un iPad real (2026-08-03) — ya no es especulativo

`go-ios` (el mismo binario ya vendorizado en `Tools/iOS/go-ios/ios.exe` para el mirror de la
Fase 8c) trae un segundo backend además de WDA: **DeviceKit** (`--driver=devicekit`, default).
A diferencia de WDA (limitado a MJPEG, mismo techo que el mirror actual), DeviceKit expone:

- `GET http://127.0.0.1:12004/h264?fps=30&quality=80` → **h264 real de hardware**, confirmado
  inspeccionando los bytes crudos: unidades NAL con start code `00 00 00 01` seguidas de SPS
  (`27`), PPS (`28`), SEI (`06`) e IDR (`05`) — la estructura real de un stream H.264, no capturas
  de pantalla repetidas. ~730 KB en 5 s (~1.15 Mbps) a fps=30/quality=80 contra un iPad real.
- `POST http://127.0.0.1:12004/rpc` (JSON-RPC 2.0) con métodos `device.io.tap`/`.swipe`/
  `.longpress`/`.text`/`.button`/`.orientation.*`, `device.apps.launch/terminate/foreground`,
  `device.screenshot`, `device.info`, `device.dump.ui` — **táctil real confirmado**: un
  `ios ui tap --x=<x> --y=<y>` movió algo en la pantalla del iPad de prueba.
- CLI cliente ya integrado en el mismo binario: `ios ui tap/swipe/stream/...` (ver
  `ios help ui <comando>`), no hace falta hablar el protocolo JSON-RPC a mano si no se quiere.

### La receta completa que funcionó (para no tener que redescubrirla)

1. **Cuenta de Apple Developer Program de pago** (no alcanza un Apple ID gratis — confirmado,
   `ios/signing/appstoreconnect.go` no tiene fallback). Generar una API Key en
   appstoreconnect.apple.com → Users and Access → Integrations → App Store Connect API (rol Admin),
   descargar el `.p8` (solo se puede una vez), anotar Key ID e Issuer ID.
2. **Descargar los artefactos a mano, no con `ios ui download`**: el CDN de terceros
   (`deviceboxhq.com`) devuelve `403 Forbidden` al User-Agent default de Go. Workaround:
   ```
   curl -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36" \
     -o devicekit.ipa https://deviceboxhq.com/devicekit-ios-runner-0.0.18.ipa
   ```
   (mismo truco si además se quiere WDA, aunque no es necesario para esta fase — ver nota al final
   sobre el bug de empaquetado que tiene ese camino).
3. **Certificado + perfil de aprovisionamiento**, con un `--bundleid` propio (los defaults de
   go-ios, `com.deviceboxhq.goios.*`, ya están registrados por el proveedor del CDN → `409
   Conflict: ... is not available`):
   ```
   ios sign provision appstoreconnect --bundleid=com.<tuorg>.devicekit \
     --asc-key-id=<ASC_KEY_ID> --asc-issuer-id=<ASC_ISSUER_ID> --asc-private-key=<ruta al .p8> \
     --p12-output=devicekit.p12 --profile-output=devicekit.mobileprovision \
     --device-name="<nombre del dispositivo>"
   ```
   **Apple solo permite un certificado `IOS_DEVELOPMENT` activo a la vez** — si se repite este
   comando para otro bundle id (p.ej. si más adelante también se quiere WDA) va a fallar con
   `409 Conflict: You already have a current iOS Development certificate`. Solución: reusar el
   mismo certificado con `--certificate-id=<id>` (el `certificateID` que imprime el comando
   anterior) — ese modo no crea un `.p12` nuevo, se reusa el que ya existe.
4. **Instalar**: `ios ui install devicekit --path=devicekit.ipa --p12file=devicekit.p12 --profile=devicekit.mobileprovision --bundleid=com.<tuorg>.devicekit --output=devicekit-signed.ipa`.
5. **Túnel** (igual que el mirror de la Fase 8c, sin elevación): `ios tunnel start --userspace`.
6. **Bloqueo real, no evitable con las herramientas actuales**: lanzar DeviceKit vía
   `ios ui run devicekit --bundleid=com.<tuorg>.devicekit` fallaba siempre igual —
   `"Timed out waiting for response for message:5 channel:0"` /
   `"An established connection was aborted by the software in your host machine"` — probado 6
   veces: con túnel fresco, sin el mirror MJPEG corriendo en simultáneo, con la pantalla
   desbloqueada confirmada, y después de un reboot completo del iPad. Mismo error, palabra por
   palabra, reportado en
   [go-ios#431](https://github.com/danielpaulus/go-ios/issues/431) — un colaborador ahí confirma
   que no es un problema de instalación sino del handshake del protocolo de testing de Apple, y que
   se resuelve **conectando el dispositivo una vez a una Mac real con Xcode** (abrir Xcode, Window →
   Devices and Simulators, esperar que termine de "preparar" el dispositivo, y opcionalmente correr
   cualquier app de prueba una vez). **Confirmado en este proyecto**: después de ese paso único, el
   mismo comando (`ios ui run devicekit`) funcionó de inmediato desde Windows, sin volver a tocar la
   Mac. go-ios tiene un issue abierto sin resolver preguntando por soporte de iOS 26+
   ([go-ios#603](https://github.com/danielpaulus/go-ios/issues/603)) — el iPad de prueba corre iOS
   27, así que es plausible que este handshake sea más frágil en versiones muy nuevas de iOS.
7. Una vez corriendo (`ios ui run devicekit` queda en foreground, reenvía el puerto 12004), usar
   `ios ui tap/swipe/stream h264/...` o pegarle directo al JSON-RPC en `127.0.0.1:12004/rpc`.

**Nota sobre WDA**: se probó instalar WDA además de DeviceKit (reusando el mismo certificado) para
descartar si el bloqueo del punto 6 era específico de DeviceKit. La instalación de WDA (artefacto
`.zip`, a diferencia del `.ipa` de DeviceKit) falló con un bug distinto y no relacionado de
empaquetado en `go-ios` (`"Failed to get bundle ID from .../wda-signed.ipa.ipa"`, doble extensión
`.ipa.ipa`) — no se investigó más porque WDA de todas formas está limitado a MJPEG (mismo techo que
ya tenemos) y no aporta nada sobre DeviceKit. No hace falta WDA para esta fase.

### Lo que falta para integrarlo en el código (no hecho todavía, es el plan a seguir)

25. Vendorizar `Tools/iOS/go-ios/` ya está hecho (Fase 8c) — no hay que agregar un binario nuevo,
    `ios.exe` ya sabe hacer todo esto.
26. Decidir dónde vive el manejo de credenciales de App Store Connect (Key ID/Issuer ID/`.p8`) —
    **no comitear el `.p8` ni el `.p12` al repo** (son credenciales reales, aunque de una cuenta de
    desarrollo). Evaluar `dotnet user-secrets`, variable de entorno, o un archivo fuera del repo
    referenciado por ruta en `appsettings.Local.json` (gitignored). El `.mobileprovision` (sin
    clave privada) sí se podría vendorizar si se quiere evitar volver a llamar a la API de Apple
    cada vez, pero no es necesario — crear uno nuevo tarda segundos.
27. Nuevo `GoIosDeviceKitManager` (mismo patrón que `GoIosTunnelManager` de la Fase 8c): mantiene
    viva la sesión `ios ui run devicekit` (proceso de fondo único, reenvía el puerto 12004) en vez
    de re-lanzarla en cada request.
28. Nuevo modo de mirror (p.ej. `Mode = "go-ios-devicekit"`) que en vez de devolver `mirror_url`
    (MJPEG, consumible con un `<img>` simple) devuelva la URL del stream h264
    (`http://localhost:12004/h264?...`) — **el frontend no puede mostrar esto con un `<img>` como
    hace hoy con el MJPEG**: h264 Annex-B crudo necesita un decoder (`VideoDecoder` de WebCodecs
    hacia un `<canvas>`, o muxearlo a fragmented MP4 al vuelo para usar Media Source Extensions +
    `<video>`) — esto es trabajo de frontend nuevo, no una extensión trivial de
    `IOSControls.vue`/`mirrorWindow.ts`.
29. Nuevo puerto en Application: `IIOSControlService` (o extender `IIOSDeviceService` — evaluar en
    el momento, replicando la separación mirror/control ya decidida en el Anexo de la Fase 7) con
    métodos tipo `TapAsync(udid, x, y)` / `SwipeAsync(udid, from, to)`. Implementación en
    Infrastructure: llamar directo al JSON-RPC de DeviceKit por HTTP (`127.0.0.1:12004/rpc`) es más
    barato que invocar `ios.exe ui tap` como subproceso por cada toque (ida y vuelta de proceso vs.
    una request HTTP local).
30. Decidir si esto se expone reusando `ExecuteIOSActionCommand` (ya tiene un switch por `action:
    "tap"/"swipe"` preparado en `IOSDeviceService.ExecuteActionAsync`, hoy cae al `default` "no
    soportada todavía") o con commands nuevos dedicados — replicar el patrón que ya use
    `StartMirrorCommand`/`ExecuteAndroidActionCommand` para no introducir un estilo distinto.
31. Actualizar `GetIOSDeviceStatusQuery`/`GetDeviceStatusAsync` para que `capabilities.touch` pase
    a `true` una vez el control esté realmente disponible para ese dispositivo.
32. Documentar como requisito operativo (no de código): el paso 6 de la receta (parear con una Mac
    real) hay que rehacerlo si el registro de pairing del dispositivo se invalida (reset de red/
    privacidad en el iPad, o un dispositivo nuevo) — no es un setup de una sola vez para siempre,
    es una-sola-vez-por-dispositivo-hasta-que-se-despaire.

## Fase 10 (opcional, requiere decisión aparte) — Contrato HTTP de errores

A diferencia de las Fases 8 y 9, esta no es una continuación del hilo de iOS — es un tema
transversal que afecta a todos los controllers (Android, iOS, Files, Monitoring, Stats) por igual,
por eso queda al final en vez de intercalada entre las fases de iOS.

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
- **Fase 8**: `mirror/start` contra un iPad real conectado (o en la misma WiFi para AirPlay)
  levanta video real vía UxPlay, no el error de configuración actual; `mirror/stop` lo corta;
  `GET .../status` refleja `mirror_active`/PID mientras corre.
- **Fase 9**: un `tap`/`swipe` real vía `ExecuteIOSActionCommand` (o el command dedicado que se
  elija) mueve algo en la pantalla del dispositivo; `capabilities.touch` pasa a `true` solo cuando
  DeviceKit está efectivamente corriendo para ese udid, no de forma global; el nuevo endpoint de
  mirror h264 se ve fluido en el frontend (no a los ~3-8 fps del MJPEG actual) una vez resuelto el
  decoder del lado del cliente.

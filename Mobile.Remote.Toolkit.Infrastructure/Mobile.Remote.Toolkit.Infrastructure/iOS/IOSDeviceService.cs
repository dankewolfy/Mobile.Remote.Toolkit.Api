using System.Diagnostics;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Services;
using Mobile.Remote.Toolkit.Application.Services.iOS;
using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Application.Utils;

namespace Mobile.Remote.Toolkit.Infrastructure.iOS
{
    public class IOSDeviceService : IIOSDeviceService
    {
        private readonly IOSMirrorProcessRegistry _mirrorRegistry;
        private readonly GoIosTunnelManager _tunnelManager;
        private readonly GoIosDeviceKitManager _deviceKitManager;
        private readonly IIOSControlService _controlService;
        private readonly IProcessHelper _processHelper;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IOSDeviceService> _logger;

        public IOSDeviceService(
            IOSMirrorProcessRegistry mirrorRegistry,
            GoIosTunnelManager tunnelManager,
            GoIosDeviceKitManager deviceKitManager,
            IIOSControlService controlService,
            IProcessHelper processHelper,
            INotificationService notificationService,
            IConfiguration configuration,
            ILogger<IOSDeviceService> logger)
        {
            _mirrorRegistry = mirrorRegistry;
            _tunnelManager = tunnelManager;
            _deviceKitManager = deviceKitManager;
            _controlService = controlService;
            _processHelper = processHelper;
            _notificationService = notificationService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<IOSDeviceResponse>> GetConnectedDevicesAsync()
        {
            var result = await _processHelper.ExecuteCommandAsync("idevice_id", "-l");

            if (!result.Success)
            {
                _logger.LogWarning("No se pudieron listar dispositivos iOS: {Error}", result.Error);
                return new List<IOSDeviceResponse>();
            }

            var udids = result.Output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var deviceTasks = udids.Select(GetDeviceInfoAsync);
            var devices = await Task.WhenAll(deviceTasks);

            return devices.Where(d => d != null).ToList();
        }

        public async Task<IOSDeviceResponse> GetDeviceInfoAsync(string udid)
        {
            try
            {
                var nameTask = GetDeviceValueAsync(udid, "DeviceName");
                var productTypeTask = GetDeviceValueAsync(udid, "ProductType");
                var productVersionTask = GetDeviceValueAsync(udid, "ProductVersion");
                var serialNumberTask = GetDeviceValueAsync(udid, "SerialNumber");

                await Task.WhenAll(nameTask, productTypeTask, productVersionTask, serialNumberTask);

                var productType = productTypeTask.Result;

                return new IOSDeviceResponse
                {
                    Id = udid,
                    Udid = udid,
                    Name = string.IsNullOrWhiteSpace(nameTask.Result) ? $"iOS {LastChars(udid, 4)}" : nameTask.Result,
                    Model = productType,
                    ProductType = productType,
                    IOSVersion = productVersionTask.Result,
                    SerialNumber = serialNumberTask.Result,
                    Platform = "ios",
                    Active = _mirrorRegistry.IsActive(udid)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo info del dispositivo iOS {Udid}", udid);
                return null;
            }
        }

        public async Task<Dictionary<string, object>> GetDeviceStatusAsync(string udid)
        {
            var connectedDevices = await GetConnectedDevicesAsync();
            var isConnected = connectedDevices.Any(d => d.Udid.Equals(udid, StringComparison.OrdinalIgnoreCase));
            var session = _mirrorRegistry.GetAlive(udid);
            var touchAvailable = await _controlService.IsAvailableAsync(udid);

            var status = new Dictionary<string, object>
            {
                ["connected"] = isConnected,
                ["mirror_active"] = session != null,
                ["udid"] = udid,
                ["platform"] = "ios",
                ["timestamp"] = DateTime.UtcNow,
                ["capabilities"] = new Dictionary<string, object>
                {
                    ["screenshot"] = true,
                    ["mirror"] = true,
                    ["touch"] = touchAvailable,
                    ["touch_note"] = touchAvailable
                        ? "Control tactil real via DeviceKit (go-ios)."
                        : "Requiere mirror con mode=go-ios-devicekit corriendo para este dispositivo (DeviceKit instalado y firmado, ver appsettings IOS:DeviceKit)."
                }
            };

            if (session != null)
            {
                status["process_id"] = session.Process.Id;
                status["mirror_mode"] = session.Mode;
                status["mirror_executable"] = session.Executable;

                if (session.Port.HasValue)
                    status["mirror_url"] = $"http://localhost:{session.Port.Value}{session.StreamPath}";
            }

            return status;
        }

        public async Task<ActionResponse> ExecuteActionAsync(string udid, string action, Dictionary<string, object> options, Dictionary<string, object> payload)
        {
            return action?.ToLowerInvariant() switch
            {
                "start_mirror" => await StartMirrorAsync(udid, options ?? payload),
                "stop_mirror" => await StopMirrorAsync(udid),
                "screenshot" => await TakeScreenshotAsync(udid, payload?.GetValueOrDefault("filename")?.ToString()),
                "tap" => await _controlService.TapAsync(udid, GetDouble(payload, "x"), GetDouble(payload, "y")),
                "swipe" => await ExecuteSwipeAsync(udid, payload),
                "long_press" => await _controlService.LongPressAsync(udid, GetDouble(payload, "x"), GetDouble(payload, "y"), GetInt(payload, "duration_ms")),
                "type_text" => await _controlService.TypeTextAsync(udid, GetOption(NormalizeOptions(payload), "text") ?? string.Empty),
                "button" => await _controlService.PressButtonAsync(udid, GetOption(NormalizeOptions(payload), "name") ?? string.Empty),
                _ => new ActionResponse
                {
                    Success = false,
                    Message = "Accion iOS no soportada todavia",
                    Error = "Acciones soportadas: start_mirror, stop_mirror, screenshot, tap, swipe, long_press, type_text, button."
                }
            };
        }

        public Task<bool> IsMirrorActiveAsync(string udid)
            => Task.FromResult(_mirrorRegistry.IsActive(udid));

        public async Task<ActionResponse> StartMirrorAsync(string udid, Dictionary<string, object> options)
        {
            try
            {
                if (_mirrorRegistry.IsActive(udid))
                {
                    var active = _mirrorRegistry.GetAlive(udid);
                    return new ActionResponse
                    {
                        Success = false,
                        Message = "Ya hay un mirror activo para este dispositivo iOS",
                        Error = "Mirror already running",
                        Data = active?.ToResponse().ToDictionary()
                    };
                }

                var opts = NormalizeOptions(options);
                var mode = GetOption(opts, "mode") ?? _configuration["IOS:Mirror:Mode"] ?? "external";

                if (string.Equals(mode, "go-ios", StringComparison.OrdinalIgnoreCase))
                    return await StartMirrorViaGoIosAsync(udid, mode);

                if (string.Equals(mode, "go-ios-devicekit", StringComparison.OrdinalIgnoreCase))
                    return await StartMirrorViaDeviceKitAsync(udid, mode);

                var executable = GetOption(opts, "executable")
                    ?? GetOption(opts, "toolPath")
                    ?? _configuration["IOS:Mirror:Executable"];

                if (string.IsNullOrWhiteSpace(executable))
                {
                    return new ActionResponse
                    {
                        Success = false,
                        Message = "Mirror iOS requiere configurar una herramienta externa de visualizacion",
                        Error = "Configure IOS:Mirror:Executable en appsettings o envie options.executable/options.toolPath.",
                        Data = new Dictionary<string, object>
                        {
                            ["expected_options"] = new
                            {
                                executable = "Ruta o nombre del ejecutable de mirror/AirPlay",
                                arguments = "Argumentos opcionales. Puede usar {udid}.",
                                mode = "external|airplay|usb"
                            },
                            ["example"] = new
                            {
                                executable = "Tools\\iOS\\mirror\\ios-mirror.exe",
                                arguments = "--udid {udid}"
                            }
                        }
                    };
                }

                var argumentsTemplate = GetOption(opts, "arguments")
                    ?? _configuration["IOS:Mirror:Arguments"]
                    ?? "-u {udid}";
                var arguments = argumentsTemplate.Replace("{udid}", udid, StringComparison.OrdinalIgnoreCase);

                var environmentVariables = _configuration.GetSection("IOS:Mirror:EnvironmentVariables")
                    .GetChildren()
                    .ToDictionary(section => section.Key, section => section.Value, StringComparer.OrdinalIgnoreCase);

                _logger.LogInformation("[iOS Mirror] Iniciando {Executable} {Arguments}", executable, arguments);

                var process = await _processHelper.StartBackgroundProcessAsync(executable, arguments, environmentVariables);
                _mirrorRegistry.Register(udid, process, mode, executable, arguments);

                await _notificationService.NotifyMirrorStarted(udid);

                return new ActionResponse
                {
                    Success = true,
                    Message = "Mirror iOS iniciado correctamente",
                    Data = new Dictionary<string, object>
                    {
                        ["udid"] = udid,
                        ["mode"] = mode,
                        ["executable"] = executable,
                        ["arguments"] = arguments,
                        ["pid"] = process.Id,
                        ["touch_supported"] = false
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error iniciando mirror iOS para {Udid}", udid);
                return new ActionResponse
                {
                    Success = false,
                    Message = $"Error iniciando mirror iOS: {ex.Message}",
                    Error = ex.Message
                };
            }
        }

        public async Task<ActionResponse> StopMirrorAsync(string udid)
        {
            try
            {
                var session = _mirrorRegistry.Remove(udid);

                if (session == null)
                    return new ActionResponse { Success = true, Message = "No hay mirror iOS activo para este dispositivo" };

                // A diferencia del viejo mirror por IosScreenCaptureTool (corria elevado via Scheduled
                // Task), tanto "ios screenshot --stream" como "ios ui run devicekit" de go-ios corren
                // sin privilegios especiales - se pueden matar directo, sin ningun mecanismo de
                // Scheduled Task de por medio.
                if (string.Equals(session.Mode, "go-ios-devicekit", StringComparison.OrdinalIgnoreCase))
                {
                    // DeviceKit sirve mirror h264 y control tactil desde el mismo proceso - pararlo
                    // tambien deja sin efecto el control tactil para este udid (capabilities.touch
                    // vuelve a false en la proxima consulta de estado).
                    _deviceKitManager.StopIfRunningFor(udid);
                }
                else if (!session.Process.HasExited)
                {
                    session.Process.Kill();
                }

                session.Process.Dispose();

                await _notificationService.NotifyMirrorStopped(udid);

                return new ActionResponse { Success = true, Message = "Mirror iOS detenido correctamente" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deteniendo mirror iOS para {Udid}", udid);
                return new ActionResponse
                {
                    Success = false,
                    Message = $"Error deteniendo mirror iOS: {ex.Message}",
                    Error = ex.Message
                };
            }
        }

        private async Task<ActionResponse> StartMirrorViaGoIosAsync(string udid, string mode)
        {
            // go-ios reemplaza al viejo stack IosScreenCaptureTool/pymobiledevice3: "ios screenshot
            // --stream" sirve un stream MJPEG real por HTTP (consumible por cualquier navegador con
            // un <img src="...">, no solo por una ventana nativa dockeada en Electron), y "tunnel start
            // --userspace" no pide privilegios de administrador en Windows - confirmado en vivo con un
            // iPad real. Por eso no hace falta ningun mecanismo de Scheduled Task elevada aca.
            var executable = _configuration["IOS:Mirror:GoIosExecutable"];
            if (string.IsNullOrWhiteSpace(executable))
            {
                return new ActionResponse
                {
                    Success = false,
                    Message = "Mirror iOS via go-ios requiere configuracion",
                    Error = "Configure IOS:Mirror:GoIosExecutable en appsettings."
                };
            }

            if (!int.TryParse(_configuration["IOS:Mirror:Port"], out var port))
                port = 3333;

            await _tunnelManager.EnsureRunningAsync(_processHelper, executable);

            var arguments = $"screenshot --stream --port={port} --udid={udid}";
            var process = await _processHelper.StartBackgroundProcessAsync(executable, arguments);

            _mirrorRegistry.Register(udid, process, mode, executable, arguments, port);
            await _notificationService.NotifyMirrorStarted(udid);

            return new ActionResponse
            {
                Success = true,
                Message = "Mirror iOS iniciado correctamente (via go-ios)",
                Data = new Dictionary<string, object>
                {
                    ["udid"] = udid,
                    ["mode"] = mode,
                    ["executable"] = executable,
                    ["pid"] = process.Id,
                    ["port"] = port,
                    ["mirror_url"] = $"http://localhost:{port}/",
                    ["touch_supported"] = false
                }
            };
        }

        private async Task<ActionResponse> StartMirrorViaDeviceKitAsync(string udid, string mode)
        {
            // DeviceKit ("ios ui run devicekit", backend --driver=devicekit de go-ios) sirve h264
            // real de hardware y el JSON-RPC de control tactil desde el mismo proceso/puerto - por
            // eso arrancar el mirror en este modo tambien deja listo el control (tap/swipe/etc.)
            // para el mismo udid, a diferencia del mirror MJPEG (go-ios "screenshot --stream"), que
            // no expone ningun control.
            var executable = _configuration["IOS:DeviceKit:Executable"] ?? _configuration["IOS:Mirror:GoIosExecutable"];
            var bundleId = _configuration["IOS:DeviceKit:BundleId"];

            if (string.IsNullOrWhiteSpace(executable) || string.IsNullOrWhiteSpace(bundleId))
            {
                return new ActionResponse
                {
                    Success = false,
                    Message = "Mirror iOS via DeviceKit requiere configuracion",
                    Error = "Configure IOS:DeviceKit:Executable e IOS:DeviceKit:BundleId en appsettings (requiere DeviceKit instalado y firmado en el dispositivo, ver receta de la Fase 9)."
                };
            }

            if (!int.TryParse(_configuration["IOS:DeviceKit:Port"], out var port))
                port = 12004;

            var fps = _configuration["IOS:DeviceKit:Fps"] ?? "30";
            var quality = _configuration["IOS:DeviceKit:Quality"] ?? "80";

            await _tunnelManager.EnsureRunningAsync(_processHelper, executable);
            var process = await _deviceKitManager.EnsureRunningAsync(_processHelper, executable, bundleId, udid);

            if (!await WaitForDeviceKitReadyAsync(udid, process))
            {
                _deviceKitManager.StopIfRunningFor(udid);
                return new ActionResponse
                {
                    Success = false,
                    Message = "DeviceKit no respondio a tiempo",
                    Error = "El proceso 'ios ui run devicekit' se lanzo pero no respondio via RPC tras varios intentos. " +
                        "Puede ser el handshake de testing de Apple fallando (ver receta Fase 9, punto 6): probar " +
                        "reconectar el cable USB, desbloquear la pantalla del dispositivo, o re-parear con una Mac real " +
                        "con Xcode si el problema persiste."
                };
            }

            var streamPath = $"/h264?fps={fps}&quality={quality}";
            var arguments = $"ui run devicekit --bundleid={bundleId} --udid={udid}";
            _mirrorRegistry.Register(udid, process, mode, executable, arguments, port, streamPath);

            await _notificationService.NotifyMirrorStarted(udid);

            return new ActionResponse
            {
                Success = true,
                Message = "Mirror iOS iniciado correctamente (via DeviceKit, h264)",
                Data = new Dictionary<string, object>
                {
                    ["udid"] = udid,
                    ["mode"] = mode,
                    ["executable"] = executable,
                    ["pid"] = process.Id,
                    ["port"] = port,
                    ["mirror_url"] = $"http://localhost:{port}{streamPath}",
                    ["stream_format"] = "h264-annexb",
                    ["touch_supported"] = true
                }
            };
        }

        // El mirror window (mirrorWindow.ts) grava la ruta real del drag y la manda en
        // payload.points ({x,y,t} con t relativo en ms) - si viene, se reproduce tal cual en
        // vez de interpolar una linea recta entre from/to (fallback para llamados directos
        // sin ruta grabada, p.ej. desde Swagger).
        private async Task<ActionResponse> ExecuteSwipeAsync(string udid, Dictionary<string, object> payload)
        {
            var points = GetSwipePoints(payload, "points");
            if (points != null && points.Count >= 2)
                return await _controlService.SwipePathAsync(udid, points);

            return await _controlService.SwipeAsync(
                udid,
                GetDouble(payload, "from_x"),
                GetDouble(payload, "from_y"),
                GetDouble(payload, "to_x"),
                GetDouble(payload, "to_y"),
                GetInt(payload, "duration_ms"));
        }

        private static List<(double X, double Y, double TimeOffsetSeconds)> GetSwipePoints(Dictionary<string, object> payload, string key)
        {
            if (payload == null
                || !payload.TryGetValue(key, out var value)
                || value is not System.Text.Json.JsonElement element
                || element.ValueKind != System.Text.Json.JsonValueKind.Array)
                return null;

            var points = new List<(double X, double Y, double TimeOffsetSeconds)>();
            foreach (var item in element.EnumerateArray())
            {
                var x = item.TryGetProperty("x", out var xEl) ? xEl.GetDouble() : 0;
                var y = item.TryGetProperty("y", out var yEl) ? yEl.GetDouble() : 0;
                var tMs = item.TryGetProperty("t", out var tEl) ? tEl.GetDouble() : 0;
                points.Add((x, y, tMs / 1000.0));
            }

            return points;
        }

        private async Task<bool> WaitForDeviceKitReadyAsync(string udid, Process process, int maxAttempts = 8, int delayMs = 500)
        {
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (process.HasExited)
                {
                    _logger.LogWarning("[DeviceKit] Proceso para {Udid} termino inesperadamente (ExitCode={Code}) mientras se esperaba que quedara listo", udid, process.ExitCode);
                    return false;
                }

                if (await _controlService.IsAvailableAsync(udid))
                    return true;

                await Task.Delay(delayMs);
            }

            _logger.LogWarning("[DeviceKit] {Udid} no respondio via RPC despues de {Attempts} intentos", udid, maxAttempts);
            return false;
        }

        public async Task<ActionResponse> TakeScreenshotAsync(string udid, string filename = null)
        {
            try
            {
                var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var screenshotsFolder = Path.Combine(picturesPath, "ScrcpyManager");
                Directory.CreateDirectory(screenshotsFolder);

                if (string.IsNullOrWhiteSpace(filename))
                {
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    filename = $"screenshot_ios_{LastChars(udid, 8)}_{timestamp}.tiff";
                }

                filename = Path.GetFileName(filename);
                if (string.IsNullOrWhiteSpace(Path.GetExtension(filename)))
                    filename += ".tiff";

                var fullPath = Path.Combine(screenshotsFolder, filename);
                var result = await _processHelper.ExecuteCommandAsync(
                    "idevicescreenshot",
                    $"-u {QuoteArg(udid)} {QuoteArg(fullPath)}",
                    timeoutSeconds: 60);

                var fileInfo = new FileInfo(fullPath);
                var success = result.Success && fileInfo.Exists && fileInfo.Length > 0;

                if (!success)
                {
                    if (fileInfo.Exists && fileInfo.Length == 0)
                        File.Delete(fullPath);

                    return new ActionResponse
                    {
                        Success = false,
                        Message = "Error tomando screenshot iOS",
                        Error = string.IsNullOrWhiteSpace(result.Error) ? result.Output : result.Error
                    };
                }

                await _notificationService.NotifyScreenshotTaken(udid, filename);

                return new ActionResponse
                {
                    Success = true,
                    Message = "Screenshot iOS tomado correctamente",
                    Data = new Dictionary<string, object>
                    {
                        ["filename"] = filename,
                        ["full_path"] = fullPath,
                        ["folder"] = screenshotsFolder,
                        ["size"] = fileInfo.Length,
                        ["content_type"] = "image/tiff"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tomando screenshot iOS para {Udid}", udid);
                return new ActionResponse
                {
                    Success = false,
                    Message = $"Error tomando screenshot iOS: {ex.Message}",
                    Error = ex.Message
                };
            }
        }

        public Task<List<IOSMirrorSessionResponse>> GetMirrorSessionsAsync()
            => Task.FromResult(_mirrorRegistry.GetActiveSessions());

        private async Task<string> GetDeviceValueAsync(string udid, string key)
        {
            var result = await _processHelper.ExecuteCommandAsync(
                "ideviceinfo",
                $"-u {QuoteArg(udid)} -k {QuoteArg(key)}");

            return result.Success ? result.Output.Trim() : string.Empty;
        }

        private static Dictionary<string, object> NormalizeOptions(Dictionary<string, object> options)
            => options == null
                ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(options, StringComparer.OrdinalIgnoreCase);

        private static string GetOption(Dictionary<string, object> options, string key)
        {
            if (!options.TryGetValue(key, out var value) || value == null)
                return null;

            if (value is System.Text.Json.JsonElement jsonElement)
                return jsonElement.ValueKind == System.Text.Json.JsonValueKind.String
                    ? jsonElement.GetString()
                    : jsonElement.ToString();

            return value.ToString();
        }

        private static double GetDouble(Dictionary<string, object> payload, string key, double defaultValue = 0)
        {
            if (payload == null || !payload.TryGetValue(key, out var value) || value == null)
                return defaultValue;

            if (value is System.Text.Json.JsonElement jsonElement)
                return jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number ? jsonElement.GetDouble() : defaultValue;

            return Convert.ToDouble(value);
        }

        private static int? GetInt(Dictionary<string, object> payload, string key)
        {
            if (payload == null || !payload.TryGetValue(key, out var value) || value == null)
                return null;

            if (value is System.Text.Json.JsonElement jsonElement)
                return jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number ? jsonElement.GetInt32() : null;

            return Convert.ToInt32(value);
        }

        private static string LastChars(string value, int count)
            => string.IsNullOrEmpty(value) ? string.Empty : value[Math.Max(0, value.Length - count)..];

        private static string QuoteArg(string value)
            => $"\"{value?.Replace("\"", "\\\"")}\"";
    }

    internal static class IOSMirrorSessionResponseExtensions
    {
        public static Dictionary<string, object> ToDictionary(this IOSMirrorSessionResponse response)
            => new()
            {
                ["udid"] = response.Udid,
                ["mode"] = response.Mode,
                ["executable"] = response.Executable,
                ["arguments"] = response.Arguments,
                ["pid"] = response.ProcessId,
                ["started_at_utc"] = response.StartedAtUtc
            };
    }
}

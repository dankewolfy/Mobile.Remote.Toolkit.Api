using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Business.Utils;

namespace Mobile.Remote.Toolkit.Business.Services.iOS
{
    public class IOSDeviceService : IIOSDeviceService
    {
        private readonly IOSMirrorProcessRegistry _mirrorRegistry;
        private readonly IProcessHelper _processHelper;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IOSDeviceService> _logger;

        public IOSDeviceService(
            IOSMirrorProcessRegistry mirrorRegistry,
            IProcessHelper processHelper,
            INotificationService notificationService,
            IConfiguration configuration,
            ILogger<IOSDeviceService> logger)
        {
            _mirrorRegistry = mirrorRegistry;
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
                    ["touch"] = false,
                    ["touch_note"] = "Pendiente: requiere WebDriverAgent/Appium, iPhone Mirroring en macOS o una integracion especifica."
                }
            };

            if (session != null)
            {
                status["process_id"] = session.Process.Id;
                status["mirror_mode"] = session.Mode;
                status["mirror_executable"] = session.Executable;
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
                _ => new ActionResponse
                {
                    Success = false,
                    Message = "Accion iOS no soportada todavia",
                    Error = "Por ahora iOS soporta start_mirror, stop_mirror y screenshot. Touch/control queda para una siguiente fase."
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

                var mode = GetOption(opts, "mode") ?? _configuration["IOS:Mirror:Mode"] ?? "external";
                var argumentsTemplate = GetOption(opts, "arguments")
                    ?? _configuration["IOS:Mirror:Arguments"]
                    ?? "-u {udid}";
                var arguments = argumentsTemplate.Replace("{udid}", udid, StringComparison.OrdinalIgnoreCase);

                _logger.LogInformation("[iOS Mirror] Iniciando {Executable} {Arguments}", executable, arguments);

                var process = await _processHelper.StartBackgroundProcessAsync(executable, arguments);
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

                if (!session.Process.HasExited)
                    session.Process.Kill();

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

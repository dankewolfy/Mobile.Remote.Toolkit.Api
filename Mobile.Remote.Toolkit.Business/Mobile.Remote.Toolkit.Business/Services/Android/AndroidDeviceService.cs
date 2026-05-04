using System.Diagnostics;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Utils;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public class AndroidDeviceService : IAndroidDeviceService
    {
        private readonly MirrorProcessRegistry _mirrorRegistry;
        private readonly IProcessHelper _processHelper;
        private readonly IFileService _fileService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AndroidDeviceService> _logger;

        public AndroidDeviceService(MirrorProcessRegistry mirrorRegistry, IProcessHelper processHelper, IFileService fileService, INotificationService notificationService, ILogger<AndroidDeviceService> logger)
        {
            _mirrorRegistry = mirrorRegistry;
            _processHelper = processHelper;
            _fileService = fileService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<AndroidDeviceResponse> GetDeviceInfoAsync(string serial)
        {
            try
            {
                var brandTask = _processHelper.ExecuteCommandAsync("adb", $"-s {serial} shell getprop ro.product.brand");
                var modelTask = _processHelper.ExecuteCommandAsync("adb", $"-s {serial} shell getprop ro.product.model");
                var versionTask = _processHelper.ExecuteCommandAsync("adb", $"-s {serial} shell getprop ro.build.version.release");

                await Task.WhenAll(brandTask, modelTask, versionTask);

                var brand = brandTask.Result.Success ? brandTask.Result.Output.Trim() : "Desconocido";
                var model = modelTask.Result.Success ? modelTask.Result.Output.Trim() : "Desconocido";
                var version = versionTask.Result.Success ? versionTask.Result.Output.Trim() : "Desconocido";

                var deviceName = brand != "Desconocido" && model != "Desconocido"
                    ? $"{brand} {model}"
                    : $"Android {serial[Math.Max(0, serial.Length - 4)..]}";

                var device = new AndroidDeviceResponse
                {
                    Id = serial,
                    Serial = serial,
                    Name = deviceName,
                    Brand = brand,
                    Model = model,
                    AndroidVersion = version,
                    Platform = "android"
                };

                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo info del dispositivo {serial}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<AndroidDeviceResponse>> GetConnectedDevicesAsync()
        {
            var result = await _processHelper.ExecuteCommandAsync("adb", "devices");

            if (!result.Success)
            {
                _logger.LogError($"Error ejecutando adb devices: {result.Error}");
                return new List<AndroidDeviceResponse>();
            }

            var devices = new List<AndroidDeviceResponse>();
            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var deviceTasks = new List<Task<AndroidDeviceResponse>>();

            foreach (var line in lines)
            {
                if (line.Contains("device") && !line.Contains("List of devices"))
                {
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var serial = parts[0].Trim();
                        _logger.LogInformation($"Dispositivo encontrado: {serial}");

                        deviceTasks.Add(GetDeviceInfoAsync(serial));
                    }
                }
            }

            var deviceResults = await Task.WhenAll(deviceTasks);

            devices.AddRange(deviceResults.Where(d => d != null));

            return devices;
        }

        public async Task<ActionResponse> ExecuteActionAsync(string serial, string action, Dictionary<string, object> options, Dictionary<string, object> payload)
        {
            return action.ToLower() switch
            {
                "start_mirror" => await StartMirrorAsync(serial, options),
                "stop_mirror" => await StopMirrorAsync(serial),
                "screenshot" => await TakeScreenshotAsync(serial, payload?.GetValueOrDefault("filename")?.ToString()),
                _ => new ActionResponse { Success = false, Error = "Acción no reconocida" }
            };
        }

        public Task<bool> IsMirrorActiveAsync(string serial)
            => Task.FromResult(_mirrorRegistry.IsActive(serial));

        public async Task<ActionResponse> StartMirrorAsync(string serial, Dictionary<string, object> options = null)
        {
            try
            {
                // Construir argumentos de scrcpy
                var arguments = $"-s {serial}";

                if (options != null && options.Count > 0)
                {
                    // Normalizar claves a minúsculas para comparación case-insensitive
                    var opts = new Dictionary<string, object>(options, StringComparer.OrdinalIgnoreCase);

                    if (opts.TryGetValue("stayAwake", out var stayAwake) && stayAwake is true)
                        arguments += " --stay-awake";

                    if (opts.TryGetValue("noAudio", out var noAudio) && noAudio is true)
                        arguments += " --no-audio";

                    if (opts.TryGetValue("showTouches", out var showTouches) && showTouches is true)
                        arguments += " --show-touches";

                    if (opts.TryGetValue("turnScreenOff", out var turnScreenOff) && turnScreenOff is true)
                        arguments += " --turn-screen-off";
                }
                else
                {
                    arguments += " --stay-awake";
                }

                _logger.LogInformation($"Iniciando scrcpy con argumentos: {arguments}");

                // Si ya hay un mirror activo para este serial, rechazar
                if (_mirrorRegistry.IsActive(serial))
                {
                    var pid = _mirrorRegistry.GetAlive(serial)!.Id;
                    _logger.LogWarning($"Mirror ya activo para {serial} (PID={pid})");
                    return new ActionResponse
                    {
                        Success = false,
                        Message = "Ya hay un mirror activo para este dispositivo",
                        Error = "Mirror already running"
                    };
                }

                var process = await _processHelper.StartBackgroundProcessAsync("scrcpy", arguments);
                _mirrorRegistry.Register(serial, process);

                return new ActionResponse
                {
                    Success = true,
                    Message = "Mirror iniciado correctamente",
                    Data = new Dictionary<string, object>
                    {
                        ["serial"] = serial,
                        ["arguments"] = arguments,
                        ["pid"] = process.Id
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Excepción iniciando mirror para {serial}");
                return new ActionResponse
                {
                    Success = false,
                    Message = $"Error iniciando mirror: {ex.Message}",
                    Error = ex.Message
                };
            }
        }

        public async Task<ActionResponse> StopMirrorAsync(string serial)
        {
            try
            {
                _logger.LogInformation($"Deteniendo mirror para dispositivo: {serial}");

                var trackedProcess = _mirrorRegistry.Remove(serial);

                if (trackedProcess == null)
                {
                    _logger.LogInformation($"No hay mirror registrado para {serial}");
                    return new ActionResponse { Success = true, Message = "No hay mirror activo para este dispositivo" };
                }

                try
                {
                    if (!trackedProcess.HasExited)
                    {
                        trackedProcess.Kill();
                        _logger.LogInformation($"Proceso scrcpy (PID={trackedProcess.Id}) terminado para {serial}");
                    }
                    trackedProcess.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error terminando proceso para {serial}");
                }

                await _notificationService.NotifyMirrorStopped(serial);
                return new ActionResponse { Success = true, Message = "Mirror detenido correctamente" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deteniendo mirror para {serial}");
                return new ActionResponse
                {
                    Success = false,
                    Error = $"Error deteniendo mirror: {ex.Message}"
                };
            }
        }

        public async Task<ActionResponse> TakeScreenshotAsync(string serial, string filename = null)
        {
            try
            {
                var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var scrcpyFolder = Path.Combine(picturesPath, "ScrcpyManager");
                Directory.CreateDirectory(scrcpyFolder);

                if (string.IsNullOrEmpty(filename))
                {
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    filename = $"screenshot_{serial}_{timestamp}.png";
                }

                var fullPath = Path.Combine(scrcpyFolder, filename);

                var result = await _processHelper.ExecuteCommandAsync("scrcpy",
                    $"-s {serial} --no-display --screenshot={fullPath}");

                if (result.Success)
                {
                    await _notificationService.NotifyScreenshotTaken(serial, filename);
                }

                return new ActionResponse
                {
                    Success = result.Success,
                    Message = result.Success ? "Screenshot tomado correctamente" : "Error tomando screenshot",
                    Error = result.Success ? null : result.Error,
                    Data = result.Success ? new Dictionary<string, object>
                    {
                        ["filename"] = filename,
                        ["full_path"] = fullPath,
                        ["folder"] = scrcpyFolder
                    } : null
                };
            }
            catch (Exception ex)
            {
                return new ActionResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<List<string>> GetConnectedDeviceSerialsAsync()
        {
            var result = await _processHelper.ExecuteCommandAsync("adb", "devices");

            if (!result.Success) return new List<string>();

            var serials = new List<string>();
            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines.Skip(1)) // Skip header
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("device")) continue;

                var parts = line.Split('\t');
                if (parts.Length >= 2)
                {
                    serials.Add(parts[0].Trim());
                }
            }

            return serials;
        }

        public async Task<List<AndroidDeviceResponse>> GetActiveDevicesAsync()
        {
            var allDevices = await GetConnectedDevicesAsync();
            return allDevices.Where(d => d.Active).ToList();
        }

        public async Task<bool> IsDeviceConnectedAsync(string serial)
        {
            var connectedDevices = await GetConnectedDeviceSerialsAsync();
            return connectedDevices.Contains(serial);
        }

        public async Task<Dictionary<string, object>> GetDeviceStatusAsync(string serial)
        {
            var isConnected = await IsDeviceConnectedAsync(serial);
            var isMirrorActive = await IsMirrorActiveAsync(serial);

            var status = new Dictionary<string, object>
            {
                ["connected"] = isConnected,
                ["mirror_active"] = isMirrorActive,
                ["serial"] = serial,
                ["timestamp"] = DateTime.UtcNow
            };

            if (isMirrorActive)
            {
                var process = _mirrorRegistry.GetAlive(serial);
                if (process != null)
                {
                    status["process_id"] = process.Id;
                    status["process_name"] = process.ProcessName;
                }
            }

            return status;
        }

        public async Task<ActionResponse> ExecuteAdbCommandAsync(string serial, string command)
        {
            try
            {
                var result = await _processHelper.ExecuteCommandAsync("adb", $"-s {serial} {command}");

                return new ActionResponse
                {
                    Success = result.Success,
                    Message = result.Success ? "Comando ejecutado correctamente" : "Error ejecutando comando",
                    Error = result.Success ? string.Empty : result.Error,
                    Data = new Dictionary<string, object>
                    {
                        ["output"] = result.Output,
                        ["command"] = command
                    }
                };
            }
            catch (Exception ex)
            {
                return new ActionResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ActionResponse> ExecuteScrcpyCommandAsync(string serial, string command)
        {
            try
            {
                var result = await _processHelper.ExecuteCommandAsync("scrcpy", $"-s {serial} {command}");

                return new ActionResponse
                {
                    Success = result.Success,
                    Message = result.Success ? "Comando ejecutado correctamente" : "Error ejecutando comando",
                    Error = result.Success ? string.Empty : result.Error,
                    Data = new Dictionary<string, object>
                    {
                        ["output"] = result.Output,
                        ["command"] = command
                    }
                };
            }
            catch (Exception ex)
            {
                return new ActionResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}

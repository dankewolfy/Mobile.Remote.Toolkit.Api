using System.Diagnostics;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Utils;
using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Application.Services.Android
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
                "start_mirror"   => await StartMirrorAsync(serial, options),
                "stop_mirror"    => await StopMirrorAsync(serial),
                "screenshot"     => await TakeScreenshotAsync(serial, payload?.GetValueOrDefault("filename")?.ToString()),
                "home_button"    => await SendKeyeventAsync(serial, "KEYCODE_HOME"),
                "back_button"    => await SendKeyeventAsync(serial, "KEYCODE_BACK"),
                "volume_up"      => await SendKeyeventAsync(serial, "KEYCODE_VOLUME_UP"),
                "volume_down"    => await SendKeyeventAsync(serial, "KEYCODE_VOLUME_DOWN"),
                "wake_device"    => await ToggleScreenAsync(serial),
                _ => new ActionResponse { Success = false, Error = "Acción no reconocida" }
            };
        }

        private async Task<ActionResponse> SendKeyeventAsync(string serial, string keycode)
        {
            _logger.LogInformation("[Keyevent] Serial={Serial} Keycode={Keycode}", serial, keycode);
            var result = await _processHelper.ExecuteCommandAsync("adb", $"-s {serial} shell input keyevent {keycode}");
            _logger.LogInformation("[Keyevent] Success={Success} Output='{Output}' Error='{Error}'", result.Success, result.Output, result.Error);
            return new ActionResponse
            {
                Success = result.Success,
                Message = result.Success ? $"Keyevent {keycode} enviado" : "Error enviando keyevent",
                Error = result.Success ? null : result.Error
            };
        }

        private async Task<ActionResponse> ToggleScreenAsync(string serial)
        {
            // Detectar si la pantalla está encendida o apagada
            var powerResult = await _processHelper.ExecuteCommandAsync("adb", $"-s {serial} shell dumpsys power");
            var output = powerResult.Output ?? string.Empty;

            // Distintos Android muestran distintos campos; intentar los más comunes
            bool isScreenOn = output.Contains("Display Power: state=ON")
                           || output.Contains("mWakefulness=Awake")
                           || output.Contains("mWakefulnessRaw=Awake");

            if (isScreenOn)
            {
                // Apagar pantalla manteniendo el mirror
                var r = await _processHelper.ExecuteCommandAsync("adb", $"-s {serial} shell input keyevent KEYCODE_SLEEP");
                return new ActionResponse { Success = r.Success, Message = r.Success ? "Pantalla apagada" : "Error apagando pantalla", Error = r.Error };
            }
            else
            {
                // Encender pantalla
                var r = await _processHelper.ExecuteCommandAsync("adb", $"-s {serial} shell input keyevent KEYCODE_WAKEUP");
                return new ActionResponse { Success = r.Success, Message = r.Success ? "Pantalla encendida" : "Error encendiendo pantalla", Error = r.Error };
            }
        }

        public Task<bool> IsMirrorActiveAsync(string serial)
            => Task.FromResult(_mirrorRegistry.IsActive(serial));

        public async Task<ActionResponse> StartMirrorAsync(string serial, Dictionary<string, object> options = null)
        {
            try
            {
                // Log de opciones recibidas (debug)
                _logger.LogInformation($"[Mirror] Serial={serial} | Opciones recibidas ({options?.Count ?? 0}): {(options == null ? "null" : string.Join(", ", options.Select(kv => $"{kv.Key}={kv.Value} ({kv.Value?.GetType().Name})")))}" );

                // Construir argumentos de scrcpy
                var arguments = $"-s {serial}";

                if (options != null && options.Count > 0)
                {
                    // Normalizar claves a minúsculas para comparación case-insensitive
                    var opts = new Dictionary<string, object>(options, StringComparer.OrdinalIgnoreCase);

                    if (IsTrue(opts, "stayAwake"))
                        arguments += " --stay-awake";

                    if (IsTrue(opts, "noAudio"))
                        arguments += " --no-audio";

                    if (IsTrue(opts, "showTouches"))
                        arguments += " --show-touches";

                    if (IsTrue(opts, "turnScreenOff"))
                        arguments += " --turn-screen-off";
                }
                else
                {
                    arguments += " --stay-awake";
                }

                _logger.LogInformation($"[Mirror] Comando scrcpy final: scrcpy {arguments}");

                // Título de ventana fijo para que Electron pueda encontrarla por nombre (sin comillas — serial no tiene espacios)
                arguments += $" --window-title MRT-{serial}";
                _logger.LogInformation($"[Mirror] Comando scrcpy CON título: scrcpy {arguments}");

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

                // adb exec-out screencap -p escribe PNG binario en stdout; no se puede usar
                // redirección de shell (>) desde C#, así que capturamos el stream directo.
                var adbPath = _processHelper.GetAdbPath();
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = $"-s {serial} exec-out screencap -p",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using var process = new System.Diagnostics.Process { StartInfo = startInfo };
                process.Start();

                // Leer stdout como bytes y escribir al fichero
                using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    await process.StandardOutput.BaseStream.CopyToAsync(fs);
                }

                var errorOutput = await process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit(15000));

                var fileInfo = new FileInfo(fullPath);
                bool success = process.ExitCode == 0 && fileInfo.Exists && fileInfo.Length > 0;

                if (!success)
                {
                    // Limpiar archivo vacío/inválido si hubo error
                    if (fileInfo.Exists && fileInfo.Length == 0) File.Delete(fullPath);

                    return new ActionResponse
                    {
                        Success = false,
                        Message = "Error tomando screenshot",
                        Error = string.IsNullOrWhiteSpace(errorOutput) ? $"ExitCode={process.ExitCode}" : errorOutput
                    };
                }

                await _notificationService.NotifyScreenshotTaken(serial, filename);

                return new ActionResponse
                {
                    Success = true,
                    Message = "Screenshot tomado correctamente",
                    Data = new Dictionary<string, object>
                    {
                        ["filename"] = filename,
                        ["full_path"] = fullPath,
                        ["folder"] = scrcpyFolder,
                        ["size"] = fileInfo.Length
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

        /// <summary>
        /// Lee una opción booleana del diccionario de forma robusta.
        /// Soporta System.Boolean (boxed) y System.Text.Json.JsonElement.
        /// </summary>
        private static bool IsTrue(Dictionary<string, object> opts, string key)
        {
            if (!opts.TryGetValue(key, out var val)) return false;
            return val switch
            {
                bool b => b,
                System.Text.Json.JsonElement je => je.ValueKind == System.Text.Json.JsonValueKind.True,
                string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}

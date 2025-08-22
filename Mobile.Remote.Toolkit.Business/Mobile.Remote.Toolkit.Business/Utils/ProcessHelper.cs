#nullable disable

using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Models;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Services.Android;

namespace Mobile.Remote.Toolkit.Business.Utils
{
    public class ProcessHelper : IProcessHelper
    {
        private readonly ILogger<ProcessHelper> _logger;
        private readonly ProcessCommandExecutor _executor;
        private readonly string _toolsPath;
        private readonly string _adbPath;
        private readonly string _scrcpyPath;

        public ProcessHelper(ILogger<ProcessHelper> logger, ILogger<ProcessCommandExecutor> executorLogger)
        {
            _logger = logger;
            _executor = new ProcessCommandExecutor(executorLogger);

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _toolsPath = Path.Combine(baseDirectory, "Tools");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _adbPath = Path.Combine(_toolsPath, Patform.Android.ToString(), CommandTool.Adb.ToString(), Patform.Win.ToString(), CommandTool.Adb.ToString() + ".exe");
                _scrcpyPath = Path.Combine(_toolsPath, Patform.Android.ToString(), CommandTool.Scrcpy.ToString(), Patform.Win.ToString(), CommandTool.Scrcpy.ToString() + ".exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _adbPath = Path.Combine(_toolsPath, Patform.Android.ToString(), CommandTool.Adb.ToString(), Patform.Linux.ToString(), CommandTool.Adb.ToString());
                _scrcpyPath = Path.Combine(_toolsPath, Patform.Android.ToString(), CommandTool.Scrcpy.ToString(), Patform.Linux.ToString(), CommandTool.Scrcpy.ToString());
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _adbPath = Path.Combine(_toolsPath, Patform.Android.ToString(), CommandTool.Adb.ToString(), Patform.Mac.ToString(), CommandTool.Adb.ToString());
                _scrcpyPath = Path.Combine(_toolsPath, Patform.Android.ToString(), CommandTool.Scrcpy.ToString(), Patform.Mac.ToString(), CommandTool.Scrcpy.ToString());
            }

            _logger.LogInformation($"Base Directory: {baseDirectory}");
            _logger.LogInformation($"Tools Path: {_toolsPath}");
            _logger.LogInformation($"ADB Path: {_adbPath}");
            _logger.LogInformation($"Scrcpy Path: {_scrcpyPath}");
        }

        public async Task<ProcessResultResponse> ExecuteCommandAsync(CommandTool tool, string arguments, int timeoutSeconds = 30)
        {
            string actualFileName = tool switch
            {
                CommandTool.Adb => _adbPath,
                CommandTool.Scrcpy => _scrcpyPath,
                _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
            };

            if (!File.Exists(actualFileName))
            {
                _logger.LogError($"Herramienta no encontrada: {actualFileName}");
                return new ProcessResultResponse
                {
                    Success = false,
                    Output = string.Empty,
                    Error = $"Herramienta no encontrada: {actualFileName}",
                    ExitCode = -1
                };
            }

            return await _executor.ExecuteAsync(actualFileName, arguments, timeoutSeconds);
        }

        public async Task<byte[]> ExecuteCommandBinaryAsync(CommandTool tool, string arguments, int timeoutSeconds = 30)
        {
            string actualFileName = tool switch
            {
                CommandTool.Adb => _adbPath,
                CommandTool.Scrcpy => _scrcpyPath,
                _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
            };

            var startInfo = new ProcessStartInfo
            {
                FileName = actualFileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            using var ms = new MemoryStream();
            process.Start();
            await process.StandardOutput.BaseStream.CopyToAsync(ms);
            process.WaitForExit(timeoutSeconds * 1000);
            return ms.ToArray();
        }

        public async Task<bool> IsProcessRunningAsync(string processName)
        {
            try
            {
                _logger.LogDebug($"Verificando si el proceso '{processName}' está ejecutándose");

                await Task.Run(() =>
                {
                    var normalizedName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        ? processName[..^4]
                        : processName;

                    var processes = Process.GetProcessesByName(normalizedName);
                    return processes.Length > 0;
                });

                var processes = Process.GetProcessesByName(processName.Replace(".exe", ""));
                var isRunning = processes.Length > 0;

                _logger.LogDebug($"Proceso '{processName}' {(isRunning ? "está" : "no está")} ejecutándose");

                return isRunning;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verificando proceso: {processName}");
                return false;
            }
        }

        public async Task<List<int>> GetProcessIdsByNameAsync(string processName)
        {
            try
            {
                _logger.LogDebug($"Obteniendo IDs de procesos para: {processName}");

                var processIds = await Task.Run(() =>
                {
                    var normalizedName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        ? processName[..^4]
                        : processName;

                    var processes = Process.GetProcessesByName(normalizedName);
                    return processes.Select(p => p.Id).ToList();
                });

                _logger.LogDebug($"Encontrados {processIds.Count} procesos con nombre '{processName}': [{string.Join(", ", processIds)}]");

                return processIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo IDs de procesos: {processName}");
                return new List<int>();
            }
        }

        public async Task<bool> KillProcessAsync(int processId)
        {
            try
            {
                _logger.LogInformation($"Intentando terminar proceso con ID: {processId}");

                var killed = await Task.Run(() =>
                {
                    try
                    {
                        var process = Process.GetProcessById(processId);

                        _logger.LogDebug($"Proceso encontrado: {process.ProcessName} (PID: {processId})");

                        process.Kill();

                        process.WaitForExit(5000);

                        return process.HasExited;
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogDebug($"Proceso con ID {processId} no existe o ya fue terminado");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error terminando proceso {processId}");
                        return false;
                    }
                });

                if (killed)
                {
                    _logger.LogInformation($"Proceso {processId} terminado exitosamente");
                }
                else
                {
                    _logger.LogWarning($"No se pudo terminar el proceso {processId}");
                }

                return killed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error terminando proceso: {processId}");
                return false;
            }
        }
    }
}

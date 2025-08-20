#nullable disable

using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;

namespace Mobile.Remote.Toolkit.Business.Utils
{
    public class ProcessHelper : IProcessHelper
    {
        private readonly ILogger<ProcessHelper> _logger;
        private readonly string _toolsPath;
        private readonly string _adbPath;
        private readonly string _scrcpyPath;

        public ProcessHelper(ILogger<ProcessHelper> logger)
        {
            _logger = logger;
            
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _toolsPath = Path.Combine(baseDirectory, "Tools");
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _adbPath = Path.Combine(_toolsPath, "Android", "adb", "adb.exe");
                _scrcpyPath = Path.Combine(_toolsPath, "Android", "scrcpy", "scrcpy.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _adbPath = Path.Combine(_toolsPath, "Android", "adb", "adb");
                _scrcpyPath = Path.Combine(_toolsPath, "Android", "scrcpy", "scrcpy");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _adbPath = Path.Combine(_toolsPath, "Android", "adb", "adb");
                _scrcpyPath = Path.Combine(_toolsPath, "Android", "scrcpy", "scrcpy");
            }

            _logger.LogInformation($"Base Directory: {baseDirectory}");
            _logger.LogInformation($"Tools Path: {_toolsPath}");
            _logger.LogInformation($"ADB Path: {_adbPath}");
            _logger.LogInformation($"Scrcpy Path: {_scrcpyPath}");
        }

        public async Task<ProcessResult> ExecuteCommandAsync(string fileName, string arguments, int timeoutSeconds = 30)
        {
            var timeoutMs = timeoutSeconds * 1000; // Convertir a milisegundos

            try
            {
                // Mapear comandos a rutas completas
                var actualFileName = fileName.ToLower() switch
                {
                    "adb" => _adbPath,
                    "scrcpy" => _scrcpyPath,
                    _ => fileName
                };

                _logger.LogInformation($"Ejecutando: {actualFileName} {arguments}");

                // Verificar que el archivo existe
                if (!File.Exists(actualFileName) && fileName.ToLower() is "adb" or "scrcpy")
                {
                    _logger.LogError($"Herramienta no encontrada: {actualFileName}");
                    return new ProcessResult
                    {
                        Success = false,
                        Output = string.Empty,
                        Error = $"Herramienta no encontrada: {actualFileName}"
                    };
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = actualFileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.Exists(_toolsPath) ? _toolsPath : Environment.CurrentDirectory
                };

                using var process = new Process { StartInfo = startInfo };
                
                var outputBuffer = new List<string>();
                var errorBuffer = new List<string>();

                process.OutputDataReceived += (_, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        outputBuffer.Add(e.Data);
                };

                process.ErrorDataReceived += (_, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        errorBuffer.Add(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var completed = await Task.Run(() => process.WaitForExit(timeoutMs));

                if (!completed)
                {
                    _logger.LogWarning($"Proceso excedió timeout de {timeoutSeconds}s: {actualFileName}");
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error terminando proceso por timeout");
                    }

                    return new ProcessResult
                    {
                        Success = false,
                        Output = string.Empty,
                        Error = $"Timeout después de {timeoutSeconds} segundos"
                    };
                }

                var output = string.Join("\n", outputBuffer);
                var error = string.Join("\n", errorBuffer);

                var success = process.ExitCode == 0;

                _logger.LogInformation($"Proceso completado: ExitCode={process.ExitCode}, Success={success}");
                if (!string.IsNullOrEmpty(output))
                    _logger.LogDebug($"Output: {output}");
                if (!string.IsNullOrEmpty(error))
                    _logger.LogDebug($"Error: {error}");

                return new ProcessResult
                {
                    Success = success,
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ejecutando comando: {fileName} {arguments}");
                return new ProcessResult
                {
                    Success = false,
                    Output = string.Empty,
                    Error = ex.Message
                };
            }
        }

        public async Task<bool> IsProcessRunningAsync(string processName)
        {
            try
            {
                _logger.LogDebug($"Verificando si el proceso '{processName}' está ejecutándose");

                await Task.Run(() =>
                {
                    // Normalizar el nombre del proceso (quitar .exe si está presente)
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
                    // Normalizar el nombre del proceso (quitar .exe si está presente)
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
                        
                        // Esperar un poco para que el proceso termine
                        process.WaitForExit(5000);
                        
                        return process.HasExited;
                    }
                    catch (ArgumentException)
                    {
                        // El proceso no existe o ya fue terminado
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

using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Models.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public class ProcessCommandExecutor : ICommandExecutor
    {
        private readonly ILogger<ProcessCommandExecutor> _logger;

        public ProcessCommandExecutor(ILogger<ProcessCommandExecutor> logger)
        {
            _logger = logger;
        }

        public async Task<CommandResultResponse> ExecuteAsync(string executable, string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var process = new Process { StartInfo = startInfo };
                var outputBuffer = new List<string>();
                var errorBuffer = new List<string>();
                process.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) outputBuffer.Add(e.Data); };
                process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) errorBuffer.Add(e.Data); };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await Task.Run(() => process.WaitForExit());
                var output = string.Join("\n", outputBuffer);
                var error = string.Join("\n", errorBuffer);
                var success = process.ExitCode == 0;
                return new CommandResultResponse(success, output, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command: {Executable} {Arguments}", executable, arguments);
                return new CommandResultResponse(false, string.Empty, ex.Message);
            }
        }

        public async Task<List<int>> GetProcessIdsByNameAsync(string processName)
        {
            try
            {
                var normalizedName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? processName[..^4]
                    : processName;
                var processes = await Task.Run(() => Process.GetProcessesByName(normalizedName));
                return new List<int>(processes.Select(p => p.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting process IDs: {ProcessName}", processName);
                return new List<int>();
            }
        }

        public async Task<bool> KillProcessAsync(int processId)
        {
            try
            {
                var killed = await Task.Run(() =>
                {
                    try
                    {
                        var process = Process.GetProcessById(processId);
                        process.Kill();
                        process.WaitForExit(5000);
                        return process.HasExited;
                    }
                    catch (ArgumentException)
                    {
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error killing process {ProcessId}", processId);
                        return false;
                    }
                });
                return killed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error killing process: {ProcessId}", processId);
                return false;
            }
        }
    }
}

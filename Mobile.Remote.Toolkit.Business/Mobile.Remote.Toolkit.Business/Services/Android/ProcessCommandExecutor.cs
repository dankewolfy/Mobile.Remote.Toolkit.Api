using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public class ProcessCommandExecutor
    {
        private readonly ILogger<ProcessCommandExecutor> _logger;

        public ProcessCommandExecutor(ILogger<ProcessCommandExecutor> logger)
        {
            _logger = logger;
        }

        public async Task<ProcessResultResponse> ExecuteAsync(string executable, string arguments, int timeoutSeconds = 30)
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
                var exited = await Task.Run(() => process.WaitForExit(timeoutSeconds * 1000));
                var output = string.Join("\n", outputBuffer);
                var error = string.Join("\n", errorBuffer);
                var success = process.ExitCode == 0;
                return new ProcessResultResponse
                {
                    Success = success,
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command: {Executable} {Arguments}", executable, arguments);
                return new ProcessResultResponse
                {
                    Success = false,
                    Output = string.Empty,
                    Error = ex.Message,
                    ExitCode = -1
                };
            }
        }
    }
}

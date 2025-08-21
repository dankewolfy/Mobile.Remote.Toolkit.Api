using System.Diagnostics;
using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Models.Android;
using Mobile.Remote.Toolkit.Business.Models.Requests.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public class MirrorService : IMirrorService
    {
        private readonly ICommandExecutor _executor;
        private readonly ILogger<MirrorService> _logger;
        private readonly ConcurrentDictionary<string, Process> _activeProcesses = new();

        public MirrorService(ICommandExecutor executor, ILogger<MirrorService> logger)
        {
            _executor = executor;
            _logger = logger;
        }

        public async Task<ActionResponse> StartMirrorAsync(string serial, Dictionary<string, object> options)
        {
            var args = ScrcpyCommands.StartMirror(serial, options);
            try
            {
                var result = await _executor.ExecuteAsync("scrcpy", args);
                if (result.Success)
                {
                    _logger.LogInformation("Mirror started for {Serial}", serial);
                    return new ActionResponse(true, "Mirror started");
                }
                else
                {
                    _logger.LogWarning("Error starting mirror for {Serial}: {Error}", serial, result.Error);
                    return new ActionResponse(false, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting mirror for {Serial}", serial);
                return new ActionResponse(false, ex.Message);
            }
        }

        public async Task<ActionResponse> StopMirrorAsync(string serial)
        {
            try
            {
                var pids = await _executor.GetProcessIdsByNameAsync("scrcpy");
                foreach (var pid in pids)
                    await _executor.KillProcessAsync(pid);
                _logger.LogInformation("Mirror stopped for {Serial}", serial);
                return new ActionResponse(true, "Mirror stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping mirror for {Serial}", serial);
                return new ActionResponse(false, ex.Message);
            }
        }

        public async Task<bool> IsMirrorActiveAsync(string serial)
        {
            var pids = await _executor.GetProcessIdsByNameAsync("scrcpy");
            return pids.Any();
        }

        public async Task<Dictionary<string, object>> GetMirrorStatusAsync(string serial)
        {
            var active = await IsMirrorActiveAsync(serial);
            return new Dictionary<string, object>
            {
                ["active"] = active,
                ["serial"] = serial
            };
        }
    }
}

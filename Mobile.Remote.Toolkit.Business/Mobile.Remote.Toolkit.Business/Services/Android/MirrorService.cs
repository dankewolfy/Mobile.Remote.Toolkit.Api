using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Utils;
using Mobile.Remote.Toolkit.Business.Models;
using Mobile.Remote.Toolkit.Business.Utils.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public class MirrorService : IMirrorService
    {
        private readonly IProcessHelper _processHelper;
        private readonly ILogger<MirrorService> _logger;

        public MirrorService(IProcessHelper processHelper, ILogger<MirrorService> logger)
        {
            _processHelper = processHelper;
            _logger = logger;
        }

        public async Task<ActionResponse> StartMirrorAsync(string serial, Dictionary<string, object> options)
        {
            var args = ScrcpyCommands.StartMirror(serial, options);
            try
            {
                var processResult = await _processHelper.ExecuteCommandAsync(CommandTool.Scrcpy, args);
                if (processResult.Success)
                {
                    _logger.LogInformation("Mirror started for {Serial}", serial);
                    return new ActionResponse(true, "Mirror started");
                }
                else
                {
                    _logger.LogWarning("Error starting mirror for {Serial}: {Error}", serial, processResult.Error);
                    return new ActionResponse(false, processResult.Error);
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
                var pids = await _processHelper.GetProcessIdsByNameAsync(CommandTool.Scrcpy.ToString());
                foreach (var pid in pids)
                    await _processHelper.KillProcessAsync(pid);
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
            var pids = await _processHelper.GetProcessIdsByNameAsync(CommandTool.Scrcpy.ToString());
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

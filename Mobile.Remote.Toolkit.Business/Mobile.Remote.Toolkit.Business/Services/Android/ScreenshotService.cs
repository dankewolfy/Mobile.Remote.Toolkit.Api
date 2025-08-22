using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Utils;
using Mobile.Remote.Toolkit.Business.Models;
using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public class ScreenshotService : IScreenshotService
    {
        private readonly IProcessHelper _processHelper;
        private readonly ILogger<ScreenshotService> _logger;

        public ScreenshotService(IProcessHelper processHelper, ILogger<ScreenshotService> logger)
        {
            _processHelper = processHelper;
            _logger = logger;
        }

        public async Task<ActionResponse> TakeScreenshotAsync(string serial, string filename)
        {
            try
            {
                var args = $"-s {serial} exec-out screencap -p > {filename}";
                var result = await _processHelper.ExecuteCommandAsync(CommandTool.Scrcpy, args);
                if (result.Success)
                {
                    _logger.LogInformation("Screenshot taken for {Serial} at {Filename}", serial, filename);
                    var data = new Dictionary<string, object> { { "filename", filename } };
                    return new ActionResponse(true, "Screenshot taken", data);
                }
                else
                {
                    _logger.LogWarning("Error taking screenshot for {Serial}: {Error}", serial, result.Error);
                    return new ActionResponse(false, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error taking screenshot for {Serial}", serial);
                return new ActionResponse(false, ex.Message);
            }
        }
    }
}

using Microsoft.Extensions.Logging;
using Mobile.Remote.Toolkit.Business.Models.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public class ScreenshotService : IScreenshotService
    {
        private readonly ICommandExecutor _executor;
        private readonly ILogger<ScreenshotService> _logger;

        public ScreenshotService(ICommandExecutor executor, ILogger<ScreenshotService> logger)
        {
            _executor = executor;
            _logger = logger;
        }

    public async Task<ActionResponse> TakeScreenshotAsync(string serial, string filename)
        {
            try
            {
                var command = $"-s {serial} exec-out screencap -p > {filename}";
                var result = await _executor.ExecuteAsync("adb", command);
                if (result.Success)
                {
                    _logger.LogInformation("Screenshot taken for {Serial} at {Filename}", serial, filename);
                    return new ActionResponse(true, "Screenshot taken", filename);
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

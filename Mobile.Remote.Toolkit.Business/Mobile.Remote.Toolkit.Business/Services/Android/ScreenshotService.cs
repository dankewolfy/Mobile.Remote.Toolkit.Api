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

        public async Task<ScreenshotResponse> TakeScreenshotAsync(string serial, string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                filename = $"screenshot_{serial}_{DateTime.Now:yyyyMMdd_HHmmss}.png";

            var args = $"-s {serial} exec-out screencap -p";
            var imageBytes = await _processHelper.ExecuteCommandBinaryAsync(CommandTool.Adb, args);

            return new ScreenshotResponse
            {
                Bytes = imageBytes ?? [],
                Filename = filename
            };
        }
    }
}

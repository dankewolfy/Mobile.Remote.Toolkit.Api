using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Utils;
using Mobile.Remote.Toolkit.Business.Models;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public class AndroidDeviceService : IAndroidDeviceService
    {
        private readonly IProcessHelper _processHelper;
        private readonly IDeviceInfoProvider _deviceInfoProvider;
        private readonly IMirrorService _mirrorService;
        private readonly IScreenshotService _screenshotService;

        public AndroidDeviceService(IProcessHelper processHelper, IDeviceInfoProvider deviceInfoProvider, IMirrorService mirrorService, IScreenshotService screenshotService, ILogger<AndroidDeviceService> logger)
        {
            _processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));
            _deviceInfoProvider = deviceInfoProvider ?? throw new ArgumentNullException(nameof(deviceInfoProvider));
            _mirrorService = mirrorService ?? throw new ArgumentNullException(nameof(mirrorService));
            _screenshotService = screenshotService ?? throw new ArgumentNullException(nameof(screenshotService));
        }

        public Task<AndroidDeviceResponse> GetDeviceInfoAsync(string serial)
            => _deviceInfoProvider.GetDeviceInfoAsync(serial);

        public Task<List<AndroidDeviceResponse>> GetConnectedDeviceSerialsAsync()
            => _deviceInfoProvider.GetConnectedDeviceSerialsAsync();

        public Task<ActionResponse> StartMirrorAsync(string serial, Dictionary<string, object> options)
            => _mirrorService.StartMirrorAsync(serial, options);

        public Task<ActionResponse> StopMirrorAsync(string serial)
            => _mirrorService.StopMirrorAsync(serial);

        public Task<bool> IsMirrorActiveAsync(string serial)
            => _mirrorService.IsMirrorActiveAsync(serial);

        public Task<Dictionary<string, object>> GetMirrorStatusAsync(string serial)
            => _mirrorService.GetMirrorStatusAsync(serial);

        public Task<ActionResponse> TakeScreenshotAsync(string serial, string filename)
            => _screenshotService.TakeScreenshotAsync(serial, filename);

        public async Task<ProcessResultResponse> ExecuteAdbCommandAsync(string serial, string command)
        {
            var args = $"-s {serial} {command}";
            return await _processHelper.ExecuteCommandAsync(CommandTool.Adb, args);
        }

        public Task<ProcessResultResponse> ExecuteScrcpyCommandAsync(string serial, string command)
            => _processHelper.ExecuteCommandAsync(CommandTool.Scrcpy, $"-s {serial} {command}");

        public async Task<List<AndroidDeviceResponse>> GetConnectedDeviceInfosAsync()
        {
            var devices = await GetConnectedDeviceSerialsAsync();
            var deviceInfos = new List<AndroidDeviceResponse>();
            foreach (var device in devices)
            {
                var info = await GetDeviceInfoAsync(device.Serial);
                deviceInfos.Add(info);
            }
            return deviceInfos;
        }
    }
}

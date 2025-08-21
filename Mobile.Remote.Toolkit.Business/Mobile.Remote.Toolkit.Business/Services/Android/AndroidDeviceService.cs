using Microsoft.Extensions.Logging;
using Mobile.Remote.Toolkit.Business.Models.Android;
using Mobile.Remote.Toolkit.Business.Models.Requests.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public class AndroidDeviceService : IAndroidDeviceService
    {
        private readonly IDeviceInfoProvider _deviceInfoProvider;
        private readonly IMirrorService _mirrorService;
        private readonly IScreenshotService _screenshotService;
        private readonly ICommandExecutor _commandExecutor;
        private readonly ILogger<AndroidDeviceService> _logger;

        public AndroidDeviceService(
            IDeviceInfoProvider deviceInfoProvider,
            IMirrorService mirrorService,
            IScreenshotService screenshotService,
            ICommandExecutor commandExecutor,
            ILogger<AndroidDeviceService> logger)
        {
            _deviceInfoProvider = deviceInfoProvider;
            _mirrorService = mirrorService;
            _screenshotService = screenshotService;
            _commandExecutor = commandExecutor;
            _logger = logger;
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

        public Task<CommandResultResponse> ExecuteAdbCommandAsync(string serial, string command)
            => _commandExecutor.ExecuteAsync("adb", $"-s {serial} {command}");

        public Task<CommandResultResponse> ExecuteScrcpyCommandAsync(string serial, string command)
            => _commandExecutor.ExecuteAsync("scrcpy", $"-s {serial} {command}");

            public async Task<List<AndroidDeviceResponse>> GetConnectedDeviceInfosAsync()
            {
                var serials = await GetConnectedDeviceSerialsAsync();
                var deviceInfos = new List<AndroidDeviceResponse>();
                foreach (var serial in serials)
                {
                    var info = await GetDeviceInfoAsync(serial);
                    deviceInfos.Add(info);
                }
                return deviceInfos;
            }
    }
}

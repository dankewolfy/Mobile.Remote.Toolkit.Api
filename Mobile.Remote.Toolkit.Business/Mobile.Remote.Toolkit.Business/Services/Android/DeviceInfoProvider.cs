using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Utils;
using Mobile.Remote.Toolkit.Business.Models;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public class DeviceInfoProvider : IDeviceInfoProvider
    {
        private readonly IProcessHelper _processHelper;
        private readonly ILogger<DeviceInfoProvider> _logger;

        public DeviceInfoProvider(IProcessHelper processHelper, ILogger<DeviceInfoProvider> logger)
        {
            _processHelper = processHelper;
            _logger = logger;
        }

        public async Task<AndroidDeviceResponse> GetDeviceInfoAsync(string serial)
        {
            try
            {
                var brandTask = _processHelper.ExecuteCommandAsync(CommandTool.Adb, AndroidCommands.GetProperty(serial, AndroidCommands.Properties.Brand));
                var modelTask = _processHelper.ExecuteCommandAsync(CommandTool.Adb, AndroidCommands.GetProperty(serial, AndroidCommands.Properties.Model));
                var versionTask = _processHelper.ExecuteCommandAsync(CommandTool.Adb, AndroidCommands.GetProperty(serial, AndroidCommands.Properties.Version));
                await Task.WhenAll(brandTask, modelTask, versionTask);

                var brand = brandTask.Result.Success ? brandTask.Result.Output.Trim() : Patform.Unknown.ToString();
                var model = modelTask.Result.Success ? modelTask.Result.Output.Trim() : Patform.Unknown.ToString();
                var version = versionTask.Result.Success ? versionTask.Result.Output.Trim() : Patform.Unknown.ToString();
                var deviceName = brand != Patform.Unknown.ToString() && model != Patform.Unknown.ToString()
                    ? $"{brand} {model}"
                    : $"{Patform.Android} {serial[Math.Max(0, serial.Length - 4)..]}";

                return new AndroidDeviceResponse
                {
                    Id = serial,
                    Serial = serial,
                    Name = deviceName,
                    Brand = brand,
                    Model = model,
                    AndroidVersion = version,
                    Platform = Patform.Android.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device info {Serial}", serial);
                return new AndroidDeviceResponse
                {
                    Id = serial,
                    Serial = serial,
                    Name = $"{Patform.Android} {serial}",
                    Brand = Patform.Unknown.ToString(),
                    Model = Patform.Unknown.ToString(),
                    AndroidVersion = Patform.Unknown.ToString(),
                    Platform = Patform.Android.ToString()
                };
            }
        }

        public async Task<List<AndroidDeviceResponse>> GetConnectedDeviceSerialsAsync()
        {
            var result = await _processHelper.ExecuteCommandAsync(CommandTool.Adb, AndroidCommands.ListDevices);
            if (!result.Success) return new List<AndroidDeviceResponse>();
            var serials = result.Output.Split('\n').Skip(1).Where(l => l.Contains("device")).Select(l => l.Split('\t')[0]).ToList();
            var devices = new List<AndroidDeviceResponse>();
            foreach (var serial in serials)
            {
                var info = await GetDeviceInfoAsync(serial);
                devices.Add(info);
            }
            return devices;
        }
    }
}

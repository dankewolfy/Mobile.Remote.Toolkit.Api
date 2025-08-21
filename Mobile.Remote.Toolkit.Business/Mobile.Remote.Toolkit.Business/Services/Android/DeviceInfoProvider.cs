using Microsoft.Extensions.Logging;
using Mobile.Remote.Toolkit.Business.Models.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public class DeviceInfoProvider : IDeviceInfoProvider
    {
        private readonly ICommandExecutor _executor;
        private readonly ILogger<DeviceInfoProvider> _logger;

        public DeviceInfoProvider(ICommandExecutor executor, ILogger<DeviceInfoProvider> logger)
        {
            _executor = executor;
            _logger = logger;
        }

        public async Task<AndroidDeviceResponse> GetDeviceInfoAsync(string serial)
        {
            try
            {
                var brandTask = _executor.ExecuteAsync("adb", AndroidCommands.GetProperty(serial, AndroidCommands.Properties.Brand));
                var modelTask = _executor.ExecuteAsync("adb", AndroidCommands.GetProperty(serial, AndroidCommands.Properties.Model));
                var versionTask = _executor.ExecuteAsync("adb", AndroidCommands.GetProperty(serial, AndroidCommands.Properties.Version));
                await Task.WhenAll(brandTask, modelTask, versionTask);

                var brand = brandTask.Result.Success ? brandTask.Result.Output.Trim() : "Unknown";
                var model = modelTask.Result.Success ? modelTask.Result.Output.Trim() : "Unknown";
                var version = versionTask.Result.Success ? versionTask.Result.Output.Trim() : "Unknown";
                var deviceName = brand != "Unknown" && model != "Unknown"
                    ? $"{brand} {model}"
                    : $"Android {serial[Math.Max(0, serial.Length - 4)..]}";

                return new AndroidDeviceResponse
                {
                    Id = serial,
                    Serial = serial,
                    Name = deviceName,
                    Brand = brand,
                    Model = model,
                    AndroidVersion = version,
                    Platform = "android"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device info {Serial}", serial);
                return new AndroidDeviceResponse
                {
                    Id = serial,
                    Serial = serial,
                    Name = $"Android {serial}",
                    Brand = "Unknown",
                    Model = "Unknown",
                    AndroidVersion = "Unknown",
                    Platform = "android"
                };
            }
        }

        public async Task<List<AndroidDeviceResponse>> GetConnectedDeviceSerialsAsync()
        {
            var result = await _executor.ExecuteAsync("adb", AndroidCommands.ListDevices);
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

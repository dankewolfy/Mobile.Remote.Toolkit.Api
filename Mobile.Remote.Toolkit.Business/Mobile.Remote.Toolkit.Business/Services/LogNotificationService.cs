using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Services
{
    public class LogNotificationService : INotificationService
    {
        private readonly ILogger<LogNotificationService> _logger;

        public LogNotificationService(ILogger<LogNotificationService> logger)
        {
            _logger = logger;
        }

        public virtual async Task NotifyDeviceStatusChanged(string serial, Dictionary<string, object> status)
        {
            _logger.LogInformation("Device status changed: {Serial}", serial);
            await Task.CompletedTask;
        }

        public virtual async Task NotifyDeviceConnected(AndroidDeviceResponse device)
        {
            _logger.LogInformation("Device connected: {Serial} - {Name}", device.Serial, device.Name);
            await Task.CompletedTask;
        }

        public virtual async Task NotifyDeviceDisconnected(string serial)
        {
            _logger.LogInformation("Device disconnected: {Serial}", serial);
            await Task.CompletedTask;
        }

        public virtual async Task NotifyMirrorStarted(string serial)
        {
            _logger.LogInformation("Mirror started for device: {Serial}", serial);
            await Task.CompletedTask;
        }

        public virtual async Task NotifyMirrorStopped(string serial)
        {
            _logger.LogInformation("Mirror stopped for device: {Serial}", serial);
            await Task.CompletedTask;
        }

        public virtual async Task NotifyScreenshotTaken(string serial, string filename)
        {
            _logger.LogInformation("Screenshot taken for device: {Serial} - {Filename}", serial, filename);
            await Task.CompletedTask;
        }
    }
}

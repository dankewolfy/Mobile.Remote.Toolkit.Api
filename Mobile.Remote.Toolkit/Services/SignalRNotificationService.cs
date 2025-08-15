using Microsoft.AspNetCore.SignalR;

using Mobile.Remote.Toolkit.Api.Hubs;
using Mobile.Remote.Toolkit.Business.Services;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Api.Services
{
    public class SignalRNotificationService : LogNotificationService
    {
        private readonly IHubContext<AndroidDeviceHub> _hubContext;

        public SignalRNotificationService(
            IHubContext<AndroidDeviceHub> hubContext,
            ILogger<SignalRNotificationService> logger) : base(logger)
        {
            _hubContext = hubContext;
        }

        public override async Task NotifyDeviceStatusChanged(string serial, Dictionary<string, object> status)
        {
            await base.NotifyDeviceStatusChanged(serial, status);
            await _hubContext.Clients.Group($"device_{serial}")
                .SendAsync("DeviceStatusChanged", status);
        }

        public override async Task NotifyDeviceConnected(AndroidDeviceResponse device)
        {
            await base.NotifyDeviceConnected(device);
            await _hubContext.Clients.All
                .SendAsync("DeviceConnected", device);
        }

        public override async Task NotifyDeviceDisconnected(string serial)
        {
            await base.NotifyDeviceDisconnected(serial);
            await _hubContext.Clients.All
                .SendAsync("DeviceDisconnected", serial);
        }

        public override async Task NotifyMirrorStarted(string serial)
        {
            await base.NotifyMirrorStarted(serial);
            await _hubContext.Clients.Group($"device_{serial}")
                .SendAsync("MirrorStarted", serial);
        }

        public override async Task NotifyMirrorStopped(string serial)
        {
            await base.NotifyMirrorStopped(serial);
            await _hubContext.Clients.Group($"device_{serial}")
                .SendAsync("MirrorStopped", serial);
        }

        public override async Task NotifyScreenshotTaken(string serial, string filename)
        {
            await base.NotifyScreenshotTaken(serial, filename);
            await _hubContext.Clients.Group($"device_{serial}")
                .SendAsync("ScreenshotTaken", new { serial, filename });
        }
    }
}

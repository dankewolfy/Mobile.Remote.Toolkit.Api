using Microsoft.AspNetCore.SignalR;

using Mobile.Remote.Toolkit.Business.Services.Android;

namespace Mobile.Remote.Toolkit.Api.Hubs
{
    public class AndroidDeviceHub : Hub
    {
        private readonly IAndroidDeviceService _androidService;

        public AndroidDeviceHub(IAndroidDeviceService androidService)
        {
            _androidService = androidService;
        }

        public async Task JoinDeviceGroup(string serial)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{serial}");
        }

        public async Task LeaveDeviceGroup(string serial)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{serial}");
        }

        public async Task GetDeviceStatus(string serial)
        {
            var status = await _androidService.GetDeviceStatusAsync(serial);
            await Clients.Caller.SendAsync("DeviceStatus", status);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}

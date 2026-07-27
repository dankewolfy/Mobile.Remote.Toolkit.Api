using Mobile.Remote.Toolkit.Application.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Application.Services
{
    public interface INotificationService
    {
        Task NotifyDeviceStatusChanged(string serial, Dictionary<string, object> status);
        Task NotifyDeviceConnected(AndroidDeviceResponse device);
        Task NotifyDeviceDisconnected(string serial);
        Task NotifyMirrorStarted(string serial);
        Task NotifyMirrorStopped(string serial);
        Task NotifyScreenshotTaken(string serial, string filename);
    }
}

using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;

namespace Mobile.Remote.Toolkit.Application.Services.iOS
{
    public interface IIOSDeviceService
    {
        Task<List<IOSDeviceResponse>> GetConnectedDevicesAsync();
        Task<IOSDeviceResponse> GetDeviceInfoAsync(string udid);
        Task<Dictionary<string, object>> GetDeviceStatusAsync(string udid);
        Task<ActionResponse> ExecuteActionAsync(string udid, string action, Dictionary<string, object> options, Dictionary<string, object> payload);
        Task<bool> IsMirrorActiveAsync(string udid);
        Task<ActionResponse> StartMirrorAsync(string udid, Dictionary<string, object> options);
        Task<ActionResponse> StopMirrorAsync(string udid);
        Task<ActionResponse> TakeScreenshotAsync(string udid, string filename = null);
        Task<List<IOSMirrorSessionResponse>> GetMirrorSessionsAsync();
    }
}

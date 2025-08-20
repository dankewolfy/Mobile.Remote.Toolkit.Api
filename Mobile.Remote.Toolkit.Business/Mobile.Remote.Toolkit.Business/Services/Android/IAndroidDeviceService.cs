using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public interface IAndroidDeviceService
    {
        Task<List<AndroidDeviceResponse>> GetConnectedDevicesAsync();
        Task<AndroidDeviceResponse> GetDeviceInfoAsync(string serial);
        Task<Dictionary<string, object>> GetDeviceStatusAsync(string serial);
        Task<ActionResponse> ExecuteActionAsync(string serial, string action, Dictionary<string, object> options, Dictionary<string, object> payload);
        Task<bool> IsMirrorActiveAsync(string serial);
        Task<ActionResponse> StartMirrorAsync(string serial, Dictionary<string, object> options);
        Task<ActionResponse> StopMirrorAsync(string serial);
        Task<ActionResponse> TakeScreenshotAsync(string serial, string filename);
        Task<ActionResponse> ExecuteAdbCommandAsync(string serial, string command);
        Task<ActionResponse> ExecuteScrcpyCommandAsync(string serial, string command);
    }
}

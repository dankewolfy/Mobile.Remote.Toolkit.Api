using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public interface IAndroidDeviceService
    {
        Task<AndroidDeviceResponse> GetDeviceInfoAsync(string serial);
        Task<List<AndroidDeviceResponse>> GetConnectedDeviceSerialsAsync();
        Task<List<AndroidDeviceResponse>> GetConnectedDeviceInfosAsync();
        Task<ActionResponse> StartMirrorAsync(string serial, Dictionary<string, object> options);
        Task<ActionResponse> StopMirrorAsync(string serial);
        Task<bool> IsMirrorActiveAsync(string serial);
        Task<Dictionary<string, object>> GetMirrorStatusAsync(string serial);
        Task<ActionResponse> TakeScreenshotAsync(string serial, string filename);
        Task<ProcessResultResponse> ExecuteAdbCommandAsync(string serial, string command);
        Task<ProcessResultResponse> ExecuteScrcpyCommandAsync(string serial, string command);
    }
}

using Mobile.Remote.Toolkit.Business.Models.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public interface IDeviceInfoProvider
    {
        Task<AndroidDeviceResponse> GetDeviceInfoAsync(string serial);
    Task<List<AndroidDeviceResponse>> GetConnectedDeviceSerialsAsync();
    }
}

using Mobile.Remote.Toolkit.Application.Models.Responses;

namespace Mobile.Remote.Toolkit.Application.Services.iOS
{
    public interface IIOSDeviceKitInstallService
    {
        Task<ActionResponse> InstallAsync(string udid);
    }
}

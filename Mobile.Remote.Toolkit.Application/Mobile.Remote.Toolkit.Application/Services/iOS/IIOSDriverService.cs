using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;

namespace Mobile.Remote.Toolkit.Application.Services.iOS
{
    public interface IIOSDriverService
    {
        Task<IOSDriverStatusResponse> GetStatusAsync();
        Task<ActionResponse> InstallAsync();
    }
}

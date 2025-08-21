using Mobile.Remote.Toolkit.Business.Models.Android;
using Mobile.Remote.Toolkit.Business.Models.Requests.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public interface IMirrorService
    {
        Task<ActionResponse> StartMirrorAsync(string serial, Dictionary<string, object> options);
        Task<ActionResponse> StopMirrorAsync(string serial);
        Task<bool> IsMirrorActiveAsync(string serial);
        Task<Dictionary<string, object>> GetMirrorStatusAsync(string serial);
    }
}

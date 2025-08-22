using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public interface IScreenshotService
    {
    Task<ActionResponse> TakeScreenshotAsync(string serial, string filename);
    }
}

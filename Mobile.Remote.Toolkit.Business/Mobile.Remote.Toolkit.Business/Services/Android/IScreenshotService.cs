using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public interface IScreenshotService
    {
        Task<ScreenshotResponse> TakeScreenshotAsync(string serial, string filename);
    }
}

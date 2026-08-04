using Mobile.Remote.Toolkit.Application.Models.Responses;

namespace Mobile.Remote.Toolkit.Application.Services.iOS
{
    public interface IIOSControlService
    {
        Task<bool> IsAvailableAsync(string udid);
        Task<ActionResponse> TapAsync(string udid, double x, double y);
        Task<ActionResponse> SwipeAsync(string udid, double fromX, double fromY, double toX, double toY, int? durationMs = null);
        Task<ActionResponse> SwipePathAsync(string udid, IReadOnlyList<(double X, double Y, double TimeOffsetSeconds)> points);
        Task<ActionResponse> LongPressAsync(string udid, double x, double y, int? durationMs = null);
        Task<ActionResponse> TypeTextAsync(string udid, string text);
        Task<ActionResponse> PressButtonAsync(string udid, string button);
    }
}

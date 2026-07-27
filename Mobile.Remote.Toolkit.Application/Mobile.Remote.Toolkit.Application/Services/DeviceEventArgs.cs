using Mobile.Remote.Toolkit.Application.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Application.Services
{
    public class DeviceEventArgs : EventArgs
    {
        public AndroidDeviceResponse Device { get; set; }
    }
}

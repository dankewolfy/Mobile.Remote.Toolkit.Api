using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Services
{
    public class DeviceEventArgs : EventArgs
    {
        public AndroidDeviceResponse Device { get; set; }
    }
}

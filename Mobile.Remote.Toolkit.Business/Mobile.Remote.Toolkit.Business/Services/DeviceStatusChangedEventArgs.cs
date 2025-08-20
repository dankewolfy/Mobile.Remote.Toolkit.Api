#nullable disable

namespace Mobile.Remote.Toolkit.Business.Services
{
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public string Serial { get; set; }
        public Dictionary<string, object> Status { get; set; }
    }
}

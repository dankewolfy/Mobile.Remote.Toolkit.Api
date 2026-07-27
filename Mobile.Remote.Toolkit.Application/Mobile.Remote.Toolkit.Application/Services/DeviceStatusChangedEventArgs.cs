namespace Mobile.Remote.Toolkit.Application.Services
{
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public string Serial { get; set; }
        public Dictionary<string, object> Status { get; set; }
    }
}

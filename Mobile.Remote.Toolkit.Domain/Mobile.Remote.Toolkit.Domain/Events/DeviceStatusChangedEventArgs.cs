namespace Mobile.Remote.Toolkit.Domain.Events
{
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public string Serial { get; set; }
        public Dictionary<string, object> Status { get; set; }
    }
}

using Mobile.Remote.Toolkit.Domain.Entities;

namespace Mobile.Remote.Toolkit.Domain.Events
{
    public class DeviceEventArgs : EventArgs
    {
        public Device Device { get; set; }
    }
}

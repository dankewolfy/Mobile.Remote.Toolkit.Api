namespace Mobile.Remote.Toolkit.Application.Services
{
    public interface IDeviceMonitoringService
    {
        Task StartMonitoringAsync();
        Task StopMonitoringAsync();
        bool IsMonitoring { get; }
        event EventHandler<DeviceEventArgs> DeviceConnected;
        event EventHandler<DeviceEventArgs> DeviceDisconnected;
        event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;
    }
}

#nullable disable

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Services
{
    public class DeviceMonitoringService : IDeviceMonitoringService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeviceMonitoringService> _logger;

        private Timer _monitoringTimer;
        private readonly ConcurrentDictionary<string, AndroidDeviceResponse> _lastKnownDevices = new();
        private readonly ConcurrentDictionary<string, bool> _lastKnownStatus = new();

        public bool IsMonitoring { get; private set; }

        public event EventHandler<DeviceEventArgs> DeviceConnected;
        public event EventHandler<DeviceEventArgs> DeviceDisconnected;
        public event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;

        public DeviceMonitoringService(IServiceProvider serviceProvider, ILogger<DeviceMonitoringService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartMonitoringAsync()
        {
            if (IsMonitoring) return;

            _logger.LogInformation("Iniciando monitoreo de dispositivos Android");

            await UpdateDeviceStatusAsync();

            _monitoringTimer = new Timer(async _ => await MonitorDevicesAsync(),
                null, TimeSpan.Zero, TimeSpan.FromSeconds(3));

            IsMonitoring = true;
        }

        public async Task StopMonitoringAsync()
        {
            if (!IsMonitoring) return;

            _logger.LogInformation("Deteniendo monitoreo de dispositivos Android");

            await Task.Run(() =>
            {
                _monitoringTimer?.Dispose();
                _monitoringTimer = null;
            });

            IsMonitoring = false;
        }

        private async Task MonitorDevicesAsync()
        {
            try
            {
                await UpdateDeviceStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el monitoreo de dispositivos");
            }
        }

        private async Task UpdateDeviceStatusAsync()
        {
            try
            {
                // Crear scope para obtener servicios
                using var scope = _serviceProvider.CreateScope();
                var androidService = scope.ServiceProvider.GetRequiredService<IAndroidDeviceService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var currentDevices = await androidService.GetConnectedDevicesAsync();
                var currentDeviceDict = currentDevices.ToDictionary(d => d.Serial, d => d);

                // Detectar dispositivos conectados
                foreach (var device in currentDevices)
                {
                    if (!_lastKnownDevices.ContainsKey(device.Serial))
                    {
                        _lastKnownDevices[device.Serial] = device;
                        _logger.LogInformation($"Dispositivo conectado: {device.Serial}");

                        DeviceConnected?.Invoke(this, new DeviceEventArgs { Device = device });
                        await notificationService.NotifyDeviceConnected(device);
                    }
                    else
                    {
                        // Verificar cambios de estado
                        var lastStatus = _lastKnownStatus.GetValueOrDefault(device.Serial, false);
                        if (lastStatus != device.Active)
                        {
                            _lastKnownStatus[device.Serial] = device.Active;

                            var status = await androidService.GetDeviceStatusAsync(device.Serial);
                            DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
                            {
                                Serial = device.Serial,
                                Status = status
                            });

                            await notificationService.NotifyDeviceStatusChanged(device.Serial, status);
                        }
                    }
                }

                // Detectar dispositivos desconectados
                var disconnectedDevices = _lastKnownDevices.Keys
                    .Where(serial => !currentDeviceDict.ContainsKey(serial))
                    .ToList();

                foreach (var serial in disconnectedDevices)
                {
                    var device = _lastKnownDevices[serial];
                    _lastKnownDevices.TryRemove(serial, out _);
                    _lastKnownStatus.TryRemove(serial, out _);

                    _logger.LogInformation($"Dispositivo desconectado: {serial}");

                    DeviceDisconnected?.Invoke(this, new DeviceEventArgs { Device = device });
                    await notificationService.NotifyDeviceDisconnected(serial);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando estado de dispositivos");
            }
        }

        public void Dispose()
        {
            _monitoringTimer?.Dispose();
        }
    }
}

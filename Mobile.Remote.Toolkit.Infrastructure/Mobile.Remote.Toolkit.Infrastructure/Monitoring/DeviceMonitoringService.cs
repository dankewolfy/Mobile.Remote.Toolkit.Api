using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Mobile.Remote.Toolkit.Application.Services;
using Mobile.Remote.Toolkit.Application.Services.Android;
using Mobile.Remote.Toolkit.Application.Models.Responses.Android;
using Mobile.Remote.Toolkit.Domain.Entities;
using Mobile.Remote.Toolkit.Domain.Events;

namespace Mobile.Remote.Toolkit.Infrastructure.Monitoring
{
    /// <summary>
    /// Monitorea dispositivos Android escuchando eventos USB del sistema operativo (vía IUsbHardwareWatcher).
    /// Solo ejecuta "adb devices" cuando el watcher notifica un cambio de hardware — nunca en ciclo continuo.
    /// </summary>
    public class DeviceMonitoringService : IDeviceMonitoringService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUsbHardwareWatcher _usbWatcher;
        private readonly ILogger<DeviceMonitoringService> _logger;

        private readonly ConcurrentDictionary<string, AndroidDeviceResponse> _lastKnownDevices = new();

        // Semáforo para evitar ejecuciones solapadas si llegan dos eventos casi simultáneos
        private readonly SemaphoreSlim _updateLock = new(1, 1);

        public bool IsMonitoring { get; private set; }

        public event EventHandler<DeviceEventArgs>? DeviceConnected;
        public event EventHandler<DeviceEventArgs>? DeviceDisconnected;
        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

        public DeviceMonitoringService(
            IServiceProvider serviceProvider,
            IUsbHardwareWatcher usbWatcher,
            ILogger<DeviceMonitoringService> logger)
        {
            _serviceProvider = serviceProvider;
            _usbWatcher = usbWatcher;
            _logger = logger;
        }

        public async Task StartMonitoringAsync()
        {
            if (IsMonitoring) return;

            _logger.LogInformation("Iniciando monitoreo de dispositivos Android (modo evento USB)");

            // Snapshot inicial: saber qué está conectado ahora mismo
            await RefreshDeviceListAsync();

            _usbWatcher.Start(RefreshDeviceListAsync);

            IsMonitoring = true;
        }

        public Task StopMonitoringAsync()
        {
            if (!IsMonitoring) return Task.CompletedTask;

            _logger.LogInformation("Deteniendo monitoreo de dispositivos Android");
            _usbWatcher.Stop();
            _lastKnownDevices.Clear();
            IsMonitoring = false;
            return Task.CompletedTask;
        }

        // ── Lógica de comparación ──────────────────────────────────────────────

        /// <summary>
        /// Ejecuta "adb devices" una vez, compara con el estado anterior y notifica diferencias.
        /// </summary>
        private async Task RefreshDeviceListAsync()
        {
            // Evitar ejecuciones solapadas si llegan dos eventos USB rápido
            if (!await _updateLock.WaitAsync(0))
            {
                _logger.LogDebug("RefreshDeviceList ya en ejecución, descartando evento duplicado");
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var androidService = scope.ServiceProvider.GetRequiredService<IAndroidDeviceService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var currentDevices = await androidService.GetConnectedDevicesAsync();
                var currentDict = currentDevices.ToDictionary(d => d.Serial, d => d);

                // Nuevos dispositivos
                foreach (var device in currentDevices)
                {
                    if (!_lastKnownDevices.ContainsKey(device.Serial))
                    {
                        _lastKnownDevices[device.Serial] = device;
                        _logger.LogInformation($"Dispositivo Android detectado: {device.Name} ({device.Serial})");
                        DeviceConnected?.Invoke(this, new DeviceEventArgs { Device = ToDomainDevice(device) });
                        await notificationService.NotifyDeviceConnected(device);
                    }
                }

                // Dispositivos que ya no aparecen
                var removed = _lastKnownDevices.Keys
                    .Where(s => !currentDict.ContainsKey(s))
                    .ToList();

                foreach (var serial in removed)
                {
                    _lastKnownDevices.TryRemove(serial, out var device);
                    _logger.LogInformation($"Dispositivo Android desconectado: {serial}");
                    DeviceDisconnected?.Invoke(this, new DeviceEventArgs { Device = ToDomainDevice(device!) });
                    await notificationService.NotifyDeviceDisconnected(serial);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al refrescar lista de dispositivos");
            }
            finally
            {
                _updateLock.Release();
            }
        }

        public void Dispose()
        {
            _usbWatcher.Dispose();
            _updateLock.Dispose();
        }

        private static Device ToDomainDevice(AndroidDeviceResponse response) => new()
        {
            Serial = response.Serial,
            Platform = response.Platform,
            Name = response.Name,
            Active = response.Active,
        };
    }
}

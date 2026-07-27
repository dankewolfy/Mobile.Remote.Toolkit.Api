using System.Collections.Concurrent;
using System.Management;
using System.Runtime.InteropServices;

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
    /// Monitorea dispositivos Android escuchando eventos USB del sistema operativo (WMI en Windows).
    /// Solo ejecuta "adb devices" cuando el SO notifica un cambio de hardware — nunca en ciclo continuo.
    /// </summary>
    public class DeviceMonitoringService : IDeviceMonitoringService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeviceMonitoringService> _logger;

        private readonly ConcurrentDictionary<string, AndroidDeviceResponse> _lastKnownDevices = new();

        // WMI watchers (solo Windows)
        private ManagementEventWatcher? _connectWatcher;
        private ManagementEventWatcher? _disconnectWatcher;

        // Semáforo para evitar ejecuciones solapadas si llegan dos eventos casi simultáneos
        private readonly SemaphoreSlim _updateLock = new(1, 1);

        public bool IsMonitoring { get; private set; }

        public event EventHandler<DeviceEventArgs>? DeviceConnected;
        public event EventHandler<DeviceEventArgs>? DeviceDisconnected;
        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

        public DeviceMonitoringService(IServiceProvider serviceProvider, ILogger<DeviceMonitoringService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartMonitoringAsync()
        {
            if (IsMonitoring) return;

            _logger.LogInformation("Iniciando monitoreo de dispositivos Android (modo evento USB)");

            // Snapshot inicial: saber qué está conectado ahora mismo
            await RefreshDeviceListAsync();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                StartWmiWatchers();
            }
            else
            {
                _logger.LogWarning("Monitoreo USB basado en eventos solo disponible en Windows. " +
                                   "En otros OS usa la opción de refresco manual.");
            }

            IsMonitoring = true;
        }

        public Task StopMonitoringAsync()
        {
            if (!IsMonitoring) return Task.CompletedTask;

            _logger.LogInformation("Deteniendo monitoreo de dispositivos Android");
            StopWmiWatchers();
            _lastKnownDevices.Clear();
            IsMonitoring = false;
            return Task.CompletedTask;
        }

        // ── WMI ────────────────────────────────────────────────────────────────

        private void StartWmiWatchers()
        {
            try
            {
                // Win32_DeviceChangeEvent: EventType 2 = device arrived, 3 = device removed
                var query = new WqlEventQuery(
                    "SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");

                _connectWatcher = new ManagementEventWatcher(query);
                _connectWatcher.EventArrived += OnUsbHardwareChanged;
                _connectWatcher.Start();

                // Un solo watcher es suficiente; usamos el mismo handler para ambos tipos
                _logger.LogInformation("WMI USB watcher iniciado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error iniciando WMI watcher; el monitoreo USB no estará activo");
            }
        }

        private void StopWmiWatchers()
        {
            try { _connectWatcher?.Stop(); _connectWatcher?.Dispose(); } catch { }
            try { _disconnectWatcher?.Stop(); _disconnectWatcher?.Dispose(); } catch { }
            _connectWatcher = null;
            _disconnectWatcher = null;
        }

        /// <summary>
        /// Llamado por WMI cuando algún dispositivo USB se conecta o desconecta.
        /// Ejecuta adb devices UNA SOLA VEZ para ver si el cambio afecta a Android.
        /// </summary>
        private void OnUsbHardwareChanged(object sender, EventArrivedEventArgs e)
        {
            var eventType = e.NewEvent.Properties["EventType"]?.Value;
            _logger.LogDebug($"Evento USB del sistema (EventType={eventType}); consultando adb...");

            // Ejecutar de forma asíncrona sin bloquear el thread de WMI
            _ = Task.Run(async () =>
            {
                // Esperar brevemente para dejar que el OS registre el dispositivo con ADB
                await Task.Delay(1200);
                await RefreshDeviceListAsync();
            });
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
            StopWmiWatchers();
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

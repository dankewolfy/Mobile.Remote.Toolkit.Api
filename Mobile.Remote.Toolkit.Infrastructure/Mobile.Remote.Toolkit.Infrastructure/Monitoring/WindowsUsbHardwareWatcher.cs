using System.Management;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Services;

namespace Mobile.Remote.Toolkit.Infrastructure.Monitoring
{
    /// <summary>
    /// Escucha Win32_DeviceChangeEvent vía WMI para detectar conexión/desconexión de hardware USB.
    /// Solo dispara el callback cuando el SO notifica un cambio — nunca en ciclo continuo.
    /// </summary>
    public class WindowsUsbHardwareWatcher : IUsbHardwareWatcher
    {
        private readonly ILogger<WindowsUsbHardwareWatcher> _logger;
        private ManagementEventWatcher? _watcher;
        private Func<Task>? _onHardwareChanged;

        public WindowsUsbHardwareWatcher(ILogger<WindowsUsbHardwareWatcher> logger)
        {
            _logger = logger;
        }

        public void Start(Func<Task> onHardwareChanged)
        {
            _onHardwareChanged = onHardwareChanged;

            try
            {
                // Win32_DeviceChangeEvent: EventType 2 = device arrived, 3 = device removed
                var query = new WqlEventQuery(
                    "SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");

                _watcher = new ManagementEventWatcher(query);
                _watcher.EventArrived += OnUsbHardwareChanged;
                _watcher.Start();

                _logger.LogInformation("WMI USB watcher iniciado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error iniciando WMI watcher; el monitoreo USB no estará activo");
            }
        }

        public void Stop()
        {
            try { _watcher?.Stop(); _watcher?.Dispose(); } catch { }
            _watcher = null;
        }

        private void OnUsbHardwareChanged(object sender, EventArrivedEventArgs e)
        {
            var eventType = e.NewEvent.Properties["EventType"]?.Value;
            _logger.LogDebug($"Evento USB del sistema (EventType={eventType}); consultando adb...");

            // Ejecutar de forma asíncrona sin bloquear el thread de WMI
            _ = Task.Run(async () =>
            {
                // Esperar brevemente para dejar que el OS registre el dispositivo con ADB
                await Task.Delay(1200);
                await _onHardwareChanged!();
            });
        }

        public void Dispose() => Stop();
    }
}

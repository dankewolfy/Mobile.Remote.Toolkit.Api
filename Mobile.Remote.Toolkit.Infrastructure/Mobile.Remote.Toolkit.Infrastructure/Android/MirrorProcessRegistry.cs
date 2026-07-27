using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Services;

namespace Mobile.Remote.Toolkit.Infrastructure.Android
{
    /// <summary>
    /// Singleton que mantiene el mapa serial → proceso scrcpy activo.
    /// Al ser singleton sobrevive entre peticiones HTTP, a diferencia de AndroidDeviceService (Scoped).
    /// </summary>
    public class MirrorProcessRegistry
    {
        private readonly ConcurrentDictionary<string, Process> _processes = new();
        private readonly INotificationService _notificationService;
        private readonly ILogger<MirrorProcessRegistry> _logger;

        public MirrorProcessRegistry(INotificationService notificationService, ILogger<MirrorProcessRegistry> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Registra el proceso para un serial y suscribe al evento Exited para detectar
        /// cuando el usuario cierra la ventana de scrcpy directamente.
        /// </summary>
        public void Register(string serial, Process process)
        {
            // Habilitar la notificación del evento Exited
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) =>
            {
                _logger.LogInformation("[Mirror] Proceso scrcpy cerrado externamente para {Serial} (PID={Pid})", serial, process.Id);
                _processes.TryRemove(serial, out _);
                // Notificar al frontend vía SignalR (fire-and-forget)
                _ = _notificationService.NotifyMirrorStopped(serial);
            };
            _processes[serial] = process;
        }

        /// <summary>Devuelve el proceso si existe y sigue vivo, null en caso contrario.</summary>
        public Process? GetAlive(string serial)
        {
            if (_processes.TryGetValue(serial, out var p) && !p.HasExited)
                return p;
            return null;
        }

        /// <summary>Indica si ya hay un mirror activo para el serial.</summary>
        public bool IsActive(string serial)
            => GetAlive(serial) != null;

        /// <summary>Elimina la entrada del serial y devuelve el proceso si existía.</summary>
        public Process? Remove(string serial)
        {
            _processes.TryRemove(serial, out var p);
            return p;
        }
    }
}

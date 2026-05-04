using System.Collections.Concurrent;
using System.Diagnostics;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    /// <summary>
    /// Singleton que mantiene el mapa serial → proceso scrcpy activo.
    /// Al ser singleton sobrevive entre peticiones HTTP, a diferencia de AndroidDeviceService (Scoped).
    /// </summary>
    public class MirrorProcessRegistry
    {
        private readonly ConcurrentDictionary<string, Process> _processes = new();

        /// <summary>Registra o reemplaza el proceso para un serial.</summary>
        public void Register(string serial, Process process)
            => _processes[serial] = process;

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

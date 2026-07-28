using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Services;

namespace Mobile.Remote.Toolkit.Infrastructure.Monitoring
{
    /// <summary>
    /// Fallback multiplataforma: sin un mecanismo de eventos USB nativo (como WMI en Windows),
    /// dispara el callback de refresco por polling periódico en vez de quedar inactivo.
    /// </summary>
    public class PollingUsbHardwareWatcher : IUsbHardwareWatcher
    {
        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

        private readonly ILogger<PollingUsbHardwareWatcher> _logger;
        private Timer? _timer;
        private Func<Task>? _onHardwareChanged;

        public PollingUsbHardwareWatcher(ILogger<PollingUsbHardwareWatcher> logger)
        {
            _logger = logger;
        }

        public void Start(Func<Task> onHardwareChanged)
        {
            _onHardwareChanged = onHardwareChanged;
            _logger.LogInformation(
                "Sin eventos USB nativos en este SO; usando polling cada {Interval}s",
                PollingInterval.TotalSeconds);

            _timer = new Timer(_ => _ = _onHardwareChanged(), null, PollingInterval, PollingInterval);
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose() => Stop();
    }
}

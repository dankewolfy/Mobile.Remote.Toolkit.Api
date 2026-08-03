using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Utils;

namespace Mobile.Remote.Toolkit.Infrastructure.iOS
{
    // go-ios necesita un tunel activo para hablar con los servicios de developer de iOS 17+
    // (mismo requisito que pymobiledevice3), pero a diferencia de este ultimo, "ios tunnel start
    // --userspace" no pide privilegios de administrador - confirmado en vivo con un iPad real,
    // sin ningun prompt de UAC. El tunel es un proceso de fondo unico compartido por todas las
    // sesiones de mirror (no una por dispositivo), asi que este manager solo se asegura de que
    // haya uno vivo antes de arrancar "ios screenshot --stream".
    public class GoIosTunnelManager
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly ILogger<GoIosTunnelManager> _logger;
        private Process? _tunnelProcess;

        public GoIosTunnelManager(ILogger<GoIosTunnelManager> logger)
        {
            _logger = logger;
        }

        public async Task EnsureRunningAsync(IProcessHelper processHelper, string executable)
        {
            await _lock.WaitAsync();
            try
            {
                if (_tunnelProcess is { HasExited: false })
                    return;

                _logger.LogInformation("[go-ios] Iniciando tunel (tunnel start --userspace)");
                _tunnelProcess = await processHelper.StartBackgroundProcessAsync(executable, "tunnel start --userspace");

                // El tunel se negocia casi instantaneo contra un iPad real (~100ms medido), pero se
                // deja un margen fijo para no salir a arrancar "screenshot --stream" en una carrera
                // contra el tunel todavia inicializandose.
                await Task.Delay(1500);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}

using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Utils;

namespace Mobile.Remote.Toolkit.Infrastructure.iOS
{
    // "ios ui run devicekit" (backend --driver=devicekit de go-ios) sirve el h264 real de hardware
    // Y el JSON-RPC de control tactil desde el mismo puerto local - a diferencia del tunel
    // (GoIosTunnelManager, compartido por todos los dispositivos), este proceso esta atado a un solo
    // udid a la vez: si se pide un udid distinto al que ya esta corriendo, se reinicia el proceso.
    public class GoIosDeviceKitManager
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly ILogger<GoIosDeviceKitManager> _logger;
        private Process? _process;
        private string? _activeUdid;

        public GoIosDeviceKitManager(ILogger<GoIosDeviceKitManager> logger)
        {
            _logger = logger;
        }

        public bool IsRunningFor(string udid)
            => _process is { HasExited: false } && string.Equals(_activeUdid, udid, StringComparison.OrdinalIgnoreCase);

        public async Task<Process> EnsureRunningAsync(IProcessHelper processHelper, string executable, string bundleId, string udid)
        {
            await _lock.WaitAsync();
            try
            {
                if (IsRunningFor(udid))
                    return _process!;

                if (_process is { HasExited: false })
                {
                    _logger.LogInformation("[DeviceKit] Cambiando de dispositivo activo ({Old} -> {New}); deteniendo proceso anterior", _activeUdid, udid);
                    _process.Kill();
                    _process.Dispose();
                }

                _logger.LogInformation("[DeviceKit] Iniciando 'ui run devicekit' para {Udid}", udid);
                _process = await processHelper.StartBackgroundProcessAsync(executable, $"ui run devicekit --bundleid={bundleId} --udid={udid}");
                _activeUdid = udid;

                // Igual que el tunel: el servidor HTTP local (rpc + h264) tarda un momento en quedar
                // listo despues de arrancar el proceso.
                await Task.Delay(2000);

                return _process;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<ProcessResult> InstallAppAsync(IProcessHelper processHelper, string executable, string ipaPath, string udid)
        {
            _logger.LogInformation("[DeviceKit] Instalando {IpaPath} en {Udid}", ipaPath, udid);
            return await processHelper.ExecuteCommandAsync(executable, $"install --path=\"{ipaPath}\" --udid={udid}", timeoutSeconds: 120);
        }

        public void StopIfRunningFor(string udid)
        {
            if (!IsRunningFor(udid))
                return;

            if (!_process!.HasExited)
                _process.Kill();

            _activeUdid = null;
            _process = null;
        }
    }
}

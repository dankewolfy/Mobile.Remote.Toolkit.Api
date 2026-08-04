using System.ComponentModel;
using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Infrastructure.iOS
{
    // El driver "Apple Mobile Device Service" es lo que Windows necesita para reconocer el
    // iPhone por USB (independiente de go-ios/libimobiledevice, que solo hablan con el
    // dispositivo una vez el SO ya lo expone). No lo empaquetamos nosotros: Apple no distribuye
    // ese instalador para redistribución de terceros, así que lo pedimos vía winget en el
    // momento (paquete Apple.AppleMobileDeviceSupport), que resuelve descarga + dependencias.
    public class WindowsIosDriverService : IIOSDriverService
    {
        private const string ServiceRegistryKey = @"SYSTEM\CurrentControlSet\Services\Apple Mobile Device Service";
        private const string WingetPackageId = "Apple.AppleMobileDeviceSupport";

        private readonly ILogger<WindowsIosDriverService> _logger;

        public WindowsIosDriverService(ILogger<WindowsIosDriverService> logger)
        {
            _logger = logger;
        }

        public Task<IOSDriverStatusResponse> GetStatusAsync()
        {
            var installed = IsServiceRegistered();

            return Task.FromResult(new IOSDriverStatusResponse
            {
                Installed = installed,
                Supported = true,
                Message = installed
                    ? "Driver de Apple detectado."
                    : "No se detectó el driver de Apple necesario para conectar un iPhone por USB."
            });
        }

        public async Task<ActionResponse> InstallAsync()
        {
            // Elevación acotada a este único comando (UAC puntual), no al proceso completo de la API.
            var startInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = $"install -e --id {WingetPackageId} --silent --accept-package-agreements --accept-source-agreements",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = false
            };

            try
            {
                _logger.LogInformation("Instalando driver de Apple via winget (elevado)");

                using var process = Process.Start(startInfo);
                await process!.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("winget terminó con código {ExitCode} instalando {PackageId}", process.ExitCode, WingetPackageId);
                    return new ActionResponse
                    {
                        Success = false,
                        Message = "No se pudo instalar el driver de Apple.",
                        Error = $"winget salió con código {process.ExitCode}"
                    };
                }

                var installed = IsServiceRegistered();
                return new ActionResponse
                {
                    Success = installed,
                    Message = installed
                        ? "Driver de Apple instalado correctamente. Reconecta el iPhone."
                        : "winget completó la instalación pero el driver aún no se detecta. Reconecta el iPhone o reinicia el equipo."
                };
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                _logger.LogInformation("Instalación del driver de Apple cancelada por el usuario (UAC)");
                return new ActionResponse
                {
                    Success = false,
                    Message = "Instalación cancelada: se rechazó el permiso de administrador."
                };
            }
            catch (Win32Exception ex)
            {
                _logger.LogError(ex, "No se pudo iniciar winget para instalar el driver de Apple");
                return new ActionResponse
                {
                    Success = false,
                    Message = "No se encontró winget en este equipo. Instala \"Instalador de aplicaciones\" desde Microsoft Store e inténtalo de nuevo.",
                    Error = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado instalando el driver de Apple");
                return new ActionResponse
                {
                    Success = false,
                    Message = "Ocurrió un error instalando el driver de Apple.",
                    Error = ex.Message
                };
            }
        }

        private static bool IsServiceRegistered()
        {
            using var key = Registry.LocalMachine.OpenSubKey(ServiceRegistryKey);
            return key != null;
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Services.iOS;
using Mobile.Remote.Toolkit.Application.Utils;

namespace Mobile.Remote.Toolkit.Infrastructure.iOS
{
    // Orquesta el instalador de un click de DeviceKit: enrolar el UDID en App Store Connect si
    // hace falta, regenerar el provisioning profile ad-hoc, re-firmar el .ipa base en el Mac
    // remoto, e instalarlo en el dispositivo via go-ios. Ver receta de la Fase 9 - esto reemplaza
    // el paso manual "parear con una Mac+Xcode reales" que antes había que hacer por dispositivo.
    public class IOSDeviceKitInstallService : IIOSDeviceKitInstallService
    {
        private static readonly HttpClient HttpClient = new();

        private readonly IAppleAppStoreConnectClient _appleClient;
        private readonly IMacSigningService _macSigningService;
        private readonly GoIosDeviceKitManager _deviceKitManager;
        private readonly IProcessHelper _processHelper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IOSDeviceKitInstallService> _logger;

        public IOSDeviceKitInstallService(
            IAppleAppStoreConnectClient appleClient,
            IMacSigningService macSigningService,
            GoIosDeviceKitManager deviceKitManager,
            IProcessHelper processHelper,
            IConfiguration configuration,
            ILogger<IOSDeviceKitInstallService> logger)
        {
            _appleClient = appleClient;
            _macSigningService = macSigningService;
            _deviceKitManager = deviceKitManager;
            _processHelper = processHelper;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ActionResponse> InstallAsync(string udid)
        {
            var executable = _configuration["IOS:DeviceKit:Executable"] ?? _configuration["IOS:Mirror:GoIosExecutable"];
            if (string.IsNullOrWhiteSpace(executable))
            {
                return new ActionResponse
                {
                    Success = false,
                    Message = "DeviceKit no se pudo instalar",
                    Error = "Configure IOS:DeviceKit:Executable o IOS:Mirror:GoIosExecutable en appsettings."
                };
            }

            // Camino rapido - no requiere App Store Connect ni el Mac remoto, asi que cualquier
            // maquina del equipo (sin ninguna credencial configurada) puede usarlo tal cual: si no
            // hay .ipa pre-firmado en cache todavia, se descarga del GitHub Release publico (no
            // requiere token, el repo es publico); si el dispositivo ya esta en su lista de UDIDs,
            // instala directo. Solo cae al flujo completo (mas abajo, ese si necesita credenciales)
            // si el dispositivo es nuevo y "ios install" lo rechaza por provisioning profile invalido.
            var preSignedIpaPath = ExpandPath(_configuration["IOS:DeviceKit:PreSignedIpaPath"]);
            if (!string.IsNullOrWhiteSpace(preSignedIpaPath))
            {
                if (!File.Exists(preSignedIpaPath))
                    await TryDownloadPreSignedIpaAsync(preSignedIpaPath);

                if (File.Exists(preSignedIpaPath))
                {
                    _logger.LogInformation("[DeviceKit] Probando instalar con el .ipa ya firmado (sin App Store Connect)");
                    var quickInstallResult = await _deviceKitManager.InstallAppAsync(_processHelper, executable, preSignedIpaPath, udid);
                    if (quickInstallResult.Success)
                    {
                        return new ActionResponse
                        {
                            Success = true,
                            Message = "DeviceKit instalado correctamente (ipa ya firmado)",
                            Data = new Dictionary<string, object> { ["udid"] = udid }
                        };
                    }

                    _logger.LogInformation(
                        "[DeviceKit] El .ipa ya firmado no sirvio para {Udid} (probablemente dispositivo nuevo) - probando el flujo completo: {Error}",
                        udid, quickInstallResult.Error);
                }
            }

            // A partir de aqui hace falta el flujo completo (enrolar+firmar) - esto SI requiere
            // App Store Connect y el Mac remoto, solo pasa en la maquina "admin" que enrola
            // dispositivos nuevos, no en cada instalacion del equipo.
            // IMPORTANTE: el .ipa base debe ser devicekit-ios 0.0.18 o anterior (release de
            // mobile-next/devicekit-ios) - la 0.0.19 (2026-06-14) saco el streaming H264 a otro
            // repo, dejando solo MJPEG en este binario. Un .ipa post-0.0.19 aca hace que
            // /h264 devuelva 404 mientras tap/swipe/RPC siguen funcionando normal (confirmado
            // con "strings" sobre el binario: post-0.0.19 solo trae simbolos MJPEG/Streamer,
            // sin H264HTTPHandler). No hay una alternativa mas nueva con H264 integrado todavia.
            var baseIpaPath = ExpandPath(_configuration["IOS:DeviceKit:BaseIpaPath"]);
            if (string.IsNullOrWhiteSpace(baseIpaPath) || !File.Exists(baseIpaPath))
            {
                return new ActionResponse
                {
                    Success = false,
                    Message = "DeviceKit no se pudo instalar",
                    Error = "Este dispositivo no esta en el .ipa pre-firmado todavia y necesita enrolarse. " +
                        "Configure IOS:DeviceKit:BaseIpaPath en appsettings apuntando al .ipa base de DeviceKit " +
                        "(fuera del repo, ver %AppData%\\MobileRemoteToolkit) - usar devicekit-ios 0.0.18 o anterior. " +
                        "Esto solo hace falta en la maquina que enrola dispositivos nuevos."
                };
            }

            try
            {
                if (!await _appleClient.IsDeviceRegisteredAsync(udid))
                {
                    _logger.LogInformation("[DeviceKit] {Udid} no estaba registrado en Apple Developer, registrando", udid);
                    await _appleClient.RegisterDeviceAsync(udid, $"MobileRemoteToolkit {udid}");
                }

                _logger.LogInformation("[DeviceKit] Regenerando provisioning profile ad-hoc");
                var profile = await _appleClient.RegenerateDeviceKitProfileAsync();

                var baseIpa = await File.ReadAllBytesAsync(baseIpaPath);

                _logger.LogInformation("[DeviceKit] Re-firmando .ipa en el Mac remoto");
                var signedIpa = await _macSigningService.ResignIpaAsync(baseIpa, profile);

                var tempIpaPath = Path.Combine(Path.GetTempPath(), $"DeviceKit.{udid}.ipa");
                await File.WriteAllBytesAsync(tempIpaPath, signedIpa);

                try
                {
                    var installResult = await _deviceKitManager.InstallAppAsync(_processHelper, executable, tempIpaPath, udid);
                    if (!installResult.Success)
                    {
                        return new ActionResponse
                        {
                            Success = false,
                            Message = "DeviceKit no se pudo instalar",
                            Error = $"'ios install' termino con error: {installResult.Error}"
                        };
                    }
                }
                finally
                {
                    File.Delete(tempIpaPath);
                }

                // El profile recien regenerado incluye TODOS los dispositivos habilitados (no solo
                // este), asi que el .ipa recien firmado sirve como el nuevo "camino rapido" para
                // cualquiera de ellos - se sobreescribe el cache para que la proxima instalacion
                // (incluida la de este mismo dispositivo) no vuelva a tocar App Store Connect/Mac.
                if (!string.IsNullOrWhiteSpace(preSignedIpaPath))
                {
                    await File.WriteAllBytesAsync(preSignedIpaPath, signedIpa);
                    _logger.LogInformation("[DeviceKit] Cache de .ipa pre-firmado actualizado en {Path}", preSignedIpaPath);
                }

                return new ActionResponse
                {
                    Success = true,
                    Message = "DeviceKit instalado correctamente",
                    Data = new Dictionary<string, object> { ["udid"] = udid }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DeviceKit] Fallo instalando DeviceKit en {Udid}", udid);
                return new ActionResponse
                {
                    Success = false,
                    Message = "DeviceKit no se pudo instalar",
                    Error = ex.Message
                };
            }
        }

        private async Task TryDownloadPreSignedIpaAsync(string destinationPath)
        {
            var downloadUrl = _configuration["IOS:DeviceKit:PreSignedIpaDownloadUrl"];
            if (string.IsNullOrWhiteSpace(downloadUrl))
                return;

            try
            {
                _logger.LogInformation("[DeviceKit] Descargando .ipa pre-firmado desde {Url}", downloadUrl);
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var bytes = await HttpClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(destinationPath, bytes);
                _logger.LogInformation("[DeviceKit] .ipa pre-firmado descargado ({Bytes} bytes)", bytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DeviceKit] No se pudo descargar el .ipa pre-firmado desde {Url} - se intentara el flujo completo", downloadUrl);
            }
        }

        private static string ExpandPath(string? path)
            => string.IsNullOrWhiteSpace(path) ? path : Environment.ExpandEnvironmentVariables(path);
    }
}

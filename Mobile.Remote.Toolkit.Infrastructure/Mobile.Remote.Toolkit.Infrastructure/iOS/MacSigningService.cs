using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Services.iOS;

using Renci.SshNet;

namespace Mobile.Remote.Toolkit.Infrastructure.iOS
{
    // Dispara el re-firmado del .ipa de DeviceKit en el Mac remoto (ya preparado con Xcode y el
    // certificado de firma) por SSH/SFTP - el .p12/certificado nunca sale del Mac, solo viajan el
    // .ipa base y el .mobileprovision nuevo. No existia ninguna automatizacion headless para esto
    // (solo pairing manual desde Visual Studio); el script real (resign-devicekit.sh) vive en el
    // Mac, no en este repo.
    public class MacSigningService : IMacSigningService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MacSigningService> _logger;

        public MacSigningService(IConfiguration configuration, ILogger<MacSigningService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<byte[]> ResignIpaAsync(byte[] baseIpa, byte[] provisioningProfile)
        {
            var host = _configuration["IOS:DeviceKit:MacSigner:Host"];
            var username = _configuration["IOS:DeviceKit:MacSigner:Username"];
            var privateKeyPath = ExpandPath(_configuration["IOS:DeviceKit:MacSigner:PrivateKeyPath"]);
            var remoteWorkDir = _configuration["IOS:DeviceKit:MacSigner:RemoteWorkDir"];
            var remoteScriptPath = _configuration["IOS:DeviceKit:MacSigner:RemoteScriptPath"];
            var port = int.TryParse(_configuration["IOS:DeviceKit:MacSigner:Port"], out var configuredPort) ? configuredPort : 22;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(privateKeyPath)
                || string.IsNullOrWhiteSpace(remoteWorkDir) || string.IsNullOrWhiteSpace(remoteScriptPath))
            {
                throw new InvalidOperationException(
                    "Configure IOS:DeviceKit:MacSigner:Host/Username/PrivateKeyPath/RemoteWorkDir/RemoteScriptPath en appsettings " +
                    "(la llave privada SSH vive fuera del repo, ver %AppData%\\MobileRemoteToolkit).");
            }

            if (!File.Exists(privateKeyPath))
                throw new InvalidOperationException($"No se encontro la llave privada SSH en '{privateKeyPath}'.");

            using var keyFile = new PrivateKeyFile(privateKeyPath);
            var connectionInfo = new ConnectionInfo(host, port, username, new PrivateKeyAuthenticationMethod(username, keyFile));

            var remoteIpaPath = $"{remoteWorkDir}/DeviceKit.ipa";
            var remoteProfilePath = $"{remoteWorkDir}/profile.mobileprovision";
            var remoteSignedIpaPath = $"{remoteWorkDir}/DeviceKit.signed.ipa";

            _logger.LogInformation("[MacSigner] Subiendo .ipa base y provisioning profile a {Host}", host);
            using (var sftp = new SftpClient(connectionInfo))
            {
                sftp.Connect();
                UploadStream(sftp, remoteIpaPath, baseIpa);
                UploadStream(sftp, remoteProfilePath, provisioningProfile);
                sftp.Disconnect();
            }

            _logger.LogInformation("[MacSigner] Ejecutando {Script} por SSH", remoteScriptPath);
            using (var ssh = new SshClient(connectionInfo))
            {
                ssh.Connect();
                var command = ssh.CreateCommand($"'{remoteScriptPath}' '{remoteIpaPath}' '{remoteProfilePath}' '{remoteSignedIpaPath}'");
                var result = await Task.Run(() => command.Execute());

                if (command.ExitStatus != 0)
                {
                    throw new InvalidOperationException(
                        $"El script de re-firmado en el Mac termino con error (exit {command.ExitStatus}): {command.Error}");
                }

                _logger.LogInformation("[MacSigner] Re-firmado exitoso: {Output}", result);
                ssh.Disconnect();
            }

            _logger.LogInformation("[MacSigner] Descargando .ipa firmado");
            using var sftpDownload = new SftpClient(connectionInfo);
            sftpDownload.Connect();
            using var downloadStream = new MemoryStream();
            sftpDownload.DownloadFile(remoteSignedIpaPath, downloadStream);
            sftpDownload.Disconnect();

            return downloadStream.ToArray();
        }

        private static void UploadStream(SftpClient sftp, string remotePath, byte[] content)
        {
            using var stream = new MemoryStream(content);
            sftp.UploadFile(stream, remotePath, canOverride: true);
        }

        private static string ExpandPath(string? path)
            => string.IsNullOrWhiteSpace(path) ? path : Environment.ExpandEnvironmentVariables(path);
    }
}

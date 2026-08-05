using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Infrastructure.iOS
{
    // Cliente minimo de la App Store Connect API (Devices + Profiles), autenticado con un JWT
    // ES256 firmado con la .p8 (kid=KeyId, iss=IssuerId) - permite registrar un UDID nuevo y
    // regenerar el provisioning profile ad-hoc sin abrir Xcode (ver receta de la Fase 9).
    public class AppleAppStoreConnectClient : IAppleAppStoreConnectClient
    {
        private const string BaseUrl = "https://api.appstoreconnect.apple.com/v1";
        private static readonly HttpClient HttpClient = new();

        private readonly IConfiguration _configuration;
        private readonly ILogger<AppleAppStoreConnectClient> _logger;

        public AppleAppStoreConnectClient(IConfiguration configuration, ILogger<AppleAppStoreConnectClient> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> IsDeviceRegisteredAsync(string udid)
        {
            using var response = await SendAsync(HttpMethod.Get, $"{BaseUrl}/devices?filter[udid]={Uri.EscapeDataString(udid)}");
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"App Store Connect API error al consultar el dispositivo {udid}: {response.StatusCode} - {body}");

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0;
        }

        public async Task RegisterDeviceAsync(string udid, string name)
        {
            var payload = new
            {
                data = new
                {
                    type = "devices",
                    attributes = new { name, platform = "IOS", udid }
                }
            };

            using var response = await SendAsync(HttpMethod.Post, $"{BaseUrl}/devices", payload);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"No se pudo registrar el dispositivo {udid} en Apple Developer: {response.StatusCode} - {body}");
            }

            _logger.LogInformation("[AppStoreConnect] Dispositivo {Udid} registrado", udid);
        }

        public async Task<byte[]> RegenerateDeviceKitProfileAsync()
        {
            var bundleId = _configuration["IOS:DeviceKit:AppStoreConnect:BundleId"];
            var certificateId = _configuration["IOS:DeviceKit:AppStoreConnect:CertificateId"];
            var profileNamePrefix = _configuration["IOS:DeviceKit:AppStoreConnect:ProfileName"] ?? "MobileRemoteToolkit DeviceKit Development";

            if (string.IsNullOrWhiteSpace(bundleId) || string.IsNullOrWhiteSpace(certificateId))
                throw new InvalidOperationException("Configure IOS:DeviceKit:AppStoreConnect:BundleId y CertificateId en appsettings.");

            var bundleIdResourceId = await GetBundleIdResourceIdAsync(bundleId);
            var deviceResourceIds = await GetEnabledDeviceResourceIdsAsync();

            var payload = new
            {
                data = new
                {
                    type = "profiles",
                    attributes = new
                    {
                        name = $"{profileNamePrefix} {DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                        // DeviceKit corre como XCUITest (necesita el entitlement get-task-allow
                        // para que testmanagerd pueda instrumentarlo) - eso solo lo trae un perfil
                        // de Development, un Ad Hoc/Distribution no sirve aunque tambien liste
                        // dispositivos especificos.
                        profileType = "IOS_APP_DEVELOPMENT"
                    },
                    relationships = new
                    {
                        bundleId = new { data = new { type = "bundleIds", id = bundleIdResourceId } },
                        certificates = new { data = new[] { new { type = "certificates", id = certificateId } } },
                        devices = new { data = deviceResourceIds.Select(id => new { type = "devices", id }).ToArray() }
                    }
                }
            };

            using var response = await SendAsync(HttpMethod.Post, $"{BaseUrl}/profiles", payload);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"No se pudo regenerar el provisioning profile: {response.StatusCode} - {body}");

            using var doc = JsonDocument.Parse(body);
            var profileContentBase64 = doc.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("profileContent").GetString();
            return Convert.FromBase64String(profileContentBase64);
        }

        private async Task<string> GetBundleIdResourceIdAsync(string bundleIdentifier)
        {
            using var response = await SendAsync(HttpMethod.Get, $"{BaseUrl}/bundleIds?filter[identifier]={Uri.EscapeDataString(bundleIdentifier)}");
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"No se pudo resolver el Bundle ID '{bundleIdentifier}' en App Store Connect: {response.StatusCode} - {body}");

            using var doc = JsonDocument.Parse(body);
            var data = doc.RootElement.GetProperty("data");
            if (data.GetArrayLength() == 0)
                throw new InvalidOperationException($"El Bundle ID '{bundleIdentifier}' no existe en la cuenta de Apple Developer configurada.");

            return data[0].GetProperty("id").GetString();
        }

        private async Task<List<string>> GetEnabledDeviceResourceIdsAsync()
        {
            var ids = new List<string>();
            var url = $"{BaseUrl}/devices?filter[status]=ENABLED&limit=200";

            while (!string.IsNullOrEmpty(url))
            {
                using var response = await SendAsync(HttpMethod.Get, url);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"No se pudo listar los dispositivos habilitados: {response.StatusCode} - {body}");

                using var doc = JsonDocument.Parse(body);
                foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
                    ids.Add(item.GetProperty("id").GetString());

                url = doc.RootElement.TryGetProperty("links", out var links) && links.TryGetProperty("next", out var next)
                    ? next.GetString()
                    : null;
            }

            return ids;
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object jsonPayload = null)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", BuildJwt());

            if (jsonPayload is not null)
                request.Content = new StringContent(JsonSerializer.Serialize(jsonPayload), Encoding.UTF8, "application/json");

            return await HttpClient.SendAsync(request);
        }

        private string BuildJwt()
        {
            var keyId = _configuration["IOS:DeviceKit:AppStoreConnect:KeyId"];
            var issuerId = _configuration["IOS:DeviceKit:AppStoreConnect:IssuerId"];
            var privateKeyPath = ExpandPath(_configuration["IOS:DeviceKit:AppStoreConnect:PrivateKeyPath"]);

            if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(issuerId) || string.IsNullOrWhiteSpace(privateKeyPath))
                throw new InvalidOperationException("Configure IOS:DeviceKit:AppStoreConnect:KeyId, IssuerId y PrivateKeyPath en appsettings (ruta a la AuthKey_*.p8, fuera del repo).");

            if (!File.Exists(privateKeyPath))
                throw new InvalidOperationException($"No se encontro la App Store Connect API key en '{privateKeyPath}'.");

            var now = DateTimeOffset.UtcNow;
            var header = new { alg = "ES256", kid = keyId, typ = "JWT" };
            var payload = new
            {
                iss = issuerId,
                iat = now.ToUnixTimeSeconds(),
                exp = now.AddMinutes(20).ToUnixTimeSeconds(),
                aud = "appstoreconnect-v1"
            };

            var unsignedToken = $"{Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header))}.{Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload))}";

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(File.ReadAllText(privateKeyPath));
            var signature = ecdsa.SignData(Encoding.UTF8.GetBytes(unsignedToken), HashAlgorithmName.SHA256);

            return $"{unsignedToken}.{Base64UrlEncode(signature)}";
        }

        private static string Base64UrlEncode(byte[] data)
            => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static string ExpandPath(string? path)
            => string.IsNullOrWhiteSpace(path) ? path : Environment.ExpandEnvironmentVariables(path);
    }
}

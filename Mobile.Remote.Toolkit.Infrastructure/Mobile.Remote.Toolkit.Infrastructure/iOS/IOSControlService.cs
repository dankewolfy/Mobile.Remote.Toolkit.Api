using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Services.iOS;
using Mobile.Remote.Toolkit.Application.Utils;

namespace Mobile.Remote.Toolkit.Infrastructure.iOS
{
    // DeviceKit expone su control (tap/swipe/longpress/text/button) como JSON-RPC 2.0 sobre HTTP
    // local (127.0.0.1:{puerto}/rpc) - pegarle por HTTP directo es mas barato que invocar
    // "ios ui tap/swipe/..." como subproceso nuevo por cada toque.
    public class IOSControlService : IIOSControlService
    {
        private static readonly HttpClient HttpClient = new();

        private readonly GoIosDeviceKitManager _deviceKitManager;
        private readonly IProcessHelper _processHelper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IOSControlService> _logger;

        public IOSControlService(
            GoIosDeviceKitManager deviceKitManager,
            IProcessHelper processHelper,
            IConfiguration configuration,
            ILogger<IOSControlService> logger)
        {
            _deviceKitManager = deviceKitManager;
            _processHelper = processHelper;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> IsAvailableAsync(string udid)
        {
            if (!_deviceKitManager.IsRunningFor(udid))
                return false;

            try
            {
                await SendRpcAsync("device.info", new { });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<ActionResponse> TapAsync(string udid, double x, double y)
            => ExecuteRpcActionAsync(udid, "device.io.tap", new { x, y }, "tap");

        // device.io.swipe (IOSwipe.swift) sintetiza un solo segmento press->release de 0.1s
        // fijo entre dos puntos - se siente como un "estiron", no como un drag. device.io.gesture
        // (IOGesture.swift) sí soporta puntos intermedios con duracion propia por paso, asi que
        // se usa eso para que el gesto reproducido tenga forma y velocidad reales.
        private const int SwipeSteps = 8;

        // Fallback sin ruta real (p.ej. llamado directo via Swagger/API): interpola una linea
        // recta entre los dos puntos en varios pasos, en vez de un solo salto de 0.1s.
        public Task<ActionResponse> SwipeAsync(string udid, double fromX, double fromY, double toX, double toY, int? durationMs = null)
        {
            var totalSeconds = (durationMs ?? 300) / 1000.0;
            var points = new List<(double X, double Y, double TimeOffsetSeconds)> { (fromX, fromY, 0) };
            for (var i = 1; i <= SwipeSteps; i++)
            {
                var t = (double)i / SwipeSteps;
                points.Add((fromX + (toX - fromX) * t, fromY + (toY - fromY) * t, totalSeconds * t));
            }

            return SwipePathAsync(udid, points);
        }

        // Ruta real grabada durante el drag (start.path del mirrorWindow.ts) - se reproduce
        // tal cual via device.io.gesture, siguiendo la forma y timing reales del gesto del
        // usuario en vez de una linea recta.
        public Task<ActionResponse> SwipePathAsync(string udid, IReadOnlyList<(double X, double Y, double TimeOffsetSeconds)> points)
        {
            if (points == null || points.Count < 2)
                throw new ArgumentException("Se necesitan al menos 2 puntos para un swipe", nameof(points));

            var actions = new List<object>
            {
                new { type = "press", x = points[0].X, y = points[0].Y, duration = 0.0, button = 0 }
            };

            for (var i = 1; i < points.Count; i++)
            {
                var stepDuration = Math.Max(0.001, points[i].TimeOffsetSeconds - points[i - 1].TimeOffsetSeconds);
                actions.Add(new
                {
                    type = i == points.Count - 1 ? "release" : "move",
                    x = points[i].X,
                    y = points[i].Y,
                    duration = stepDuration,
                    button = 0
                });
            }

            return ExecuteRpcActionAsync(udid, "device.io.gesture", new { actions }, "swipe");
        }

        // IOLongpress.swift declara "duration" como TimeInterval (segundos), no milisegundos.
        public Task<ActionResponse> LongPressAsync(string udid, double x, double y, int? durationMs = null)
            => ExecuteRpcActionAsync(udid, "device.io.longpress", new { x, y, duration = (durationMs ?? 1000) / 1000.0 }, "long_press");

        public Task<ActionResponse> TypeTextAsync(string udid, string text)
            => ExecuteRpcActionAsync(udid, "device.io.text", new { text }, "type_text");

        public Task<ActionResponse> PressButtonAsync(string udid, string button)
            => ExecuteRpcActionAsync(udid, "device.io.button", new { button }, "button");

        private async Task<ActionResponse> ExecuteRpcActionAsync(string udid, string method, object @params, string actionLabel)
        {
            try
            {
                await EnsureDeviceKitRunningAsync(udid);
                await SendRpcAsync(method, @params);

                return new ActionResponse
                {
                    Success = true,
                    Message = $"Accion '{actionLabel}' ejecutada en {udid}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando accion de control '{Action}' para {Udid}", actionLabel, udid);
                return new ActionResponse
                {
                    Success = false,
                    Message = $"Error ejecutando '{actionLabel}'",
                    Error = ex.Message
                };
            }
        }

        private async Task EnsureDeviceKitRunningAsync(string udid)
        {
            var executable = _configuration["IOS:DeviceKit:Executable"] ?? _configuration["IOS:Mirror:GoIosExecutable"];
            var bundleId = _configuration["IOS:DeviceKit:BundleId"];

            if (string.IsNullOrWhiteSpace(executable) || string.IsNullOrWhiteSpace(bundleId))
                throw new InvalidOperationException("Configure IOS:DeviceKit:Executable e IOS:DeviceKit:BundleId en appsettings (requiere DeviceKit instalado y firmado en el dispositivo, ver receta de la Fase 9).");

            await _deviceKitManager.EnsureRunningAsync(_processHelper, executable, bundleId, udid);
        }

        private async Task<JsonElement> SendRpcAsync(string method, object @params)
        {
            if (!int.TryParse(_configuration["IOS:DeviceKit:Port"], out var port))
                port = 12004;

            var payload = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 1,
                method,
                @params
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await HttpClient.PostAsync($"http://127.0.0.1:{port}/rpc", content);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement.Clone();

            if (root.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
                throw new InvalidOperationException($"DeviceKit RPC error: {error}");

            return root;
        }
    }
}

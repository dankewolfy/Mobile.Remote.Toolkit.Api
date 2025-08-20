using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mobile.Remote.Toolkit.Agent.Services;
using System.Text.Json;

namespace Mobile.Remote.Toolkit.Agent
{
    /// <summary>
    /// Agent local que se ejecuta en cada máquina con dispositivos Android
    /// Este agent detecta dispositivos y ejecuta comandos localmente
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<DeviceDetectionService>();
                    services.AddHostedService<ApiCommunicationService>();
                    services.AddSingleton<IAndroidDeviceService, AndroidDeviceService>();
                    services.AddHttpClient<MobileToolkitApiClient>(client =>
                    {
                        client.BaseAddress = new Uri(context.Configuration["ApiBaseUrl"] ?? "http://localhost:5000");
                    });
                });
    }

    /// <summary>
    /// Cliente para comunicarse con la API central
    /// </summary>
    public class MobileToolkitApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MobileToolkitApiClient> _logger;

        public MobileToolkitApiClient(HttpClient httpClient, ILogger<MobileToolkitApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task RegisterAgentAsync(string agentId, string hostName)
        {
            var payload = new { AgentId = agentId, HostName = hostName, Status = "Online" };
            var response = await _httpClient.PostAsJsonAsync("/api/agents/register", payload);
            response.EnsureSuccessStatusCode();
        }

        public async Task ReportDevicesAsync(string agentId, List<AndroidDevice> devices)
        {
            var payload = new { AgentId = agentId, Devices = devices };
            var response = await _httpClient.PostAsJsonAsync("/api/agents/devices", payload);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<AgentCommand>> GetPendingCommandsAsync(string agentId)
        {
            var response = await _httpClient.GetAsync($"/api/agents/{agentId}/commands");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<AgentCommand>>(content) ?? new List<AgentCommand>();
        }

        public async Task ReportCommandResultAsync(string agentId, string commandId, object result)
        {
            var payload = new { AgentId = agentId, CommandId = commandId, Result = result };
            var response = await _httpClient.PostAsJsonAsync("/api/agents/commands/result", payload);
            response.EnsureSuccessStatusCode();
        }
    }

    /// <summary>
    /// Servicio que detecta dispositivos conectados localmente
    /// </summary>
    public class DeviceDetectionService : BackgroundService
    {
        private readonly IAndroidDeviceService _deviceService;
        private readonly MobileToolkitApiClient _apiClient;
        private readonly ILogger<DeviceDetectionService> _logger;
        private readonly string _agentId;

        public DeviceDetectionService(
            IAndroidDeviceService deviceService,
            MobileToolkitApiClient apiClient,
            ILogger<DeviceDetectionService> logger)
        {
            _deviceService = deviceService;
            _apiClient = apiClient;
            _logger = logger;
            _agentId = Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..8];
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Device Detection Service with Agent ID: {AgentId}", _agentId);

            // Registrar el agent con la API central
            await _apiClient.RegisterAgentAsync(_agentId, Environment.MachineName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Detectar dispositivos conectados
                    var devices = await _deviceService.GetConnectedDevicesAsync();
                    
                    // Reportar dispositivos a la API central
                    await _apiClient.ReportDevicesAsync(_agentId, devices);

                    _logger.LogInformation("Reported {DeviceCount} devices to central API", devices.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error detecting or reporting devices");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Servicio que escucha comandos de la API central y los ejecuta localmente
    /// </summary>
    public class ApiCommunicationService : BackgroundService
    {
        private readonly MobileToolkitApiClient _apiClient;
        private readonly IAndroidDeviceService _deviceService;
        private readonly ILogger<ApiCommunicationService> _logger;
        private readonly string _agentId;

        public ApiCommunicationService(
            MobileToolkitApiClient apiClient,
            IAndroidDeviceService deviceService,
            ILogger<ApiCommunicationService> logger)
        {
            _apiClient = apiClient;
            _deviceService = deviceService;
            _logger = logger;
            _agentId = Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..8];
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting API Communication Service");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Obtener comandos pendientes
                    var commands = await _apiClient.GetPendingCommandsAsync(_agentId);

                    foreach (var command in commands)
                    {
                        await ExecuteCommandAsync(command);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing commands from API");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ExecuteCommandAsync(AgentCommand command)
        {
            try
            {
                _logger.LogInformation("Executing command: {CommandType} for device: {DeviceSerial}", 
                    command.Type, command.DeviceSerial);

                object result = command.Type switch
                {
                    "adb" => await _deviceService.ExecuteAdbCommandAsync(command.DeviceSerial, command.Parameters["command"].ToString()),
                    "screenshot" => await _deviceService.TakeScreenshotAsync(command.DeviceSerial, command.Parameters.GetValueOrDefault("filename")?.ToString()),
                    "mirror_start" => await _deviceService.StartMirrorAsync(command.DeviceSerial),
                    "mirror_stop" => await _deviceService.StopMirrorAsync(command.DeviceSerial),
                    _ => throw new NotSupportedException($"Command type '{command.Type}' is not supported")
                };

                await _apiClient.ReportCommandResultAsync(_agentId, command.Id, result);
                _logger.LogInformation("Command {CommandId} executed successfully", command.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command {CommandId}", command.Id);
                await _apiClient.ReportCommandResultAsync(_agentId, command.Id, new { Error = ex.Message });
            }
        }
    }

    public class AndroidDevice
    {
        public string Serial { get; set; }
        public string Model { get; set; }
        public string Status { get; set; }
        public bool Active { get; set; }
        public string AgentId { get; set; }
    }

    public class AgentCommand
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string DeviceSerial { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}

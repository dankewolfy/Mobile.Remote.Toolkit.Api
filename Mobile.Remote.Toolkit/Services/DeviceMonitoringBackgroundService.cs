using Mobile.Remote.Toolkit.Business.Services.Android;

namespace Mobile.Remote.Toolkit.Api.Services
{
    public class DeviceMonitoringBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeviceMonitoringBackgroundService> _logger;
        private bool _isEnabled = false;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_isEnabled)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var deviceService = scope.ServiceProvider.GetRequiredService<IAndroidDeviceService>();

                        var devices = await deviceService.GetConnectedDevicesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error en monitoreo de dispositivos");
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        public void EnableMonitoring() => _isEnabled = true;
        public void DisableMonitoring() => _isEnabled = false;
    }
}


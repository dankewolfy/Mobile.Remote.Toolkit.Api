using System.Runtime.InteropServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Services;
using Mobile.Remote.Toolkit.Application.Services.Android;
using Mobile.Remote.Toolkit.Application.Services.iOS;
using Mobile.Remote.Toolkit.Application.Utils;
using Mobile.Remote.Toolkit.Infrastructure.Android;
using Mobile.Remote.Toolkit.Infrastructure.iOS;
using Mobile.Remote.Toolkit.Infrastructure.Files;
using Mobile.Remote.Toolkit.Infrastructure.Monitoring;
using Mobile.Remote.Toolkit.Infrastructure.Processes;

namespace Mobile.Remote.Toolkit.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Android
            services.AddSingleton<MirrorProcessRegistry>();
            services.AddScoped<IAndroidDeviceService, AndroidDeviceService>();

            // iOS
            services.AddSingleton<IOSMirrorProcessRegistry>();
            services.AddScoped<IIOSDeviceService, IOSDeviceService>();

            // Procesos y filesystem
            services.AddScoped<IProcessHelper>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ProcessHelper>>();
                return new ProcessHelper(logger);
            });
            services.AddScoped<IFileService, FileService>();

            // Monitoreo de dispositivos: el watcher USB se elige según el SO, la selección
            // vive aquí (composition root) — el servicio de monitoreo no conoce la plataforma.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddSingleton<IUsbHardwareWatcher, WindowsUsbHardwareWatcher>();
            }
            else
            {
                services.AddSingleton<IUsbHardwareWatcher, PollingUsbHardwareWatcher>();
            }

            services.AddSingleton<IDeviceMonitoringService, DeviceMonitoringService>();

            return services;
        }
    }
}

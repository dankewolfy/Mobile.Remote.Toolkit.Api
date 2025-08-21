using Microsoft.Extensions.DependencyInjection;
using Mobile.Remote.Toolkit.Business.Models.Requests.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAndroidDeviceServices(this IServiceCollection services, Action<AndroidDeviceServiceOptions> configure = null)
        {
            var options = new AndroidDeviceServiceOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);
            services.AddScoped<ICommandExecutor, ProcessCommandExecutor>();
            services.AddScoped<IDeviceInfoProvider, DeviceInfoProvider>();
            services.AddScoped<IMirrorService, MirrorService>();
            services.AddScoped<IScreenshotService, ScreenshotService>();
            services.AddScoped<IAndroidDeviceService, AndroidDeviceService>();

            return services;
        }
    }

    public class AndroidDeviceServiceOptions
    {
        public MirrorOptionsRequest DefaultMirrorOptions { get; set; } = new();
        public string ScreenshotFolder { get; set; } = "Screenshots";
    }
}

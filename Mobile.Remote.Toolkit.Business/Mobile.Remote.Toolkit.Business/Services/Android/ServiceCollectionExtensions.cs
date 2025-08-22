using Microsoft.Extensions.DependencyInjection;

using Mobile.Remote.Toolkit.Business.Utils;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAndroidDeviceServices(this IServiceCollection services, Action<AndroidDeviceServiceOptions> configure)
        {
            var options = new AndroidDeviceServiceOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);
            services.AddScoped<IDeviceInfoProvider, DeviceInfoProvider>();
            services.AddScoped<IMirrorService, MirrorService>();
            services.AddScoped<IScreenshotService, ScreenshotService>();
            services.AddScoped<IAndroidDeviceService, AndroidDeviceService>();
            services.AddScoped<IProcessHelper, ProcessHelper>();

            return services;
        }
    }

    public class AndroidDeviceServiceOptions
    {
        public Dictionary<string, object> DefaultMirrorOptions { get; set; } = [];
        public string ScreenshotFolder { get; set; } = "Screenshots";
    }
}

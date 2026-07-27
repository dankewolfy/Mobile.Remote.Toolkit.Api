using MediatR;
using Mobile.Remote.Toolkit.Api.Hubs;
using Mobile.Remote.Toolkit.Api.Services;
using Mobile.Remote.Toolkit.Business.Commands.Android;
using Mobile.Remote.Toolkit.Business.Queries.Android;
using Mobile.Remote.Toolkit.Business.Services;
using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Services.iOS;
using Mobile.Remote.Toolkit.Business.Utils;

var builder = WebApplication.CreateBuilder(args);

// Asegurar que los logs del proyecto se vean siempre en consola
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.FormatterName = "simple");
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
    options.IncludeScopes = false;
});
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("Mobile.Remote.Toolkit", LogLevel.Debug);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar CORS para Vue
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:8080",
                "http://localhost:5173",
                "https://localhost:3000",
                "https://localhost:8080",
                "https://localhost:5173"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Registrar servicios para Android
builder.Services.AddSingleton<MirrorProcessRegistry>();
builder.Services.AddScoped<IAndroidDeviceService, AndroidDeviceService>();
// Registrar servicios para iOS
builder.Services.AddSingleton<IOSMirrorProcessRegistry>();
builder.Services.AddScoped<IIOSDeviceService, IOSDeviceService>();

builder.Services.AddScoped<IProcessHelper>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<ProcessHelper>>();
    return new ProcessHelper(logger);
});
builder.Services.AddScoped<IFileService, FileService>();

builder.Services.AddSingleton<IDeviceMonitoringService, DeviceMonitoringService>();
builder.Services.AddSingleton<INotificationService, SignalRNotificationService>();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(GetAndroidDevicesQuery).Assembly,
    typeof(Program).Assembly
));

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Verificar que las herramientas estén disponibles
//using (var scope = app.Services.CreateScope())
//{
//    var processHelper = scope.ServiceProvider.GetRequiredService<IProcessHelper>();
//    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

//    try
//    {
//        var adbResult = await processHelper.ExecuteCommandAsync("adb", "version");
//        if (adbResult.Success)
//        {
//            logger.LogInformation($"ADB disponible: {adbResult.Output.Split('\n')[0]}");
//        }
//        else
//        {
//            logger.LogWarning("ADB no está disponible");
//        }
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "Error verificando herramientas");
//    }
//}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS: when hosted inside Electron the renderer origin is `null` (file://).
// In that case we allow all origins since the API only listens on localhost.
var isElectronHosted = Environment.GetEnvironmentVariable("ELECTRON_HOSTED") == "1";
if (isElectronHosted)
{
    app.UseCors(policy => policy
        .SetIsOriginAllowed(_ => true)   // allow null + any localhost
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
}
else
{
    app.UseCors("AllowVueApp");
}
app.UseAuthorization();

app.MapControllers();
app.MapHub<AndroidDeviceHub>("/hubs/android");

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        var monitoringService = scope.ServiceProvider.GetRequiredService<IDeviceMonitoringService>();

        try
        {
            await monitoringService.StartMonitoringAsync(); 
            logger.LogInformation("Monitoreo de dispositivos auto-iniciado.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo auto-iniciar el monitoreo de dispositivos.");
        }
    });
});

app.Run();
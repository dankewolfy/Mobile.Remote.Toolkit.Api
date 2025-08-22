using MediatR;

using Mobile.Remote.Toolkit.Api.Hubs;
using Mobile.Remote.Toolkit.Business.Utils;
using Mobile.Remote.Toolkit.Business.Services;
using Mobile.Remote.Toolkit.Business.Queries.Android;
using Mobile.Remote.Toolkit.Business.Commands.Android;
using Mobile.Remote.Toolkit.Business.Services.Android;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar HttpClient y el servicio para el agente ADB/Scrcpy

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

// Register all Android services with a single extension method
builder.Services.AddAndroidDeviceServices(options =>
{
    options.DefaultMirrorOptions = [];
    options.ScreenshotFolder = "Screenshots";
});

builder.Services.AddScoped<IProcessHelper>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<ProcessHelper>>();
    var executorLogger = provider.GetRequiredService<ILogger<ProcessCommandExecutor>>();
    return new ProcessHelper(logger, executorLogger);
});
builder.Services.AddScoped<IFileService, FileService>();

builder.Services.AddSingleton<IDeviceMonitoringService, DeviceMonitoringService>();
builder.Services.AddSingleton<INotificationService, Mobile.Remote.Toolkit.Business.Services.LogNotificationService>();

//builder.Services.AddHostedService<DeviceMonitoringBackgroundService>();

// MediatR
builder.Services.AddMediatR(typeof(Program).Assembly);
builder.Services.AddMediatR(typeof(GetAndroidDevicesQuery).Assembly);
builder.Services.AddMediatR(typeof(ExecuteAdbCommandHandler).Assembly);

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
app.UseCors("AllowVueApp");
app.UseAuthorization();

app.MapControllers();
app.MapHub<AndroidDeviceHub>("/hubs/android");

app.Run();
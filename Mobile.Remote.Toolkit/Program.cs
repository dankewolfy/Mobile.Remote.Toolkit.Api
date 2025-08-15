using MediatR;
using Mobile.Remote.Toolkit.Api.Hubs;
using Mobile.Remote.Toolkit.Api.Services;
using Mobile.Remote.Toolkit.Business.Commands.Android;
using Mobile.Remote.Toolkit.Business.Queries.Android;
using Mobile.Remote.Toolkit.Business.Services;
using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Utils;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<IAndroidDeviceService, AndroidDeviceService>();

builder.Services.AddScoped<IProcessHelper>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<ProcessHelper>>();
    return new ProcessHelper(logger);
});
builder.Services.AddScoped<IFileService, FileService>();

builder.Services.AddSingleton<IDeviceMonitoringService, DeviceMonitoringService>();
builder.Services.AddSingleton<INotificationService, Mobile.Remote.Toolkit.Business.Services.LogNotificationService>();

//builder.Services.AddHostedService<DeviceMonitoringBackgroundService>();

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
app.UseCors("AllowVueApp");
app.UseAuthorization();

app.MapControllers();
app.MapHub<AndroidDeviceHub>("/hubs/android");

app.Run();
using Microsoft.AspNetCore.Mvc;

using Mobile.Remote.Toolkit.Business.Utils;
using Mobile.Remote.Toolkit.Business.Services.Android;

namespace Mobile.Remote.Toolkit.Api.Controllers
{
    [ApiController]
    [Route("api/stats")]
    public class StatsController : ControllerBase
    {
        private readonly IAndroidDeviceService _androidService;
        private readonly IFileService _fileService;

        public StatsController(IAndroidDeviceService androidService, IFileService fileService)
        {
            _androidService = androidService;
            _fileService = fileService;
        }

        /// <summary>
        /// Obtiene estadísticas generales del sistema
        /// </summary>
        /// <returns>Estadísticas del sistema</returns>
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSystemStats()
        {
            var devices = await _androidService.GetConnectedDeviceSerialsAsync();
            var activeDevices = devices.Where(d => d.Active).ToList();

            return Ok(new
            {
                devices = new
                {
                    total = devices.Count,
                    active = activeDevices.Count,
                    inactive = devices.Count - activeDevices.Count
                },
                system = new
                {
                    uptime = Environment.TickCount64,
                    timestamp = DateTime.UtcNow
                }
            });
        }

        /// <summary>
        /// Obtiene estadísticas detalladas por dispositivo
        /// </summary>
        /// <returns>Estadísticas por dispositivo</returns>
        [HttpGet("devices")]
        public async Task<ActionResult<List<object>>> GetDeviceStats()
        {
            var devices = await _androidService.GetConnectedDeviceSerialsAsync();
            var deviceStats = new List<object>();

            foreach (var device in devices)
            {
                var screenshots = await _fileService.GetScreenshotFilesAsync(device.Serial);
                var status = await _androidService.GetMirrorStatusAsync(device.Serial);

                deviceStats.Add(new
                {
                    serial = device.Serial,
                    name = device.Name,
                    brand = device.Brand,
                    model = device.Model,
                    active = device.Active,
                    screenshots = screenshots.Count,
                    status = status
                });
            }

            return Ok(deviceStats);
        }

        private async Task<long> GetTotalScreenshotsSizeAsync(List<string> screenshots)
        {
            long totalSize = 0;
            var screenshotsFolder = await _fileService.GetScreenshotsFolderAsync();

            foreach (var screenshot in screenshots)
            {
                var filePath = Path.Combine(screenshotsFolder, screenshot);
                if (System.IO.File.Exists(filePath))
                {
                    totalSize += new FileInfo(filePath).Length;
                }
            }

            return totalSize;
        }
    }
}

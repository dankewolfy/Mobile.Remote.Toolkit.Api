using Microsoft.AspNetCore.Mvc;

using Mobile.Remote.Toolkit.Business.Services;
using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Api.Controllers
{
    [ApiController]
    [Route("api/monitoring")]
    public class MonitoringController : ControllerBase
    {
        private readonly IDeviceMonitoringService _monitoringService;

        public MonitoringController(IDeviceMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        /// <summary>
        /// Obtiene el estado del servicio de monitoreo
        /// </summary>
        /// <returns>Estado del monitoreo</returns>
        [HttpGet("status")]
        public ActionResult<object> GetMonitoringStatus()
        {
            return Ok(new
            {
                isMonitoring = _monitoringService.IsMonitoring,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Inicia el monitoreo manual (ya se inicia automáticamente)
        /// </summary>
        /// <returns>Resultado de la operación</returns>
        [HttpPost("start")]
        public async Task<ActionResult<ActionResponse>> StartMonitoring()
        {
            if (_monitoringService.IsMonitoring)
                return Ok(new ActionResponse
                {
                    Success = true,
                    Message = "El monitoreo ya está activo"
                });

            await _monitoringService.StartMonitoringAsync();

            return Ok(new ActionResponse
            {
                Success = true,
                Message = "Monitoreo iniciado correctamente"
            });
        }

        /// <summary>
        /// Detiene el monitoreo
        /// </summary>
        /// <returns>Resultado de la operación</returns>
        [HttpPost("stop")]
        public async Task<ActionResult<ActionResponse>> StopMonitoring()
        {
            if (!_monitoringService.IsMonitoring)
                return Ok(new ActionResponse
                {
                    Success = true,
                    Message = "El monitoreo no está activo"
                });

            await _monitoringService.StopMonitoringAsync();

            return Ok(new ActionResponse
            {
                Success = true,
                Message = "Monitoreo detenido correctamente"
            });
        }
    }
}

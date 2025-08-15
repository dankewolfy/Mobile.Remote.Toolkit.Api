using Microsoft.AspNetCore.Mvc;

using Mobile.Remote.Toolkit.Api.Controllers.Base;
using Mobile.Remote.Toolkit.Business.Commands.Android;
using Mobile.Remote.Toolkit.Business.Models.Requests.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;
using Mobile.Remote.Toolkit.Business.Queries.Android;

namespace Mobile.Remote.Toolkit.Api.Controllers.Android
{
    [ApiController]
    [Route("api/[controller]")]
    public class AndroidController : BaseController
    {
        /// <summary>
        /// Inicia el mirror para un dispositivo Android específico
        /// </summary>
        /// <param name="serial">Serial del dispositivo</param>
        /// <param name="request">Opciones del mirror</param>
        /// <returns>Resultado de la acción</returns>
        [HttpPost("devices/{serial}/mirror/start")]
        public async Task<ActionResult<ActionResponse>> StartMirror(
            string serial,
            [FromBody] StartMirrorRequest request = null)
        {
            var command = new StartMirrorCommand
            {
                Serial = serial,
                Options = request?.Options ?? new Dictionary<string, object>()
            };

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Detiene el mirror para un dispositivo Android específico
        /// </summary>
        /// <param name="serial">Serial del dispositivo</param>
        /// <returns>Resultado de la acción</returns>
        [HttpPost("devices/{serial}/mirror/stop")]
        public async Task<ActionResult<ActionResponse>> StopMirror(string serial)
        {
            var command = new StopMirrorCommand { Serial = serial };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Toma un screenshot del dispositivo Android
        /// </summary>
        /// <param name="serial">Serial del dispositivo</param>
        /// <param name="request">Opciones del screenshot</param>
        /// <returns>Resultado con información del archivo</returns>
        [HttpPost("devices/{serial}/screenshot")]
        public async Task<ActionResult<ActionResponse>> TakeScreenshot(
            string serial,
            [FromBody] ScreenshotRequest request = null)
        {
            var command = new TakeScreenshotCommand
            {
                Serial = serial,
                Filename = request?.Filename
            };

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Obtiene el estado actual de un dispositivo específico
        /// </summary>
        /// <param name="serial">Serial del dispositivo</param>
        /// <returns>Estado del dispositivo</returns>
        [HttpGet("devices/{serial}/status")]
        public async Task<ActionResult<Dictionary<string, object>>> GetDeviceStatus(string serial)
        {
            var query = new GetDeviceStatusQuery { Serial = serial };
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Obtiene solo los dispositivos activos (con mirror corriendo)
        /// </summary>
        /// <returns>Lista de dispositivos activos</returns>
        [HttpGet("devices/active")]
        public async Task<ActionResult<List<AndroidDeviceResponse>>> GetActiveDevices()
        {
            var query = new GetAndroidDevicesQuery { ActiveOnly = true };
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Ejecuta un comando ADB personalizado
        /// </summary>
        /// <param name="serial">Serial del dispositivo</param>
        /// <param name="request">Comando a ejecutar</param>
        /// <returns>Resultado del comando</returns>
        [HttpPost("devices/{serial}/adb")]
        public async Task<ActionResult<ActionResponse>> ExecuteAdb([FromRoute] string serial, [FromBody] ExecuteAdbCommandRequest request)
        {
            request.Serial = serial;
            //ActionResponse response = await Mediator.Send(request);
            return Ok(request);
        }

        [HttpGet("devices")]
        public async Task<IActionResult> GetDevices([FromQuery] GetAndroidDevicesRequest request)
        {
            var query = new GetAndroidDevicesQuery { ActiveOnly = request.ActiveOnly };
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("devices/{serial}/action")]
        public async Task<IActionResult> ExecuteAction(string serial, [FromBody] AndroidActionRequest request)
        {
            var command = new ExecuteAndroidActionCommand
            {
                Serial = serial,
                Action = request.Action,
                Payload = request.Payload
            };

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Verifica el estado de las herramientas necesarias
        /// </summary>
        /// <returns>Estado de las herramientas</returns>
        [HttpGet("tools/status")]
        public async Task<ActionResult<object>> GetToolsStatus()
        {
            return default;
        }
    }
}

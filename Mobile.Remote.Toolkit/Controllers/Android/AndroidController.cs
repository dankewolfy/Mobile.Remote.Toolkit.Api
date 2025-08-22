using Microsoft.AspNetCore.Mvc;

using Mobile.Remote.Toolkit.Api.Controllers.Base;
using Mobile.Remote.Toolkit.Business.Queries.Android;
using Mobile.Remote.Toolkit.Business.Commands.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Requests.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Api.Controllers.Android
{
    [ApiController]
    [Route("api/[controller]")]
    public class AndroidController : BaseController
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("devices")]
        public async Task<IActionResult> GetDevices() => Ok(await Mediator.Send(new GetAndroidDevicesQuery()));

        /// <summary>
        /// Obtiene un dispositivo por su serial
        /// </summary>
        /// <param name="serial"></param>
        /// <returns>Informacion de dispositivo</returns>
        [HttpGet("devices/{serial}/info")]
        public async Task<ActionResult<List<AndroidDeviceResponse>>> GetDevice([FromRoute] string serial) => Ok(await Mediator.Send(new GetAndroidDeviceBySerialQuery { Serial = serial }));

        /// <summary>
        /// Obtiene el estado actual de un dispositivo específico
        /// </summary>
        /// <param name="serial">Serial del dispositivo</param>
        /// <returns>Estado del dispositivo</returns>
        [HttpGet("devices/{serial}/status")]
        public async Task<ActionResult<Dictionary<string, object>>> GetDeviceStatus([FromRoute] string serial) => Ok(await Mediator.Send(new GetDeviceStatusQuery { Serial = serial }));

        /// <summary>
        /// Obtiene solo los dispositivos activos (con mirror corriendo)
        /// </summary>
        /// <returns>Lista de dispositivos activos</returns>
        [HttpGet("devices/active")]
        public async Task<ActionResult<List<AndroidDeviceResponse>>> GetActiveDevices() => Ok(await Mediator.Send(new GetAndroidDevicesActiveQuery()));

        /// <summary>
        /// Lista los procesos scrcpy activos
        /// </summary>
        [HttpGet("processes")]
        public async Task<IActionResult> GetScrcpyProcesses([FromServices] Mobile.Remote.Toolkit.Business.Utils.IProcessHelper processHelper)
        {
            var processIds = await processHelper.GetProcessIdsByNameAsync("scrcpy");
            var processes = processIds.Select(pid => {
                try
                {
                    var proc = System.Diagnostics.Process.GetProcessById(pid);
                    return new {
                        pid,
                        name = proc.ProcessName,
                        startTime = proc.StartTime,
                        mainWindowTitle = proc.MainWindowTitle
                    };
                }
                catch { return null; }
            }).Where(p => p != null).ToList();
            return Ok(processes);
        }

        /// <summary>
        /// Elimina procesos scrcpy por PID (solo si están activos)
        /// </summary>
        [HttpPost("processes/kill")]
        public async Task<IActionResult> KillScrcpyProcesses([FromBody] List<int> pids, [FromServices] Mobile.Remote.Toolkit.Business.Utils.IProcessHelper processHelper)
        {
            var activePids = await processHelper.GetProcessIdsByNameAsync("scrcpy");
            var killed = new List<int>();
            foreach (var pid in pids)
            {
                if (activePids.Contains(pid))
                {
                    var ok = await processHelper.KillProcessAsync(pid);
                    if (ok) killed.Add(pid);
                }
            }
            return Ok(new { killed });
        }

        /// <summary>
        /// Inicia el mirror para un dispositivo Android específico
        /// </summary>
        /// <param name="serial">Serial del dispositivo</param>
        /// <param name="request">Opciones del mirror</param>
        /// <returns>Resultado de la acción</returns>
        [HttpPost("devices/{serial}/mirror/start")]
        public async Task<ActionResult<ActionResponse>> StartMirror([FromRoute] string serial, [FromBody] StartMirrorRequest request) => Ok(await Mediator.Send(new StartMirrorCommand { Serial = serial, Options = request?.Options ?? [] }));

        /// <summary>
        /// Detiene el mirror para un dispositivo Android específico
        /// </summary>
        /// <param name="serial">Serial del dispositivo</param>
        /// <returns>Resultado de la acción</returns>
        [HttpPost("devices/{serial}/mirror/stop")]
        public async Task<ActionResult<ActionResponse>> StopMirror([FromRoute] string serial) => Ok(await Mediator.Send(new StopMirrorCommand { Serial = serial }));

        /// <summary>
        /// Toma un screenshot del dispositivo Android
        /// </summary>
        /// <param name="serial">Serial del dispositivo</param>
        /// <param name="request">Opciones del screenshot</param>
        /// <returns>Resultado con información del archivo</returns>
        [HttpPost("devices/{serial}/screenshot")]
        [Produces("image/png")]
        public async Task<IActionResult> TakeScreenshot([FromRoute] string serial, [FromBody] ScreenshotRequest request)
        {
            var response = await Mediator.Send(new TakeScreenshotCommand { Serial = serial, Filename = request.Filename });

            if (response.Bytes == null || response.Bytes.Length == 0)
                return BadRequest("No se pudo tomar la captura");

            return File(response.Bytes, "image/png", response.Filename ?? "screenshot.png");
        }

        /// <summary>
        /// Ejecuta un comando ADB personalizado
        /// </summary>
        /// <param name="serial">Serial del dispositivo</param>
        /// <param name="request">Comando a ejecutar</param>
        /// <returns>Resultado del comando</returns>
        [HttpPost("devices/{serial}/adb")]
        public async Task<ActionResult<ActionResponse>> ExecuteAdb([FromRoute] string serial, [FromBody] ExecuteAdbCommandRequest request) => Ok(await Mediator.Send(request with { Serial = serial }));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("devices/{serial}/action")]
        public async Task<IActionResult> ExecuteAction(string serial, [FromBody] AndroidActionRequest request) => Ok(await Mediator.Send(new ExecuteAndroidActionCommand { Serial = serial, Action = request.Action, Payload = request.Payload }));

        /// <summary>
        /// Verifica el estado de las herramientas necesarias
        /// </summary>
        /// <returns>Estado de las herramientas</returns>
        [HttpGet("tools/status")]
        public async Task<ActionResult<object>> GetToolsStatus() => Ok(await Mediator.Send(new GetToolsStatusQuery()));
    }
}

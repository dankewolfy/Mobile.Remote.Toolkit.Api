using Microsoft.AspNetCore.Mvc;

using Mobile.Remote.Toolkit.Api.Controllers.Base;
using Mobile.Remote.Toolkit.Application.Commands.iOS;
using Mobile.Remote.Toolkit.Application.Models.Requests.iOS;
using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Application.Queries.iOS;

namespace Mobile.Remote.Toolkit.Api.Controllers.iOS
{
    [ApiController]
    [Route("api/ios")]
    public class IOSController : BaseController
    {
        [HttpGet("devices")]
        public async Task<ActionResult<List<IOSDeviceResponse>>> GetDevices([FromQuery] bool? activeOnly)
        {
            var query = new GetIOSDevicesQuery { ActiveOnly = activeOnly };
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("devices/{udid}/info")]
        public async Task<ActionResult<IOSDeviceResponse>> GetDeviceInfo(string udid)
        {
            var query = new GetIOSDeviceInfoQuery { Udid = udid };
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("devices/{udid}/status")]
        public async Task<ActionResult<Dictionary<string, object>>> GetDeviceStatus(string udid)
        {
            var query = new GetIOSDeviceStatusQuery { Udid = udid };
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("devices/{udid}/mirror/start")]
        public async Task<ActionResult<ActionResponse>> StartMirror(
            string udid,
            [FromBody] IOSStartMirrorRequest request = null)
        {
            var command = new StartIOSMirrorCommand
            {
                Udid = udid,
                Options = request?.Options ?? new Dictionary<string, object>()
            };

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("devices/{udid}/mirror/stop")]
        public async Task<ActionResult<ActionResponse>> StopMirror(string udid)
        {
            var command = new StopIOSMirrorCommand { Udid = udid };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("devices/{udid}/action")]
        public async Task<ActionResult<ActionResponse>> ExecuteAction(string udid, [FromBody] IOSActionRequest request)
        {
            var command = new ExecuteIOSActionCommand
            {
                Udid = udid,
                Action = request.Action,
                Payload = request.Payload
            };

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("devices/{udid}/screenshot")]
        public async Task<ActionResult<ActionResponse>> TakeScreenshot(string udid, [FromQuery] string filename = null)
        {
            var command = new TakeIOSScreenshotCommand { Udid = udid, Filename = filename };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("mirror/sessions")]
        public ActionResult<object> GetMirrorSessions()
        {
            return Ok(new { success = true, sessions = new List<object>() });
        }

        [HttpGet("drivers/status")]
        public async Task<ActionResult<IOSDriverStatusResponse>> GetDriverStatus()
        {
            var query = new GetIOSDriverStatusQuery();
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("drivers/install")]
        public async Task<ActionResult<ActionResponse>> InstallDriver()
        {
            var command = new InstallIOSDriverCommand();
            var result = await Mediator.Send(command);
            return Ok(result);
        }
    }
}

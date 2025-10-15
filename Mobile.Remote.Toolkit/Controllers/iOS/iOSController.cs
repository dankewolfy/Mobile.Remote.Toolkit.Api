using Microsoft.AspNetCore.Mvc;
using MediatR;
using Mobile.Remote.Toolkit.Business.Queries.iOS;

namespace Mobile.Remote.Toolkit.Api.Controllers.iOS
{
    [ApiController]
    [Route("api/ios")]
    public class IOSController : ControllerBase
    {
        private readonly IMediator _mediator;

        public IOSController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("devices")]
        public async Task<ActionResult> GetDevices()
        {
            var devices = await _mediator.Send(new Mobile.Remote.Toolkit.Business.Queries.iOS.GetIOSDevicesQuery());
            return Ok(devices);
        }

        [HttpGet("devices/{udid}/info")]
        public async Task<ActionResult> GetDeviceInfo(string udid)
        {
            var device = await _mediator.Send(new Mobile.Remote.Toolkit.Business.Queries.iOS.GetIOSDeviceInfoQuery(udid));
            return Ok(device);
        }

        [HttpPost("devices/{udid}/mirror/start")]
        public ActionResult<object> StartMirror(string udid)
        {
            return Ok(new { success = false, error = "iOS no implementado aún" });
        }

        [HttpPost("devices/{udid}/mirror/stop")]
        public ActionResult<object> StopMirror(string udid)
        {
            return Ok(new { success = false, error = "iOS no implementado aún" });
        }

        [HttpPost("devices/{udid}/action")]
        public ActionResult<object> ExecuteAction(string udid)
        {
            return Ok(new { success = false, error = "iOS no implementado aún" });
        }

        [HttpGet("devices/{udid}/screenshot")]
        public ActionResult TakeScreenshot(string udid)
        {
            return BadRequest(new { success = false, error = "iOS no implementado aún" });
        }

        [HttpGet("mirror/sessions")]
        public ActionResult<object> GetMirrorSessions()
        {
            return Ok(new { success = true, sessions = new List<object>() });
        }
    }
}

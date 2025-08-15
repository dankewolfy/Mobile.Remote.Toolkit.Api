using Microsoft.AspNetCore.Mvc;

namespace Mobile.Remote.Toolkit.Api.Controllers.iOS
{
    [ApiController]
    [Route("api/ios")]
    public class IOSController : ControllerBase
    {
        [HttpGet("devices")]
        public ActionResult<object> GetDevices()
        {
            return Ok(new { success = true, devices = new List<object>() });
        }

        [HttpGet("devices/{udid}/info")]
        public ActionResult<object> GetDeviceInfo(string udid)
        {
            return Ok(new { success = false, error = "iOS no implementado aún" });
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

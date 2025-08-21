using System.Reflection;

using Microsoft.AspNetCore.Mvc;

namespace Mobile.Remote.Toolkit.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            return Ok(new { version });
        }
    }
}

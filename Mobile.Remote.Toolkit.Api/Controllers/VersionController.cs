using Microsoft.AspNetCore.Mvc;
using System.Reflection;

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

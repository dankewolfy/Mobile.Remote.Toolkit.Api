using System.Reflection;

using Microsoft.AspNetCore.Mvc;
using Mobile.Remote.Toolkit.Business.Models;

namespace Mobile.Remote.Toolkit.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? Patform.Unknown.ToString();
            return Ok(new { version });
        }
    }
}

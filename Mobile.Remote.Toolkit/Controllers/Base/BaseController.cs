#nullable disable

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Api.Controllers.Base
{
    [ApiController]
    [Produces("application/json", [])]
    public class BaseController : ControllerBase
    {
        private IMediator _mediator;
        private ILogger _logger;

        protected IMediator Mediator => _mediator ??= base.HttpContext.RequestServices.GetService<IMediator>();
        protected ILogger Logger => _logger ??= base.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

        protected IActionResult ApiError(string message, string error = null)
        {
            Logger.LogError("API error: {Message} | {Error}", message, error);
            return Ok(new ActionResponse { Success = false, Message = message, Error = error ?? message });
        }
    }
}

#nullable disable

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace Mobile.Remote.Toolkit.Api.Controllers.Base
{
    [ApiController]
    [Produces("application/json", [])]
    public class BaseController : ControllerBase
    {
        private IMediator _mediator;

        protected IMediator Mediator => _mediator ??= base.HttpContext.RequestServices.GetService<IMediator>();
    }
}

using MediatR;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Commands.Base
{
    public abstract class IOSBaseCommandHandler<TRequest, TResponse> : BaseCommandHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        protected readonly IIOSDeviceService IOSDeviceService;

        protected IOSBaseCommandHandler(IIOSDeviceService iosDeviceService, ILogger logger) : base(logger)
        {
            IOSDeviceService = iosDeviceService ?? throw new ArgumentNullException(nameof(iosDeviceService));
        }
    }
}

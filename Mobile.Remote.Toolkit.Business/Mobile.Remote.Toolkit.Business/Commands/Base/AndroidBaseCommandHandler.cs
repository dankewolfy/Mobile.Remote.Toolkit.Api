using MediatR;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Services.Android;

namespace Mobile.Remote.Toolkit.Business.Commands.Base
{
    public abstract class AndroidBaseCommandHandler<TRequest, TResponse> : BaseCommandHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        protected readonly IAndroidDeviceService AndroidDeviceService;

        protected AndroidBaseCommandHandler(IAndroidDeviceService androidDeviceService, ILogger logger) : base(logger)
        {
            AndroidDeviceService = androidDeviceService ?? throw new ArgumentNullException(nameof(androidDeviceService));
        }
    }
}

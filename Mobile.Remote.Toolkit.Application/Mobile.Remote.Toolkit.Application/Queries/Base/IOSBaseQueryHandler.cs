using MediatR;

using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Queries.Base
{
    public abstract class IOSBaseQueryHandler<TRequest, TResponse> : BaseQueryHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        protected readonly IIOSDeviceService IOSService;

        protected IOSBaseQueryHandler(IMediator mediator, IIOSDeviceService iosService) : base(mediator)
        {
            IOSService = iosService ?? throw new ArgumentNullException(nameof(iosService));
        }
    }
}

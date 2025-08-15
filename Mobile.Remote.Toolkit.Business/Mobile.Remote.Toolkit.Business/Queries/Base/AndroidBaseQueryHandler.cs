using MediatR;

using Mobile.Remote.Toolkit.Business.Services.Android;

namespace Mobile.Remote.Toolkit.Business.Queries.Base
{
    public abstract class AndroidBaseQueryHandler<TRequest, TResponse> : BaseQueryHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        protected readonly IAndroidDeviceService AndroidService;

        protected AndroidBaseQueryHandler(IMediator mediator, IAndroidDeviceService androidService) : base(mediator)
        {
            AndroidService = androidService ?? throw new ArgumentNullException(nameof(androidService));
        }
    }
}

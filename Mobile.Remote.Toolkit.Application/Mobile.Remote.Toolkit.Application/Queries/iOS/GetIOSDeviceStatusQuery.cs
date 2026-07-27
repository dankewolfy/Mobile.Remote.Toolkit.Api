using MediatR;

using Mobile.Remote.Toolkit.Application.Queries.Base;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Queries.iOS
{
    public sealed class GetIOSDeviceStatusQuery : IRequest<Dictionary<string, object>>
    {
        public string Udid { get; set; }

        public class GetIOSDeviceStatusQueryHandler : IOSBaseQueryHandler<GetIOSDeviceStatusQuery, Dictionary<string, object>>
        {
            public GetIOSDeviceStatusQueryHandler(IMediator mediator, IIOSDeviceService iosService)
                : base(mediator, iosService)
            {
            }

            public override async Task<Dictionary<string, object>> Handle(GetIOSDeviceStatusQuery request, CancellationToken cancellationToken)
                => await IOSService.GetDeviceStatusAsync(request.Udid);
        }
    }
}

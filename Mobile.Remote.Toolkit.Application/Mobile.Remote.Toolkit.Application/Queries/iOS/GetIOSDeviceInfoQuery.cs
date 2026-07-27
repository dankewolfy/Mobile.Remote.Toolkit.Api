using MediatR;

using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Application.Queries.Base;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Queries.iOS
{
    public sealed class GetIOSDeviceInfoQuery : IRequest<IOSDeviceResponse>
    {
        public string Udid { get; set; }

        public class GetIOSDeviceInfoQueryHandler : IOSBaseQueryHandler<GetIOSDeviceInfoQuery, IOSDeviceResponse>
        {
            public GetIOSDeviceInfoQueryHandler(IMediator mediator, IIOSDeviceService iosService)
                : base(mediator, iosService)
            {
            }

            public override async Task<IOSDeviceResponse> Handle(GetIOSDeviceInfoQuery request, CancellationToken cancellationToken)
                => await IOSService.GetDeviceInfoAsync(request.Udid);
        }
    }
}

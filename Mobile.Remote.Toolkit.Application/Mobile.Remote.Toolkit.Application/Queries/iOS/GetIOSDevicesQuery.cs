using MediatR;

using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Application.Queries.Base;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Queries.iOS
{
    public sealed class GetIOSDevicesQuery : IRequest<List<IOSDeviceResponse>>
    {
        public bool? ActiveOnly { get; set; }

        public class GetIOSDevicesQueryHandler : IOSBaseQueryHandler<GetIOSDevicesQuery, List<IOSDeviceResponse>>
        {
            public GetIOSDevicesQueryHandler(IMediator mediator, IIOSDeviceService iosService)
                : base(mediator, iosService)
            {
            }

            public override async Task<List<IOSDeviceResponse>> Handle(GetIOSDevicesQuery request, CancellationToken cancellationToken)
            {
                var devices = await IOSService.GetConnectedDevicesAsync();

                if (request.ActiveOnly == true)
                    devices = devices.Where(d => d.Active).ToList();

                return devices;
            }
        }
    }
}

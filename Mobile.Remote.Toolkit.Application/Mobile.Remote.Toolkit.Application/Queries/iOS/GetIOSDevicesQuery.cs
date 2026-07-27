using MediatR;

using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Queries.iOS
{
    public sealed class GetIOSDevicesQuery : IRequest<List<IOSDeviceResponse>>
    {
        public bool? ActiveOnly { get; set; }

        public class GetIOSDevicesQueryHandler : IRequestHandler<GetIOSDevicesQuery, List<IOSDeviceResponse>>
        {
            private readonly IIOSDeviceService _iosService;

            public GetIOSDevicesQueryHandler(IIOSDeviceService iosService)
            {
                _iosService = iosService;
            }

            public async Task<List<IOSDeviceResponse>> Handle(GetIOSDevicesQuery request, CancellationToken cancellationToken)
            {
                var devices = await _iosService.GetConnectedDevicesAsync();

                if (request.ActiveOnly == true)
                    devices = devices.Where(d => d.Active).ToList();

                return devices;
            }
        }
    }
}

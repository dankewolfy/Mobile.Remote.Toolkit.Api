using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mobile.Remote.Toolkit.Business.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Business.Services.iOS;
using Mobile.Remote.Toolkit.Business.Queries.iOS;

namespace Mobile.Remote.Toolkit.Business.Handlers.iOS
{
    public class GetIOSDevicesHandler : IRequestHandler<GetIOSDevicesQuery, List<IOSDeviceResponse>>
    {
        private readonly IOSDeviceService _deviceService;

        public GetIOSDevicesHandler(IOSDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        public async Task<List<IOSDeviceResponse>> Handle(GetIOSDevicesQuery request, CancellationToken cancellationToken)
        {
            return await _deviceService.GetDevicesAsync();
        }
    }
}

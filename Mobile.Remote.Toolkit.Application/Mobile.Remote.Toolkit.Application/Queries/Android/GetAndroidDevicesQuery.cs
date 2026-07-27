using MediatR;

using Mobile.Remote.Toolkit.Application.Services.Android;
using Mobile.Remote.Toolkit.Application.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Application.Queries.Android
{
    public class GetAndroidDevicesQuery : IRequest<List<AndroidDeviceResponse>>
    {
        public bool? ActiveOnly { get; set; }

        public class GetAndroidDevicesQueryHandler : IRequestHandler<GetAndroidDevicesQuery, List<AndroidDeviceResponse>>
        {
            private readonly IAndroidDeviceService _androidService;

            public GetAndroidDevicesQueryHandler(IAndroidDeviceService androidService)
            {
                _androidService = androidService;
            }

            public async Task<List<AndroidDeviceResponse>> Handle(GetAndroidDevicesQuery request, CancellationToken cancellationToken)
            {
                var devices = await _androidService.GetConnectedDevicesAsync();

                if (request.ActiveOnly.HasValue && request.ActiveOnly.Value)
                {
                    devices = devices.Where(d => d.Active).ToList();
                }

                return devices;
            }
        }
    }
}

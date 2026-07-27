using MediatR;

using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Queries.iOS
{
    public sealed class GetIOSDeviceInfoQuery : IRequest<IOSDeviceResponse>
    {
        public string Udid { get; set; }

        public class GetIOSDeviceInfoQueryHandler : IRequestHandler<GetIOSDeviceInfoQuery, IOSDeviceResponse>
        {
            private readonly IIOSDeviceService _iosService;

            public GetIOSDeviceInfoQueryHandler(IIOSDeviceService iosService)
            {
                _iosService = iosService;
            }

            public async Task<IOSDeviceResponse> Handle(GetIOSDeviceInfoQuery request, CancellationToken cancellationToken)
                => await _iosService.GetDeviceInfoAsync(request.Udid);
        }
    }
}

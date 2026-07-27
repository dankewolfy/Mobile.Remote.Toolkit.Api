using MediatR;

using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Queries.iOS
{
    public sealed class GetIOSDeviceStatusQuery : IRequest<Dictionary<string, object>>
    {
        public string Udid { get; set; }

        public class GetIOSDeviceStatusQueryHandler : IRequestHandler<GetIOSDeviceStatusQuery, Dictionary<string, object>>
        {
            private readonly IIOSDeviceService _iosService;

            public GetIOSDeviceStatusQueryHandler(IIOSDeviceService iosService)
            {
                _iosService = iosService;
            }

            public async Task<Dictionary<string, object>> Handle(GetIOSDeviceStatusQuery request, CancellationToken cancellationToken)
                => await _iosService.GetDeviceStatusAsync(request.Udid);
        }
    }
}

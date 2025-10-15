using MediatR;
using Mobile.Remote.Toolkit.Business.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Business.Services.iOS;
using System.Threading;
using System.Threading.Tasks;

namespace Mobile.Remote.Toolkit.Business.Handlers.iOS
{
    public class GetIOSDeviceInfoHandler : IRequestHandler<Queries.iOS.GetIOSDeviceInfoQuery, IOSDeviceResponse>
    {
        private readonly IOSDeviceService _service;
        public GetIOSDeviceInfoHandler(IOSDeviceService service)
        {
            _service = service;
        }
        public async Task<IOSDeviceResponse> Handle(Queries.iOS.GetIOSDeviceInfoQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetDeviceInfoAsync(request.Udid);
        }
    }
}

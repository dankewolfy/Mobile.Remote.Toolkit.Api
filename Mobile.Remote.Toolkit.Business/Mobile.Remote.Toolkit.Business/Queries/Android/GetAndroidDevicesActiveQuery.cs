using MediatR;

using Mobile.Remote.Toolkit.Business.Queries.Base;
using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Queries.Android
{
    public sealed class GetAndroidDevicesActiveQuery : BaseQuery<List<AndroidDeviceResponse>>
    {
        public class GetAndroidDevicesActiveQueryHandler : AndroidBaseQueryHandler<GetAndroidDevicesActiveQuery, List<AndroidDeviceResponse>>
        {
            public GetAndroidDevicesActiveQueryHandler(IAndroidDeviceService androidDeviceService, IMediator mediator) : base(mediator, androidDeviceService) { }

            public override async Task<List<AndroidDeviceResponse>> Handle(GetAndroidDevicesActiveQuery request, CancellationToken cancellationToken)
            {
                var devices = await AndroidService.GetConnectedDeviceSerialsAsync();

                return [.. devices.Where(d => d.Active)];
            }
        }
    }
}

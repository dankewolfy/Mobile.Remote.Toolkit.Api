using MediatR;

using Mobile.Remote.Toolkit.Business.Queries.Base;
using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Queries.Android
{
    public sealed class GetAndroidDevicesQuery : BaseQuery<List<AndroidDeviceResponse>>
    {
        public class GetAndroidDevicesQueryHandler : AndroidBaseQueryHandler<GetAndroidDevicesQuery, List<AndroidDeviceResponse>>
        {
            public GetAndroidDevicesQueryHandler(IAndroidDeviceService androidDeviceService, IMediator mediator) : base(mediator, androidDeviceService) { }

            public override async Task<List<AndroidDeviceResponse>> Handle(GetAndroidDevicesQuery request, CancellationToken cancellationToken)
                => await AndroidService.GetConnectedDevicesAsync();
        }
    }
}

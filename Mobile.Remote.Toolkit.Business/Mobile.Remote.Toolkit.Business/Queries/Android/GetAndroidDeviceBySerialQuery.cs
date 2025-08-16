#nullable disable

using MediatR;

using Mobile.Remote.Toolkit.Business.Queries.Base;
using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Queries.Android
{
    /// <summary>
    /// Query para obtener información detallada de un dispositivo Android por su serial
    /// </summary>
    public sealed class GetAndroidDeviceBySerialQuery : BaseQuery<AndroidDeviceResponse>
    {
        public string Serial { get; set; }

        public class GetAndroidDeviceByIdQueryHandler : AndroidBaseQueryHandler<GetAndroidDeviceBySerialQuery, AndroidDeviceResponse>
        {
            public GetAndroidDeviceByIdQueryHandler(IAndroidDeviceService androidDeviceService, IMediator mediator) : base(mediator, androidDeviceService) { }

            public override async Task<AndroidDeviceResponse> Handle(GetAndroidDeviceBySerialQuery request, CancellationToken cancellationToken)
            {
                var devices = await AndroidService.GetConnectedDevicesAsync();
                var device = devices.FirstOrDefault(d => d.Serial == request.Serial);

                return devices is null ? null : device;
            }
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Queries.Base;

namespace Mobile.Remote.Toolkit.Business.Queries.Android
{
    /// <summary>
    /// Query para obtener el estado de un dispositivo Android específico
    /// </summary>
    public sealed class GetDeviceStatusQuery : BaseQuery<Dictionary<string, object>>
    {
        public string Serial { get; set; }

        /// <summary>
        /// Handler para obtener estado del dispositivo
        /// </summary>
        public class GetDeviceStatusQueryHandler : AndroidBaseQueryHandler<GetDeviceStatusQuery, Dictionary<string, object>>
        {
            public GetDeviceStatusQueryHandler(
                IAndroidDeviceService androidDeviceService, 
                IMediator mediator, 
                ILogger<GetDeviceStatusQueryHandler> logger) 
                : base(mediator, androidDeviceService)
            {
            }

            public override async Task<Dictionary<string, object>> Handle(GetDeviceStatusQuery request, CancellationToken cancellationToken)
            {
                return await AndroidService.GetDeviceStatusAsync(request.Serial);
            }
        }
    }
}

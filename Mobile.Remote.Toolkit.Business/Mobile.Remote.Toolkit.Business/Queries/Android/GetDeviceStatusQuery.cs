using MediatR;

using Mobile.Remote.Toolkit.Business.Services.Android;

namespace Mobile.Remote.Toolkit.Business.Queries.Android
{
    /// <summary>
    /// Query para obtener el estado de un dispositivo Android específico
    /// </summary>
    public sealed class GetDeviceStatusQuery : IRequest<Dictionary<string, object>>
    {
        public string Serial { get; set; }

        /// <summary>
        /// Handler para obtener estado del dispositivo
        /// </summary>
        public class GetDeviceStatusQueryHandler : IRequestHandler<GetDeviceStatusQuery, Dictionary<string, object>>
        {
            private readonly IAndroidDeviceService _androidService;

            public GetDeviceStatusQueryHandler(IAndroidDeviceService androidService)
            {
                _androidService = androidService;
            }

            public async Task<Dictionary<string, object>> Handle(GetDeviceStatusQuery request, CancellationToken cancellationToken)
            {
                return await _androidService.GetDeviceStatusAsync(request.Serial);
            }
        }
    }
}

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Commands.Base;
using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Requests.Android;

namespace Mobile.Remote.Toolkit.Business.Commands.Android
{
    /// <summary>
    /// Comando para detener mirror de dispositivo Android
    /// </summary>
    public sealed class StopMirrorCommandHandler : AndroidBaseCommandHandler<StopMirrorRequest, ActionResponse>
    {
        public StopMirrorCommandHandler(IAndroidDeviceService androidDeviceService, ILogger<StopMirrorCommandHandler> logger) : base(androidDeviceService, logger) { }

        public override async Task<ActionResponse> Handle(StopMirrorRequest request, CancellationToken cancellationToken)
            => await AndroidDeviceService.StopMirrorAsync(request.Serial);
    }
}

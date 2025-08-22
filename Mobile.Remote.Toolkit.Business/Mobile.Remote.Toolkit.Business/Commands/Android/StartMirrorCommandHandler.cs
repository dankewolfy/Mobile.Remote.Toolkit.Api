using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Commands.Base;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Models.Requests.Android;

namespace Mobile.Remote.Toolkit.Business.Commands.Android
{
    /// <summary>
    /// Comando para iniciar mirror de dispositivo Android
    /// </summary>
    public sealed class StartMirrorCommandHandler : AndroidBaseCommandHandler<StartMirrorRequest, ActionResponse>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="androidDeviceService"></param>
        /// <param name="logger"></param>
        public StartMirrorCommandHandler(IAndroidDeviceService androidDeviceService, ILogger<StartMirrorCommandHandler> logger) : base(androidDeviceService, logger) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<ActionResponse> Handle(StartMirrorRequest request, CancellationToken cancellationToken)
            => await AndroidDeviceService.StartMirrorAsync(request.Serial, request.Options);
    }
}

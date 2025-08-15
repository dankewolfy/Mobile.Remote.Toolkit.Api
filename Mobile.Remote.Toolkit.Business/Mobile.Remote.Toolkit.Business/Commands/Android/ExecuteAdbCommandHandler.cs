#nullable disable

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Commands.Base;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Models.Requests.Android;

namespace Mobile.Remote.Toolkit.Business.Commands.Android
{
    /// <summary>
    /// Comando para ejecutar comandos ADB personalizados en dispositivos Android
    /// </summary>
    public sealed class ExecuteAdbCommandHandler : AndroidBaseCommandHandler<ExecuteAdbCommandRequest, ActionResponse>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="androidDeviceService"></param>
        /// <param name="logger"></param>
        public ExecuteAdbCommandHandler(IAndroidDeviceService androidDeviceService, ILogger<ExecuteAdbCommandHandler> logger) : base(androidDeviceService, logger) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<ActionResponse> Handle(ExecuteAdbCommandRequest request, CancellationToken cancellationToken)
            => await AndroidDeviceService.ExecuteAdbCommandAsync(request.Serial, request.Command);
    }
}

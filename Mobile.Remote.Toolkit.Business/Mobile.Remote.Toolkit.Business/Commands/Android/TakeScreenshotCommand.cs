using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Commands.Base;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Models.Requests.Android;

namespace Mobile.Remote.Toolkit.Business.Commands.Android
{
    /// <summary>
    /// Comando para tomar screenshot de dispositivo Android
    /// </summary>
    public sealed class TakeScreenshotCommand : AndroidBaseCommandHandler<ScreenshotRequest, ScreenshotResponse>
    {
        public TakeScreenshotCommand(IAndroidDeviceService androidDeviceService, ILogger<TakeScreenshotCommand> logger) : base(androidDeviceService, logger) { }

        public override async Task<ScreenshotResponse> Handle(ScreenshotRequest request, CancellationToken cancellationToken)
            => await AndroidDeviceService.TakeScreenshotAsync(request.Serial, request.Filename);
    }
}

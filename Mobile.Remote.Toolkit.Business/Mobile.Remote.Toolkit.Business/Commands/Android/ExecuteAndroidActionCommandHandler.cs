#nullable disable

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Commands.Base;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Commands.Android;

namespace Mobile.Remote.Toolkit.Business.Commands.Android
{
    public sealed class ExecuteAndroidActionCommandHandler : AndroidBaseCommandHandler<ExecuteAndroidActionCommand, ActionResponse>
    {
        public ExecuteAndroidActionCommandHandler(IAndroidDeviceService androidDeviceService, ILogger<ExecuteAndroidActionCommandHandler> logger)
            : base(androidDeviceService, logger) { }

        public override async Task<ActionResponse> Handle(ExecuteAndroidActionCommand request, CancellationToken cancellationToken)
            => await AndroidDeviceService.ExecuteActionAsync(request.Serial, request.Action, null, request.Payload);
    }
}

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Commands.Base;
using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Commands.iOS
{
    public sealed class ExecuteIOSActionCommandHandler : IOSBaseCommandHandler<ExecuteIOSActionCommand, ActionResponse>
    {
        public ExecuteIOSActionCommandHandler(IIOSDeviceService iosService, ILogger<ExecuteIOSActionCommandHandler> logger)
            : base(iosService, logger)
        {
        }

        public override async Task<ActionResponse> Handle(ExecuteIOSActionCommand request, CancellationToken cancellationToken)
            => await IOSDeviceService.ExecuteActionAsync(request.Udid, request.Action, null, request.Payload);
    }
}

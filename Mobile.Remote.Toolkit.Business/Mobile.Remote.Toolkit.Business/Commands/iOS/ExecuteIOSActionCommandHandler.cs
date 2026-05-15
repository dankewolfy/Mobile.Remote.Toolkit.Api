using MediatR;

using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Services.iOS;

namespace Mobile.Remote.Toolkit.Business.Commands.iOS
{
    public sealed class ExecuteIOSActionCommandHandler : IRequestHandler<ExecuteIOSActionCommand, ActionResponse>
    {
        private readonly IIOSDeviceService _iosService;

        public ExecuteIOSActionCommandHandler(IIOSDeviceService iosService)
        {
            _iosService = iosService;
        }

        public async Task<ActionResponse> Handle(ExecuteIOSActionCommand request, CancellationToken cancellationToken)
            => await _iosService.ExecuteActionAsync(request.Udid, request.Action, null, request.Payload);
    }
}

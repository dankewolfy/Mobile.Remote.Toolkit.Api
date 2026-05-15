using MediatR;

using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Services.iOS;

namespace Mobile.Remote.Toolkit.Business.Commands.iOS
{
    public sealed class StopIOSMirrorCommand : IRequest<ActionResponse>
    {
        public string Udid { get; set; }

        public class StopIOSMirrorCommandHandler : IRequestHandler<StopIOSMirrorCommand, ActionResponse>
        {
            private readonly IIOSDeviceService _iosService;

            public StopIOSMirrorCommandHandler(IIOSDeviceService iosService)
            {
                _iosService = iosService;
            }

            public async Task<ActionResponse> Handle(StopIOSMirrorCommand request, CancellationToken cancellationToken)
                => await _iosService.StopMirrorAsync(request.Udid);
        }
    }
}

using MediatR;

using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Commands.iOS
{
    public sealed class StartIOSMirrorCommand : IRequest<ActionResponse>
    {
        public string Udid { get; set; }
        public Dictionary<string, object> Options { get; set; } = new();

        public class StartIOSMirrorCommandHandler : IRequestHandler<StartIOSMirrorCommand, ActionResponse>
        {
            private readonly IIOSDeviceService _iosService;

            public StartIOSMirrorCommandHandler(IIOSDeviceService iosService)
            {
                _iosService = iosService;
            }

            public async Task<ActionResponse> Handle(StartIOSMirrorCommand request, CancellationToken cancellationToken)
                => await _iosService.StartMirrorAsync(request.Udid, request.Options);
        }
    }
}

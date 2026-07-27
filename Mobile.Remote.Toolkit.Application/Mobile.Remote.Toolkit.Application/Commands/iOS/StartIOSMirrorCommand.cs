using MediatR;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Commands.Base;
using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Commands.iOS
{
    public sealed class StartIOSMirrorCommand : IRequest<ActionResponse>
    {
        public string Udid { get; set; }
        public Dictionary<string, object> Options { get; set; } = new();

        public class StartIOSMirrorCommandHandler : IOSBaseCommandHandler<StartIOSMirrorCommand, ActionResponse>
        {
            public StartIOSMirrorCommandHandler(IIOSDeviceService iosService, ILogger<StartIOSMirrorCommandHandler> logger)
                : base(iosService, logger)
            {
            }

            public override async Task<ActionResponse> Handle(StartIOSMirrorCommand request, CancellationToken cancellationToken)
                => await IOSDeviceService.StartMirrorAsync(request.Udid, request.Options);
        }
    }
}

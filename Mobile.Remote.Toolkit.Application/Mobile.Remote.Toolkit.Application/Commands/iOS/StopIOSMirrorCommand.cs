using MediatR;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Commands.Base;
using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Commands.iOS
{
    public sealed class StopIOSMirrorCommand : IRequest<ActionResponse>
    {
        public string Udid { get; set; }

        public class StopIOSMirrorCommandHandler : IOSBaseCommandHandler<StopIOSMirrorCommand, ActionResponse>
        {
            public StopIOSMirrorCommandHandler(IIOSDeviceService iosService, ILogger<StopIOSMirrorCommandHandler> logger)
                : base(iosService, logger)
            {
            }

            public override async Task<ActionResponse> Handle(StopIOSMirrorCommand request, CancellationToken cancellationToken)
                => await IOSDeviceService.StopMirrorAsync(request.Udid);
        }
    }
}

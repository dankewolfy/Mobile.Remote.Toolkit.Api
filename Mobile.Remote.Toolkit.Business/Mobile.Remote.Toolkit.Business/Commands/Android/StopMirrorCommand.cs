using MediatR;

using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Services.Android;

namespace Mobile.Remote.Toolkit.Business.Commands.Android
{
    /// <summary>
    /// Comando para detener mirror de dispositivo Android
    /// </summary>
    public sealed class StopMirrorCommand : IRequest<ActionResponse>
    {
        public string Serial { get; set; }

        public class StopMirrorCommandHandler : IRequestHandler<StopMirrorCommand, ActionResponse>
        {
            private readonly IAndroidDeviceService _androidService;

            public StopMirrorCommandHandler(IAndroidDeviceService androidService)
            {
                _androidService = androidService;
            }

            public async Task<ActionResponse> Handle(StopMirrorCommand request, CancellationToken cancellationToken)
                => await _androidService.StopMirrorAsync(request.Serial);
        }
    }
}

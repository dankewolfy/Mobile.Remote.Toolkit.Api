using MediatR;

using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Services.Android;

namespace Mobile.Remote.Toolkit.Application.Commands.Android
{
    /// <summary>
    /// Comando para iniciar mirror de dispositivo Android
    /// </summary>
    public sealed class StartMirrorCommand : IRequest<ActionResponse>
    {
        public string Serial { get; set; }
        public Dictionary<string, object> Options { get; set; } = new();

        public class StartMirrorCommandHandler : IRequestHandler<StartMirrorCommand, ActionResponse>
        {
            private readonly IAndroidDeviceService _androidService;

            public StartMirrorCommandHandler(IAndroidDeviceService androidService)
            {
                _androidService = androidService;
            }

            public async Task<ActionResponse> Handle(StartMirrorCommand request, CancellationToken cancellationToken)
                => await _androidService.StartMirrorAsync(request.Serial, request.Options);
        }
    }
}

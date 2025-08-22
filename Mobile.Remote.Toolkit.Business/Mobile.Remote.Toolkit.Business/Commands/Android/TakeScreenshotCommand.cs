#nullable disable

using MediatR;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Services.Android;

namespace Mobile.Remote.Toolkit.Business.Commands.Android
{
    /// <summary>
    /// Comando para tomar screenshot de dispositivo Android
    /// </summary>
    public sealed class TakeScreenshotCommand : IRequest<ScreenshotResponse>
    {
        public string Serial { get; set; }
        public string Filename { get; set; }

        public class TakeScreenshotCommandHandler : IRequestHandler<TakeScreenshotCommand, ScreenshotResponse>
        {
            private readonly IAndroidDeviceService _androidService;

            public TakeScreenshotCommandHandler(IAndroidDeviceService androidService)
            {
                _androidService = androidService;
            }

            public async Task<ScreenshotResponse> Handle(TakeScreenshotCommand request, CancellationToken cancellationToken)
                => await _androidService.TakeScreenshotAsync(request.Serial, request.Filename);
        }
    }
}

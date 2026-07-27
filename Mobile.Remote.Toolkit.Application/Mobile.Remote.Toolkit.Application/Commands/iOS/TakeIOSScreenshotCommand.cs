using MediatR;

using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Commands.iOS
{
    public sealed class TakeIOSScreenshotCommand : IRequest<ActionResponse>
    {
        public string Udid { get; set; }
        public string Filename { get; set; }

        public class TakeIOSScreenshotCommandHandler : IRequestHandler<TakeIOSScreenshotCommand, ActionResponse>
        {
            private readonly IIOSDeviceService _iosService;

            public TakeIOSScreenshotCommandHandler(IIOSDeviceService iosService)
            {
                _iosService = iosService;
            }

            public async Task<ActionResponse> Handle(TakeIOSScreenshotCommand request, CancellationToken cancellationToken)
                => await _iosService.TakeScreenshotAsync(request.Udid, request.Filename);
        }
    }
}

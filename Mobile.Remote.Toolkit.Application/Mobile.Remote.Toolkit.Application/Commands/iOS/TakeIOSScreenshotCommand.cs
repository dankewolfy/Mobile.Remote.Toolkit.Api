using MediatR;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Commands.Base;
using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Commands.iOS
{
    public sealed class TakeIOSScreenshotCommand : IRequest<ActionResponse>
    {
        public string Udid { get; set; }
        public string Filename { get; set; }

        public class TakeIOSScreenshotCommandHandler : IOSBaseCommandHandler<TakeIOSScreenshotCommand, ActionResponse>
        {
            public TakeIOSScreenshotCommandHandler(IIOSDeviceService iosService, ILogger<TakeIOSScreenshotCommandHandler> logger)
                : base(iosService, logger)
            {
            }

            public override async Task<ActionResponse> Handle(TakeIOSScreenshotCommand request, CancellationToken cancellationToken)
                => await IOSDeviceService.TakeScreenshotAsync(request.Udid, request.Filename);
        }
    }
}

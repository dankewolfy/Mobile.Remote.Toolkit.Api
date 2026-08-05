using MediatR;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Commands.iOS
{
    public sealed class InstallIOSDeviceKitCommand : IRequest<ActionResponse>
    {
        public string Udid { get; set; }

        public class InstallIOSDeviceKitCommandHandler : IRequestHandler<InstallIOSDeviceKitCommand, ActionResponse>
        {
            private readonly IIOSDeviceKitInstallService _installService;
            private readonly ILogger<InstallIOSDeviceKitCommandHandler> _logger;

            public InstallIOSDeviceKitCommandHandler(IIOSDeviceKitInstallService installService, ILogger<InstallIOSDeviceKitCommandHandler> logger)
            {
                _installService = installService ?? throw new ArgumentNullException(nameof(installService));
                _logger = logger;
            }

            public async Task<ActionResponse> Handle(InstallIOSDeviceKitCommand request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Solicitud de instalación de DeviceKit recibida para {Udid}", request.Udid);
                return await _installService.InstallAsync(request.Udid);
            }
        }
    }
}

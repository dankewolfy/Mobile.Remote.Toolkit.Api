using MediatR;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Application.Commands.iOS
{
    public sealed class InstallIOSDriverCommand : IRequest<ActionResponse>
    {
        public class InstallIOSDriverCommandHandler : IRequestHandler<InstallIOSDriverCommand, ActionResponse>
        {
            private readonly IIOSDriverService _driverService;
            private readonly ILogger<InstallIOSDriverCommandHandler> _logger;

            public InstallIOSDriverCommandHandler(IIOSDriverService driverService, ILogger<InstallIOSDriverCommandHandler> logger)
            {
                _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
                _logger = logger;
            }

            public async Task<ActionResponse> Handle(InstallIOSDriverCommand request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Solicitud de instalación del driver de Apple recibida");
                return await _driverService.InstallAsync();
            }
        }
    }
}

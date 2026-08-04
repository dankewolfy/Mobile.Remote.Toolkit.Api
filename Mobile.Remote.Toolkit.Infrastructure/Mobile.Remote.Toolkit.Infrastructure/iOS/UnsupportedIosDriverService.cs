using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Models.Responses.iOS;
using Mobile.Remote.Toolkit.Application.Services.iOS;

namespace Mobile.Remote.Toolkit.Infrastructure.iOS
{
    // El driver "Apple Mobile Device Service" es un concepto exclusivo de Windows;
    // en Linux/macOS el SO ya expone el iPhone por USB sin este paso.
    public class UnsupportedIosDriverService : IIOSDriverService
    {
        public Task<IOSDriverStatusResponse> GetStatusAsync()
            => Task.FromResult(new IOSDriverStatusResponse
            {
                Installed = true,
                Supported = false,
                Message = "Este sistema operativo no requiere un driver adicional para detectar el iPhone."
            });

        public Task<ActionResponse> InstallAsync()
            => Task.FromResult(new ActionResponse
            {
                Success = false,
                Message = "La instalación de drivers solo aplica en Windows."
            });
    }
}

#nullable disable

using MediatR;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Utils;
using Mobile.Remote.Toolkit.Business.Queries.Base;
using Mobile.Remote.Toolkit.Business.Services.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;

namespace Mobile.Remote.Toolkit.Business.Queries.Android
{
    public sealed class GetToolsStatusQuery : BaseQuery<AndroidToolsStatusResponse>
    {
        public class GetToolsStatusQueryHandler : AndroidBaseQueryHandler<GetToolsStatusQuery, AndroidToolsStatusResponse>
        {
            private readonly IProcessHelper _processHelper;

            public GetToolsStatusQueryHandler(IAndroidDeviceService androidDeviceService, IProcessHelper processHelper, IMediator mediator, ILogger<GetToolsStatusQueryHandler> logger) : base(mediator, androidDeviceService)
            {
                _processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));
            }

            public async override Task<AndroidToolsStatusResponse> Handle(GetToolsStatusQuery request, CancellationToken cancellationToken)
            {
                var response = new AndroidToolsStatusResponse();
                try
                {
                    // Verificar ADB
                    var adbResult = await _processHelper.ExecuteCommandAsync("adb", "version");
                    response.AdbAvailable = adbResult.Success;
                    response.AdbVersion = adbResult.Output?.Split('\n')[0];

                    // Verificar scrcpy
                    var scrcpyResult = await _processHelper.ExecuteCommandAsync("scrcpy", "--version");
                    response.ScrcpyAvailable = scrcpyResult.Success;
                    response.ScrcpyVersion = scrcpyResult.Output?.Split('\n')[0];
                }
                catch (Exception ex)
                {
                    response.AdbAvailable = false;
                    response.ScrcpyAvailable = false;
                    response.AdbVersion = $"Error: {ex.Message}";
                    response.ScrcpyVersion = $"Error: {ex.Message}";
                }
                return response;
            }
        }
    }
}

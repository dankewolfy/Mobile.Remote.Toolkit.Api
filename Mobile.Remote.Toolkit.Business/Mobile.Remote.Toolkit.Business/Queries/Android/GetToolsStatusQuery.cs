using MediatR;
using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Commands.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;
using Mobile.Remote.Toolkit.Business.Queries.Base;
using Mobile.Remote.Toolkit.Business.Services.Android;

namespace Mobile.Remote.Toolkit.Business.Queries.Android
{
    public sealed class GetToolsStatusQuery : BaseQuery<AndroidToolsStatusResponse>
    {

        public class GetToolsStatusQueryHandler : AndroidBaseQueryHandler<GetToolsStatusQuery, AndroidToolsStatusResponse>
        {
            public GetToolsStatusQueryHandler(
                IAndroidDeviceService androidDeviceService, 
                IMediator mediator, 
                ILogger<GetToolsStatusQueryHandler> logger) 
                : base(mediator, androidDeviceService)
            {
            }

            public async override Task<AndroidToolsStatusResponse> Handle(GetToolsStatusQuery request, CancellationToken cancellationToken)
            {
                //try
                //{
                //    var adbResult = await Mediator.Send(new ExecuteAdbCommand
                //    {
                //        Serial = "",
                //        Command = "version"
                //    }, cancellationToken);

                //    var scrcpyResult = await Mediator.Send(new ExecuteScCommand
                //    {
                //        Serial = "",
                //        Command = "version"
                //    }, cancellationToken);

                //    return new AndroidToolsStatusResponse
                //    {
                //        AdbAvailable = adbResult.Success,
                //        AdbVersion = adbResult.Data?.GetValueOrDefault("output"),
                //        ScrcpyAvailable = true,
                //        ScrcpyVersion = true
                //    });
                //}
                //catch (Exception ex)
                //{
                //    return Ok(new
                //    {
                //        success = false,
                //        error = ex.Message,
                //        tools = new
                //        {
                //            adb_available = false,
                //            scrcpy_available = false
                //        }
                //    });
                //}
                return default;
            }
        }
    }
}

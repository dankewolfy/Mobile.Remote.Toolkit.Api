using MediatR;
using Mobile.Remote.Toolkit.Business.Commands.Android;
using Mobile.Remote.Toolkit.Business.Models.Responses.Android;
using Mobile.Remote.Toolkit.Business.Queries.Base;

namespace Mobile.Remote.Toolkit.Business.Queries.Android
{
    public sealed class GetToolsStatusQuery : IRequest<AndroidToolsStatusResponse>
    {


        public class GetToolsStatusQueryHandler : BaseQueryHandler<GetToolsStatusQuery, AndroidToolsStatusResponse>
        {
            public GetToolsStatusQueryHandler(IMediator mediator) : base(mediator) { }

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

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Utils;
using Mobile.Remote.Toolkit.Business.Commands.Base;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Requests.Process;

namespace Mobile.Remote.Toolkit.Business.Commands.Process
{
    public class GetProcessesCommandHandler : BaseCommandHandler<GetProcessesRequest, List<ProcessInfoResponse>>
    {
        private readonly IProcessHelper _processHelper;

        public GetProcessesCommandHandler(IProcessHelper processHelper, ILogger<GetProcessesCommandHandler> logger) : base(logger)
        {
            _processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));
        }

        public override async Task<List<ProcessInfoResponse>> Handle(GetProcessesRequest request, CancellationToken cancellationToken)
        {
            var processIds = await _processHelper.GetProcessIdsByNameAsync(request.Tool.ToString());
            var processes = processIds.Select(pid =>
            {
                try
                {
                    var proc = System.Diagnostics.Process.GetProcessById(pid);
                    return new ProcessInfoResponse
                    {
                        Pid = pid,
                        Name = proc.ProcessName,
                        StartTime = proc.StartTime,
                        MainWindowTitle = proc.MainWindowTitle
                    };
                }
                catch
                {
                    return null;
                }
            }).Where(p => p != null).Cast<ProcessInfoResponse>().ToList();

            return processes;
        }
    }
}

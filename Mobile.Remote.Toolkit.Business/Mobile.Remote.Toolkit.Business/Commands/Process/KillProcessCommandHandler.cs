using MediatR;

using Microsoft.Extensions.Logging;

using Mobile.Remote.Toolkit.Business.Utils;
using Mobile.Remote.Toolkit.Business.Commands.Base;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Requests.Process;

namespace Mobile.Remote.Toolkit.Business.Commands.Process
{
    public class KillProcessCommandHandler : BaseCommandHandler<KillProcessRequest, KillProcessResponse>
    {
        private readonly IProcessHelper _processHelper;
        
        public KillProcessCommandHandler(IProcessHelper processHelper, ILogger<KillProcessCommandHandler> logger) : base(logger)
        {
            _processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));
        }

        public override async Task<KillProcessResponse> Handle(KillProcessRequest request, CancellationToken cancellationToken)
        {
            var activePids = await _processHelper.GetProcessIdsByNameAsync(request.Tool.ToString());
            var killed = new List<int>();
            var failed = new List<int>();
            foreach (var pid in request.Pids)
            {
                if (activePids.Contains(pid))
                {
                    var ok = await _processHelper.KillProcessAsync(pid);
                    if (ok) killed.Add(pid);
                    else failed.Add(pid);
                }
                else
                {
                    failed.Add(pid);
                }
            }

            return new KillProcessResponse { Killed = killed, Failed = failed };
        }
    }
}

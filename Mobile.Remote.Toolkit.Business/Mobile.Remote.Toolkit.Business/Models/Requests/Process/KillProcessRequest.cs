using MediatR;

using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Models.Requests.Process
{
    public class KillProcessRequest : IRequest<KillProcessResponse>
    {
        public CommandTool Tool { get; }
        public Patform Platform { get; }
        public List<int> Pids { get; }
        public KillProcessRequest(CommandTool tool, Patform platform, List<int> pids)
        {
            Tool = tool;
            Platform = platform;
            Pids = pids;
        }
    }
}

using MediatR;

using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Models.Requests.Process
{
    public class GetProcessesRequest : IRequest<List<ProcessInfoResponse>>
    {
        public CommandTool Tool { get; }
        public Patform Platform { get; }
        public GetProcessesRequest(CommandTool tool, Patform platform)
        {
            Tool = tool;
            Platform = platform;
        }
    }
}

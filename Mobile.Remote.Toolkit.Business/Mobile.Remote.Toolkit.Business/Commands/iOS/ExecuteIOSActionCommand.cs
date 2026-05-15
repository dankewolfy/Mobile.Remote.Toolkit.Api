using MediatR;

using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Commands.iOS
{
    public sealed class ExecuteIOSActionCommand : IRequest<ActionResponse>
    {
        public string Udid { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> Payload { get; set; }
    }
}

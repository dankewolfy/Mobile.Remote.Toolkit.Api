using MediatR;
using Mobile.Remote.Toolkit.Application.Models.Responses;

namespace Mobile.Remote.Toolkit.Application.Commands.Android
{
    public class ExecuteAndroidActionCommand : IRequest<ActionResponse>
    {
        public string Serial { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> Payload { get; set; }
    }
}

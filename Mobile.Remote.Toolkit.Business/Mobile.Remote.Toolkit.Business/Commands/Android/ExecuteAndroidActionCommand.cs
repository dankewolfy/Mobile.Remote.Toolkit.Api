#nullable disable

using MediatR;

using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Commands.Android
{
    public class ExecuteAndroidActionCommand : IRequest<ActionResponse>
    {
        public string Serial { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> Payload { get; set; }
    }
}

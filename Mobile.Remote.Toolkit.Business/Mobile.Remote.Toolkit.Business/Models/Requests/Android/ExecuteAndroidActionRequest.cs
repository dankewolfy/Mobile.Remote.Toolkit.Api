#nullable disable

using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Requests.Base;

namespace Mobile.Remote.Toolkit.Business.Models.Requests.Android
{
    public sealed record ExecuteAndroidActionRequest : BaseRequest<ActionResponse>
    {
        public string Action { get; set; }
        public Dictionary<string, object> Payload { get; set; }
    }
}

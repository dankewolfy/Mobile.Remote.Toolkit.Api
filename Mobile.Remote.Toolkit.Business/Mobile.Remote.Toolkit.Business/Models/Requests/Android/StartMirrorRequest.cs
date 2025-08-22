#nullable disable

using System.Runtime.Serialization;

using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Requests.Base;

namespace Mobile.Remote.Toolkit.Business.Models.Requests.Android
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public sealed record StartMirrorRequest : BaseRequest<ActionResponse>
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Options { get; set; } = [];
    }
}

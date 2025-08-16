#nullable disable

using MediatR;

using System.Runtime.Serialization;

using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Models.Requests.Base;

namespace Mobile.Remote.Toolkit.Business.Models.Requests.Android
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public sealed record ExecuteScrcpyCommandRequest : BaseRequest<ActionResponse>
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Serial { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Command { get; set; }
    }
}

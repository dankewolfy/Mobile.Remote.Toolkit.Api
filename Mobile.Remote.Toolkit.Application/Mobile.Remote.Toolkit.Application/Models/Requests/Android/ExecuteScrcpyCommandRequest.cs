#nullable disable

using MediatR;

using System.Runtime.Serialization;

using Mobile.Remote.Toolkit.Application.Models.Responses;
using Mobile.Remote.Toolkit.Application.Models.Requests.Base;

namespace Mobile.Remote.Toolkit.Application.Models.Requests.Android
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public sealed class ExecuteScrcpyCommandRequest : BaseRequest, IRequest<ActionResponse>
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

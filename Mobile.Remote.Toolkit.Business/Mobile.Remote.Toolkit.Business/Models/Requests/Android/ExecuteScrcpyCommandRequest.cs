#nullable disable

using Mobile.Remote.Toolkit.Business.Models.Android;
using Mobile.Remote.Toolkit.Business.Models.Requests.Base;
using Mobile.Remote.Toolkit.Business.Models.Responses;
using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Models.Requests.Android
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public sealed record ExecuteScrcpyCommandRequest : BaseRequest<CommandResultResponse>
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

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Models.Responses
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class KillProcessResponse
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public List<int> Killed { get; set; } = [];

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public List<int> Failed { get; set; } = [];
    }
}

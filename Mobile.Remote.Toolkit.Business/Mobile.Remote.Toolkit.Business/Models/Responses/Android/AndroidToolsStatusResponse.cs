#nullable disable

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Models.Responses.Android
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public sealed class AndroidToolsStatusResponse
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public bool AdbAvailable { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string AdbVersion { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public bool ScrcpyAvailable { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string ScrcpyVersion { get; set; }
    }
}

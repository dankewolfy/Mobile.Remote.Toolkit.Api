#nullable disable

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Models.Responses.Android
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class AndroidDeviceResponse
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Serial { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Model { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Platform { get; set; } = "android";

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string AndroidVersion { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Brand { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public bool Active { get; set; }
    }
}

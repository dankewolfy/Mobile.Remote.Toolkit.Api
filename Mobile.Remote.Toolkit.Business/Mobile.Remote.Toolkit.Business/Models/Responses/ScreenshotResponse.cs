#nullable disable

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Models.Responses
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class ScreenshotResponse
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public byte[] Bytes { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Filename { get; set; }
    }
}
#nullable disable

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Models.Responses
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class ActionResponse
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public bool Success { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Message { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Error { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Data { get; set; }

        public ActionResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}

#nullable disable

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Application.Models.Responses.iOS
{
    [DataContract]
    public class IOSDriverStatusResponse
    {
        [DataMember]
        public bool Installed { get; set; }

        [DataMember]
        public bool Supported { get; set; } = true;

        [DataMember]
        public string Message { get; set; }
    }
}

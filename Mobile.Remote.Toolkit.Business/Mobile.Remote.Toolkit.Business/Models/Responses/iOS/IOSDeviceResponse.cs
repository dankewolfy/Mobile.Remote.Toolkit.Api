#nullable disable

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Models.Responses.iOS
{
    [DataContract]
    public class IOSDeviceResponse
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Udid { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Model { get; set; }

        [DataMember]
        public string ProductType { get; set; }

        [DataMember]
        public string IOSVersion { get; set; }

        [DataMember]
        public string SerialNumber { get; set; }

        [DataMember]
        public string Platform { get; set; } = "ios";

        [DataMember]
        public bool Active { get; set; }
    }
}

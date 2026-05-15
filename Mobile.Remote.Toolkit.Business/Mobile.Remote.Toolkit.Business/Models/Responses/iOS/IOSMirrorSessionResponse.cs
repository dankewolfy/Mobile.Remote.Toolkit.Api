#nullable disable

using System.Runtime.Serialization;

namespace Mobile.Remote.Toolkit.Business.Models.Responses.iOS
{
    [DataContract]
    public class IOSMirrorSessionResponse
    {
        [DataMember]
        public string Udid { get; set; }

        [DataMember]
        public string Mode { get; set; }

        [DataMember]
        public string Executable { get; set; }

        [DataMember]
        public string Arguments { get; set; }

        [DataMember]
        public int ProcessId { get; set; }

        [DataMember]
        public DateTime StartedAtUtc { get; set; }
    }
}

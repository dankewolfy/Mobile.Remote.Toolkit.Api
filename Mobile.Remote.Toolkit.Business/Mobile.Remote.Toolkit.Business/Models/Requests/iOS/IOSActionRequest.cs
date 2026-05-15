namespace Mobile.Remote.Toolkit.Business.Models.Requests.iOS
{
    public class IOSActionRequest
    {
        public string Udid { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> Payload { get; set; }
    }
}

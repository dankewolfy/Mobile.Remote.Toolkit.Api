namespace Mobile.Remote.Toolkit.Application.Models.Requests.iOS
{
    public class IOSActionRequest
    {
        public string? Udid { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> Payload { get; set; }
    }
}

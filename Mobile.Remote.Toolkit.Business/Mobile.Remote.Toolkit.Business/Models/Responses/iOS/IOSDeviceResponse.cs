using System.Collections.Generic;

namespace Mobile.Remote.Toolkit.Business.Models.Responses.iOS
{
    public class IOSDeviceResponse
    {
        public string Udid { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public string ProductType { get; set; }
        public string ProductVersion { get; set; }
        public bool IsOnline { get; set; }
    }
}

namespace Mobile.Remote.Toolkit.Business.Models.Requests.Android
{
    public class AndroidActionRequest
    {
        /// <summary>
        /// 
        /// </summary>
        public string Serial { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, object> Payload { get; set; }
    }
}

#nullable disable

namespace Mobile.Remote.Toolkit.Business.Models.Responses
{
    public class ProcessResultResponse
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public int ExitCode { get; set; }
    }
}

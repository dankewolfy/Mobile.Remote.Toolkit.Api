#nullable disable

namespace Mobile.Remote.Toolkit.Business.Utils
{
    public class ProcessResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public int ExitCode { get; set; }
    }
}

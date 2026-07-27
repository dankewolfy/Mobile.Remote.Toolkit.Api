namespace Mobile.Remote.Toolkit.Application.Utils
{
    public class ProcessResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public int ExitCode { get; set; }
    }
}

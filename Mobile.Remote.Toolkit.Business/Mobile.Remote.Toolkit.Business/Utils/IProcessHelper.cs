namespace Mobile.Remote.Toolkit.Business.Utils
{
    public interface IProcessHelper
    {
        Task<ProcessResult> ExecuteCommandAsync(string fileName, string arguments, int timeoutSeconds = 30);
        Task<bool> IsProcessRunningAsync(string processName);
        Task<List<int>> GetProcessIdsByNameAsync(string processName);
        Task<bool> KillProcessAsync(int processId);
    }
}

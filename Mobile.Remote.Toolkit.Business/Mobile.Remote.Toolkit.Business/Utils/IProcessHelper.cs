using Mobile.Remote.Toolkit.Business.Models;
using Mobile.Remote.Toolkit.Business.Models.Responses;

namespace Mobile.Remote.Toolkit.Business.Utils
{
    public interface IProcessHelper
    {
        Task<ProcessResultResponse> ExecuteCommandAsync(CommandTool tool, string arguments, int timeoutSeconds = 30);
        Task<bool> IsProcessRunningAsync(string processName);
        Task<List<int>> GetProcessIdsByNameAsync(string processName);
        Task<bool> KillProcessAsync(int processId);
        Task<byte[]> ExecuteCommandBinaryAsync(CommandTool tool, string args, int timeoutSeconds = 30);
    }
}

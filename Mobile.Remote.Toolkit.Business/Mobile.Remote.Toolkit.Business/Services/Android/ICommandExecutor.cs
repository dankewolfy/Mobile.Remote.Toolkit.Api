using Mobile.Remote.Toolkit.Business.Models.Android;

namespace Mobile.Remote.Toolkit.Business.Services.Android
{
    public interface ICommandExecutor
    {
        Task<CommandResultResponse> ExecuteAsync(string executable, string arguments);
        Task<List<int>> GetProcessIdsByNameAsync(string processName);
        Task<bool> KillProcessAsync(int processId);
    }
}

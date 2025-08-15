namespace Mobile.Remote.Toolkit.Business.Utils
{
    public interface IFileService
    {
        Task<string> GetScreenshotsFolderAsync();
        Task<List<string>> GetScreenshotFilesAsync(string serial = null);
        Task<byte[]> GetFileContentAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        Task<FileInfo> GetFileInfoAsync(string filePath);
        Task<string> SaveScreenshotAsync(string serial, byte[] content, string filename = null);
    }
}

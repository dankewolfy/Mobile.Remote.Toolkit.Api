namespace Mobile.Remote.Toolkit.Business.Utils
{
    public class FileService : IFileService
    {
        private readonly string _screenshotsFolder;

        public FileService()
        {
            var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            _screenshotsFolder = Path.Combine(picturesPath, "ScrcpyManager");
            Directory.CreateDirectory(_screenshotsFolder);
        }

        public async Task<string> GetScreenshotsFolderAsync()
        {
            return _screenshotsFolder;
        }

        public async Task<List<string>> GetScreenshotFilesAsync(string serial = null)
        {
            try
            {
                var files = Directory.GetFiles(_screenshotsFolder, "*.png")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToList();

                if (!string.IsNullOrEmpty(serial))
                {
                    files = files.Where(f => Path.GetFileName(f).Contains(serial)).ToList();
                }

                return files.Select(f => Path.GetFileName(f)).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task<byte[]> GetFileContentAsync(string filePath)
        {
            var fullPath = Path.Combine(_screenshotsFolder, filePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Archivo no encontrado");

            return await File.ReadAllBytesAsync(fullPath);
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_screenshotsFolder, filePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<FileInfo> GetFileInfoAsync(string filePath)
        {
            var fullPath = Path.Combine(_screenshotsFolder, filePath);
            return File.Exists(fullPath) ? new FileInfo(fullPath) : null;
        }

        public async Task<string> SaveScreenshotAsync(string serial, byte[] content, string filename = null)
        {
            if (string.IsNullOrEmpty(filename))
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                filename = $"screenshot_{serial}_{timestamp}.png";
            }

            var fullPath = Path.Combine(_screenshotsFolder, filename);
            await File.WriteAllBytesAsync(fullPath, content);

            return filename;
        }
    }
}

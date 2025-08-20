using System.IO.Compression;

using Microsoft.AspNetCore.Mvc;

namespace Mobile.Remote.Toolkit.Controllers
{
    [ApiController]
    [Route("api/extensions")]
    public class ExtensionsController : ControllerBase
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<ExtensionsController> _logger;

        public ExtensionsController(IWebHostEnvironment hostEnvironment, ILogger<ExtensionsController> logger)
        {
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene información de la extensión Mobile Remote Toolkit
        /// </summary>
        [HttpGet("mobile-remote-toolkit/info")]
        public IActionResult GetExtensionInfo()
        {
            try
            {
                var extensionPath = Path.Combine(_hostEnvironment.WebRootPath, "browser-extension");
                var manifestPath = Path.Combine(extensionPath, "manifest.json");

                if (!System.IO.File.Exists(manifestPath))
                {
                    return NotFound(new { error = "Extensión no encontrada" });
                }

                var manifestContent = System.IO.File.ReadAllText(manifestPath);
                var manifest = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(manifestContent);

                return Ok(new
                {
                    name = "Mobile Remote Toolkit Extension",
                    version = manifest?.GetValueOrDefault("version", "1.0.0"),
                    description = "Extensión para acceso directo a dispositivos USB Android",
                    available = true,
                    size = GetDirectorySize(extensionPath),
                    lastModified = Directory.GetLastWriteTime(extensionPath)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo información de extensión");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Descarga la extensión Mobile Remote Toolkit empaquetada
        /// </summary>
        [HttpGet("mobile-remote-toolkit/download")]
        public IActionResult DownloadExtension()
        {
            try
            {
                var extensionPath = Path.Combine(_hostEnvironment.WebRootPath, "browser-extension");
                
                if (!Directory.Exists(extensionPath))
                {
                    return NotFound(new { error = "Extensión no encontrada" });
                }

                // Crear ZIP en memoria
                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    AddDirectoryToZip(archive, extensionPath, "");
                }

                memoryStream.Position = 0;
                var fileName = $"mobile-remote-toolkit-extension-v{GetExtensionVersion()}.zip";

                return File(memoryStream.ToArray(), "application/zip", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error descargando extensión");
                return StatusCode(500, new { error = "Error generando descarga" });
            }
        }

        /// <summary>
        /// Sube una nueva versión de la extensión (para administradores)
        /// </summary>
        [HttpPost("mobile-remote-toolkit/upload")]
        public async Task<IActionResult> UploadExtension(IFormFile extensionZip)
        {
            try
            {
                if (extensionZip == null || extensionZip.Length == 0)
                {
                    return BadRequest(new { error = "Archivo ZIP requerido" });
                }

                if (!extensionZip.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = "Solo se permiten archivos ZIP" });
                }

                var extensionPath = Path.Combine(_hostEnvironment.WebRootPath, "extensions", "mobile-remote-toolkit");

                // Backup de versión anterior si existe
                if (Directory.Exists(extensionPath))
                {
                    var backupPath = $"{extensionPath}_backup_{DateTime.Now:yyyyMMddHHmmss}";
                    Directory.Move(extensionPath, backupPath);
                }

                // Crear directorio y extraer
                Directory.CreateDirectory(extensionPath);

                using var stream = extensionZip.OpenReadStream();
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                await Task.Run(() => archive.ExtractToDirectory(extensionPath));

                // Validar que tiene manifest.json
                var manifestPath = Path.Combine(extensionPath, "manifest.json");
                if (!System.IO.File.Exists(manifestPath))
                {
                    return BadRequest(new { error = "Extensión inválida: manifest.json no encontrado" });
                }

                _logger.LogInformation("Extensión actualizada exitosamente");

                return Ok(new
                {
                    success = true,
                    message = "Extensión actualizada exitosamente",
                    version = GetExtensionVersion()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo extensión");
                return StatusCode(500, new { error = "Error procesando archivo" });
            }
        }

        /// <summary>
        /// Lista todas las extensiones disponibles
        /// </summary>
        [HttpGet]
        public IActionResult GetExtensions()
        {
            try
            {
                var extensionsPath = _hostEnvironment.WebRootPath;
                
                if (!Directory.Exists(extensionsPath))
                {
                    Directory.CreateDirectory(extensionsPath);
                    return Ok(new { extensions = new object[0] });
                }

                var extensions = new List<object>();
                
                // Mobile Remote Toolkit Extension
                var mobileRemoteToolkitPath = Path.Combine(extensionsPath, "browser-extension");
                if (Directory.Exists(mobileRemoteToolkitPath))
                {
                    extensions.Add(new
                    {
                        id = "mobile-remote-toolkit",
                        name = "Mobile Remote Toolkit Extension",
                        version = GetExtensionVersion(),
                        description = "Extensión para acceso directo a dispositivos USB Android",
                        available = true,
                        downloadUrl = "/api/extensions/mobile-remote-toolkit/download",
                        size = GetDirectorySize(mobileRemoteToolkitPath),
                        lastModified = Directory.GetLastWriteTime(mobileRemoteToolkitPath)
                    });
                }

                return Ok(new { extensions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando extensiones");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene el estado de instalación de extensiones
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetExtensionStatus()
        {
            try
            {
                var extensionsPath = _hostEnvironment.WebRootPath;
                var mobileRemoteToolkitPath = Path.Combine(extensionsPath, "browser-extension");

                return Ok(new
                {
                    extensionsPath,
                    mobileRemoteToolkit = new
                    {
                        installed = Directory.Exists(mobileRemoteToolkitPath),
                        version = Directory.Exists(mobileRemoteToolkitPath) ? GetExtensionVersion() : null,
                        path = mobileRemoteToolkitPath
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estado de extensiones");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        #region Helper Methods

        private void AddDirectoryToZip(ZipArchive archive, string sourcePath, string entryPath)
        {
            var files = Directory.GetFiles(sourcePath);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var entryName = string.IsNullOrEmpty(entryPath) ? fileName : $"{entryPath}/{fileName}";
                
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open();
                using var fileStream = System.IO.File.OpenRead(file);
                fileStream.CopyTo(entryStream);
            }

            var directories = Directory.GetDirectories(sourcePath);
            foreach (var directory in directories)
            {
                var directoryName = Path.GetFileName(directory);
                var newEntryPath = string.IsNullOrEmpty(entryPath) ? directoryName : $"{entryPath}/{directoryName}";
                AddDirectoryToZip(archive, directory, newEntryPath);
            }
        }

        private long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path)) return 0;

            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            return files.Sum(file => new FileInfo(file).Length);
        }

        private string GetExtensionVersion()
        {
            try
            {
                var extensionPath = Path.Combine(_hostEnvironment.WebRootPath, "browser-extension");
                var manifestPath = Path.Combine(extensionPath, "manifest.json");

                if (!System.IO.File.Exists(manifestPath))
                    return "1.0.0";

                var manifestContent = System.IO.File.ReadAllText(manifestPath);
                var manifest = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(manifestContent);

                return manifest?.GetValueOrDefault("version", "1.0.0")?.ToString() ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }

        #endregion
    }
}

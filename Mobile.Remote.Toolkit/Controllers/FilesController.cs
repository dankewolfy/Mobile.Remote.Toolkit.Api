using Microsoft.AspNetCore.Mvc;

using Mobile.Remote.Toolkit.Business.Models.Responses;
using Mobile.Remote.Toolkit.Business.Utils;

namespace Mobile.Remote.Toolkit.Api.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }

        /// <summary>
        /// Obtiene la lista de screenshots
        /// </summary>
        /// <param name="serial">Filtrar por serial de dispositivo</param>
        /// <returns>Lista de archivos</returns>
        [HttpGet("screenshots")]
        public async Task<ActionResult<List<string>>> GetScreenshots([FromQuery] string serial)
        {
            var files = await _fileService.GetScreenshotFilesAsync(serial);
            return Ok(files);
        }

        /// <summary>
        /// Descarga un screenshot específico
        /// </summary>
        /// <param name="filename">Nombre del archivo</param>
        /// <returns>Archivo</returns>
        [HttpGet("screenshots/{filename}")]
        public async Task<IActionResult> DownloadScreenshot(string filename)
        {
            try
            {
                var content = await _fileService.GetFileContentAsync(filename);
                return File(content, "image/png", filename);
            }
            catch (FileNotFoundException)
            {
                return NotFound("Archivo no encontrado");
            }
        }

        /// <summary>
        /// Elimina un screenshot
        /// </summary>
        /// <param name="filename">Nombre del archivo</param>
        /// <returns>Resultado de la operación</returns>
        [HttpDelete("screenshots/{filename}")]
        public async Task<ActionResult<ActionResponse>> DeleteScreenshot(string filename)
        {
            var result = await _fileService.DeleteFileAsync(filename);

            return Ok(new ActionResponse(
                result,
                result ? "Archivo eliminado correctamente" : "Error eliminando archivo"
            ));
        }

        /// <summary>
        /// Obtiene información de un archivo
        /// </summary>
        /// <param name="filename">Nombre del archivo</param>
        /// <returns>Información del archivo</returns>
        [HttpGet("screenshots/{filename}/info")]
        public async Task<ActionResult<object>> GetScreenshotInfo(string filename)
        {
            var fileInfo = await _fileService.GetFileInfoAsync(filename);

            if (fileInfo == null)
                return NotFound("Archivo no encontrado");

            return Ok(new
            {
                name = fileInfo.Name,
                size = fileInfo.Length,
                created = fileInfo.CreationTime,
                modified = fileInfo.LastWriteTime,
                fullPath = fileInfo.FullName
            });
        }
    }
}

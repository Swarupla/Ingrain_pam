using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;

namespace Accenture.MyWizard.Ingrain.WebService.Controllers
{
    public class FileDownloadController : MyWizardControllerBase
    {
        private string GetMimeType(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out string contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }

        /*[HttpGet]
        [Route("api/DownloadFile")]
        public IActionResult DownloadFile(string fileName)
        {
            var filepath = Path.Combine(
                           Directory.GetCurrentDirectory(),
                           "Files", fileName);

            var mimeType = this.GetMimeType(fileName);

            byte[] fileBytes = System.IO.File.ReadAllBytes(filepath);

            return new FileContentResult(fileBytes, mimeType)
            {
                FileDownloadName = fileName
            };
        }*/
    }
}
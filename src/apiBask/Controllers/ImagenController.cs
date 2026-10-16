using apiBask.Models.Storage;
using Microsoft.AspNetCore.Mvc;

namespace apiBask.Controllers
{
    // Solo se pueden subir imágenes; se muestran desde la URL pública del blob.
    [Route("/Imagen")]
    public class ImagenController(FileService fileService) : ApiControllerBase
    {
        private const long TamañoMaximo = 5 * 1024 * 1024;

        [HttpPost("Upload")]
        [RequestSizeLimit(TamañoMaximo + 64 * 1024)]
        public async Task<IActionResult> Upload(IFormFile image)
        {
            if (image.Length == 0 || image.Length > TamañoMaximo)
                return Invalido("La imagen tiene que pesar menos de 5 MB.");
            if (!FileService.EsImagen(image))
                return Invalido("El archivo tiene que ser una imagen JPG, PNG, WEBP o GIF.");

            return Ok(await fileService.UploadAsync(image));
        }
    }
}

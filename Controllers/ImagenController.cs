using apiBask.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace apiBask.Controllers
{
    [ApiController]
    [Authorize]
    public class ImagenController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ImagenController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpPost("upload")]
        public IActionResult UploadImage([FromForm] IFormFile image)
        {
            Response respuesta = new Response();
            try
            {
                if (image != null && image.Length > 0)
                {
                    if (!image.ContentType.StartsWith("image/"))
                    {
                        respuesta.mensaje = "El archivo subido no es una imagen válida.";
                        return BadRequest(respuesta);
                    }
                    if (image.Length > 1000000) 
                    {
                        respuesta.mensaje = "La imágen supera el tamaño máximo de 1mb";
                        return BadRequest(respuesta);
                    }
                    // Obtiene la ruta base donde se encuentra la carpeta wwwroot
                    var basePath = _configuration.GetValue<string>(WebHostDefaults.ContentRootKey);

                    // Genera una ruta para la carpeta donde se guardarán las imágenes
                    var uploadPath = Path.Combine(basePath, "wwwroot", "images");

                    // Genera una ruta completa para la imagen
                    var imagePath = Path.Combine(uploadPath, image.FileName);

                    // Copia la imagen al directorio de destino
                    using (var fileStream = new FileStream(imagePath, FileMode.Create))
                    {
                        image.CopyTo(fileStream);
                    }

                    // Genera la URL completa de la imagen
                    string imageUrl = Path.Combine("/images", image.FileName);


                    respuesta.exito = 1;
                    respuesta.mensaje = "Se subió la imagen.";
                    respuesta.data = imageUrl;
                }
                else
                {
                    respuesta.mensaje = "No se proporcionó ninguna imagen para subir.";
                    return BadRequest(respuesta);
                }
            }
            catch (Exception)
            {
                respuesta.mensaje = "Error al subir imagen.";
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }        
    }
}

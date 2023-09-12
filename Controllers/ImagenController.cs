using apiBask.Models.Response;
using apiBask.Models.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace apiBask.Controllers
{
    [ApiController]
    [Route("/Imagen")]
    //[Authorize]
    public class ImagenController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly BasketContext _basketContext;
        private readonly FileService _fileService;
        public ImagenController(IConfiguration configuration, FileService fileService, BasketContext basketContext)
        {
            _configuration = configuration;            
            _basketContext = basketContext;
            _fileService = new FileService(_basketContext);
        }

        [HttpGet]
        
        public async Task<IActionResult> ListAllBlobs() 
        {
            var result = await _fileService.ListAsync();
            return Ok(result);
        }

        [HttpPost("Upload")]
        public async Task<IActionResult> Upload(IFormFile image) 
        {
            var result = await _fileService.UploadAsync(image);
            return Ok(result);
        }

        [HttpGet]
        [Route("filename")]
        public async Task<IActionResult> Download(string filename) 
        {
            var result = await _fileService.DownloadAsync(filename);
            return File(result.Content, result.ContentType, result.Name);
        }

        [HttpDelete]
        [Route("filename")]
        public async Task<IActionResult> Delete(string filename) 
        {
            var result = _fileService.DeleteAsync(filename);
            return Ok(result);
        }            
    }
}

using apiBask.Models.Request;
using apiBask.Models.Response;
using apiBask.Services;
using Microsoft.AspNetCore.Mvc;

namespace apiBask.Controllers
{
    [ApiController]
    [Route("/Usuario")]
    public class UsuarioController(IUserService userService, LigaDemo demo) : ControllerBase
    {
        [HttpPost("Registro")]
        public async Task<IActionResult> Crear(RegisterRequest model)
        {
            if (!await userService.RegistrarAsync(model))
                return BadRequest(new Response { mensaje = "El usuario ya existe." });
            return Ok(new Response { exito = 1, mensaje = "Registro con éxito." });
        }

        [HttpPost("LogIn")]
        public async Task<IActionResult> LogIn(LoginRequest model)
        {
            // Quien entra con la cuenta de la demo ve los datos de ejemplo limpios si ya tocaba reiniciarlos.
            await demo.ReiniciarSiHaceFaltaAsync(HttpContext.RequestServices, model.Usuario);
            var userResponse = await userService.AuthAsync(model);
            if (userResponse == null)
                return Ok(new Response { mensaje = "Usuario o contraseña incorrectos." });
            return Ok(new Response { exito = 1, data = userResponse });
        }
    }
}

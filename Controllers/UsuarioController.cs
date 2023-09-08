using apiBask.Models;
using apiBask.Models.Request;
using apiBask.Models.Response;
using apiBask.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace apiBask.Controllers
{
    [ApiController]
    [Route("/Usuario")]


    public class UsuarioController : ControllerBase
    {
        private IUserService _userService;
        private readonly BasketContext _dbContext;
        public UsuarioController(IUserService userService, BasketContext dbContext)
        {
            _userService = userService;
            _dbContext = dbContext;
        }

        [HttpPost("Registro")]
        public IActionResult Crear(RegisterRequest model)
        {
            Response respuesta = new Response();
            try
            {
                
                    Usuario user = new Usuario();
                    user.Usuario1 = model.Usuario;
                    user.Pass = model.Pass;
                _dbContext.Usuarios.Add(user);
                _dbContext.SaveChanges();
                    respuesta.exito = 1;
                    respuesta.mensaje = "Registro con éxito.";
                
            }
            catch (Exception)
            {
                respuesta.mensaje = "El usuario ya existe.";
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }

        [HttpPost("LogIn")]
        public IActionResult LogIn(LoginRequest model)
        {
            Response respuesta = new Response();

            var userResponse = _userService.Auth(model);

            if (userResponse == null)
            {
                respuesta.mensaje = "Usuario o contraseña incorrectos.";
                return BadRequest(respuesta);
            }

            respuesta.exito = 1;
            respuesta.data = userResponse;

            return Ok(respuesta);           
        }
    }
}

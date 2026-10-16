using System.Security.Claims;
using apiBask.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace apiBask.Controllers
{
    // Base de los controladores de datos: piden login y saben quién es el usuario.
    // Las respuestas mantienen la forma { exito, mensaje, data } que usa el front.
    [ApiController]
    [Authorize]
    public abstract class ApiControllerBase : ControllerBase
    {
        // El usuario siempre sale del token; nunca de lo que manda el cliente.
        protected string UsuarioActual => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        protected IActionResult Exito(string mensaje, object? data = null) =>
            Ok(new Response { exito = 1, mensaje = mensaje, data = data });

        // Lo que no existe o es de otro usuario responde igual que antes: 200 con exito = 0.
        protected IActionResult NoEncontrado(string mensaje) =>
            Ok(new Response { mensaje = mensaje });

        protected IActionResult Invalido(string mensaje) =>
            BadRequest(new Response { mensaje = mensaje });
    }
}

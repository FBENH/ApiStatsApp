using apiBask.Models.Request;
using apiBask.Models;
using apiBask.Services;
using Microsoft.AspNetCore.Mvc;
using apiBask.Models.Response;
using Microsoft.AspNetCore.Authorization;

namespace apiBask.Controllers
{
    [ApiController]
    [Route("/StatPartido")]
    [Authorize]
    public class StatPartidoController : ControllerBase    
    {
        private readonly BasketContext _dbContext;

        public StatPartidoController(BasketContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpPost("Crear")]
            public IActionResult CrearStat(StatPartidoRequest model)
            {
                Response respuesta = new Response();
                try
                {
                    
                        EstadisticaPartido _stat = new EstadisticaPartido();
                        _stat.PartidoId = model.PartidoId;
                        _stat.EquipoId = model.EquipoId;
                        _stat.Puntos = model.Puntos;
                        _stat.Perdidas= model.Perdidas;
                        _stat.Asistencias = model.Asistencias;
                        _stat.Rebotes = model.Rebotes;
                        _stat.RebotesDefensivos = model.RebotesDefensivos;
                        _stat.RebotesOfensivos = model.RebotesOfensivos;
                        _stat.Robos = model.Robos;
                        _stat.Tapones = model.Tapones;
                        _stat.Intentos2Puntos = model.Intentos2Puntos;
                        _stat.Aciertos2Puntos = model.Aciertos2Puntos;
                        _stat.Intentos1Punto = model.Intentos1Punto;
                        _stat.Aciertos1Punto = model.Aciertos1Punto;
                        _stat.Intentos3Puntos = model.Intentos3Puntos;
                        _stat.Aciertos3Puntos = model.Aciertos3Puntos;
                        _stat.Faltas = model.Faltas;
                _dbContext.Add(_stat);
                _dbContext.SaveChanges();
                        respuesta.exito = 1;
                        respuesta.mensaje = "Se creó la estadística de partido correctamente.";
                    
                }
                catch (Exception ex)
                {
                    respuesta.mensaje = ex.Message;
                    return BadRequest(respuesta);
                }
                return Ok(respuesta);
            }           
        }
}

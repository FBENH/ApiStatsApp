using apiBask.Models.Request;
using apiBask.Models;
using Microsoft.AspNetCore.Mvc;
using apiBask.Models.Response;
using Microsoft.AspNetCore.Authorization;

namespace apiBask.Controllers
{
    [ApiController]
    [Route("/StatJugador")]
    [Authorize]
    public class StatJugadorController : ControllerBase
    {
        private readonly BasketContext _dbContext;

        public StatJugadorController(BasketContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("Crear")]
        public IActionResult CrearStat(StatJugadorRequest model)
        {
            Response respuesta = new Response();
            try
            {
                
                    EstadisticaJugador _stat = new EstadisticaJugador();
                    _stat.PartidoId = model.PartidoId;
                    _stat.JugadorId = model.JugadorId;
                    _stat.Puntos = model.Puntos;
                    _stat.Perdidas = model.Perdidas;
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
                    _stat.Minutos = TimeSpan.FromMilliseconds( model.Minutos);
                    _dbContext.Add(_stat);
                    _dbContext.SaveChanges();
                    respuesta.exito = 1;
                    respuesta.mensaje = "Se creó la estadística de jugador correctamente.";
                
            }
            catch (Exception ex)
            {
                respuesta.mensaje = ex.Message;
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }        

        [HttpGet("ListarPorJugador")]
        public IActionResult ListarPorJugador(int idJugador) 
        {
            Response respuesta = new Response();
            try
            {
                
                    List<EstadisticaJugador> estadistica = new List<EstadisticaJugador>();
                    estadistica = _dbContext.EstadisticaJugadors.Where(e => e.JugadorId == idJugador)
                        .ToList<EstadisticaJugador>();
                    if(estadistica.Any()) 
                    {
                        respuesta.exito = 1;
                        respuesta.mensaje="";
                        respuesta.data = estadistica;
                    }

                
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

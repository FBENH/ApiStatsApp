using apiBask.Models.Request;
using apiBask.Models;
using apiBask.Services;
using Microsoft.AspNetCore.Mvc;
using apiBask.Models.Response;
using Microsoft.EntityFrameworkCore;
using apiBask.Models.Parcial;
using Microsoft.AspNetCore.Authorization;

namespace apiBask.Controllers
{
    [ApiController]
    [Route("/Jugadores")]
    [Authorize]
    
    public class JugadorController : ControllerBase
    {
        private readonly JugadorService jugadorService= new JugadorService();
        private readonly BasketContext _dbContext;

        public JugadorController(BasketContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("Crear")]
        public IActionResult CrearJugador(JugadorRequest model)
        {
            Response respuesta = new Response();
            try
            {                
                    Jugador _jugador = new Jugador();                    
                    _jugador.Altura = model.Altura;
                    _jugador.IdUsuario = model.Usuario;
                    if (model.EquipoId != null) 
                    {
                        var _equipo= _dbContext.Equipos.Find(model.EquipoId);
                        if (_equipo!=null) 
                        {
                            _jugador.EquipoId = _equipo.Id;
                            _jugador.Equipo = _equipo.Nombre;                            
                        }
                    }                    
                    _jugador.Peso = model.Peso;
                    _jugador.Foto = model.Foto;
                    _jugador.Nombre = model.Nombre;
                    _jugador.Posicion = model.Posicion;
                    _jugador.Activo = true;
                    _jugador.Numero = model.Numero;
                    _dbContext.Add(_jugador);
                    _dbContext.SaveChanges();
                    respuesta.exito = 1;
                    respuesta.mensaje = "Se creó el jugador correctamente.";                
            }
            catch (Exception ex)
            {
                respuesta.mensaje = ex.Message;
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }
        [HttpDelete("Eliminar")]
        public IActionResult EliminarJugador(int id)
        {
            Response respuesta = new Response();
            try
            {                  
                var _jugador = _dbContext.Jugadors.Where(d => d.Id == id).FirstOrDefault();
                if(_jugador != null) 
                {
                    if (!jugadorService.TieneDependencias(_dbContext, id))
                    {
                        _dbContext.Remove(_jugador);
                        _dbContext.SaveChanges();
                        respuesta.exito = 1;
                        respuesta.mensaje = "Se eliminó el jugador.";
                    }
                    else
                    {
                        _jugador.Activo = false;
                        _dbContext.Entry(_jugador).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        _dbContext.SaveChanges();
                        respuesta.exito = 1;
                        respuesta.mensaje = "Se eliminó el jugador.";
                    }
                }                
            }
            catch (Exception ex)
            {
                respuesta.mensaje = ex.Message;
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }
        [HttpPut("Modificar")]
        public IActionResult ModificarJugador(JugadorRequest model)
        {
            Response respuesta = new Response();
            try
            {               
                var _jugador = _dbContext.Jugadors.Where(d => d.Id == model.id).FirstOrDefault();
                if (_jugador != null) 
                {
                    _jugador.Nombre = model.Nombre;
                    _jugador.Altura = model.Altura;
                    _jugador.Peso = model.Peso;
                    _jugador.Foto = model.Foto;
                    _jugador.Posicion = model.Posicion;
                    if (model.EquipoId != null)
                    {                        
                        var _equipo = _dbContext.Equipos.Find(model.EquipoId);
                        if (_equipo != null)
                        {
                            _jugador.EquipoId = _equipo.Id;
                            _jugador.Equipo = _equipo.Nombre;
                        }
                    }
                    _dbContext.Jugadors.Entry(_jugador).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    _dbContext.SaveChanges();
                    respuesta.exito = 1;
                    respuesta.mensaje = "Se modificó el jugador con éxito.";

                }
            }
            catch (Exception ex)
            {
                respuesta.mensaje = ex.Message;
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }
        [HttpGet("Listar")]
        public IActionResult ListarJugadores(string idUsuario)
        {
            Response respuesta = new Response();
            List<Jugador> lista = new List<Jugador>();
            try
            {                
                    lista = _dbContext.Jugadors.Where(j=> j.Activo && j.IdUsuario == idUsuario).ToList();
                    respuesta.exito = 1;
                    respuesta.mensaje = "Se cargó la lista con los jugadores";
                    respuesta.data = lista;
                
            }
            catch (Exception)
            {
                respuesta.mensaje = "Error al cargar la lista";
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }

        [HttpGet("ListarPorEquipo")]
        public IActionResult ListarJugadoresEquipo(int equipoId)
        {
            Response respuesta = new Response();
            try
            {                
                    List<Jugador> lista = new List<Jugador>();
                    if (_dbContext.Equipos.Where(e=> e.Activo && e.Id == equipoId).Any()) 
                    {
                        lista = _dbContext.Jugadors.Where(j => j.EquipoId == equipoId).ToList<Jugador>();
                    }                    
                    respuesta.exito = 1;
                    respuesta.mensaje = "Se obtuvieron los jugadores.";
                    respuesta.data = lista;                
            }
            catch (Exception)
            {
                respuesta.mensaje = "Error al listar jugadores.";
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }
        [HttpGet("ListarPorId")]
        public IActionResult ListarPorId(int id) 
        {
            Response respuesta = new Response();
            try
            {                  
                JugadorResponse jugadorResponse = new JugadorResponse();
                var jugador = _dbContext.Jugadors.Include(j => j.EquipoNavigation).
                Include(j=> j.EstadisticaJugadors).
                FirstOrDefault(j => j.Id == id);
                if (jugador != null) 
                {
                    jugadorResponse.Id = jugador.Id;
                    jugadorResponse.Nombre = jugador.Nombre;
                    jugadorResponse.Posicion = jugador.Posicion;
                    jugadorResponse.Foto = jugador.Foto;
                    jugadorResponse.Altura = jugador.Altura;
                    jugadorResponse.Peso = jugador.Peso;
                    jugadorResponse.Numero = jugador.Numero;
                    if (jugador.EquipoNavigation != null && jugador.EquipoNavigation.Activo)
                    {
                        EquipoResponse equipoResponse = new EquipoResponse();
                        equipoResponse.Id = jugador.EquipoNavigation.Id;
                        equipoResponse.Nombre = jugador.EquipoNavigation.Nombre;
                        equipoResponse.Ciudad = jugador.EquipoNavigation.Ciudad;
                        equipoResponse.Escudo = jugador.EquipoNavigation.Escudo;
                        equipoResponse.Entrenador = jugador.EquipoNavigation.Entrenador;
                        jugadorResponse.EquipoId = jugador.EquipoId;
                        jugadorResponse.Equipo = equipoResponse;
                    }
                    if (jugador.EstadisticaJugadors != null)
                    {
                        List<EstadisticaJResponse> estadisticas = new List<EstadisticaJResponse>();
                        foreach (var estadistica in jugador.EstadisticaJugadors)
                        {
                            EstadisticaJResponse estadisticaJ = new EstadisticaJResponse();
                            estadisticaJ.JugadorId = estadistica.JugadorId;
                            estadisticaJ.PartidoId = estadistica.PartidoId;
                            estadisticaJ.Puntos = estadistica.Puntos;
                            estadisticaJ.Asistencias = estadistica.Asistencias;
                            estadisticaJ.Rebotes = estadistica.Rebotes;
                            estadisticaJ.RebotesOfensivos = estadistica.RebotesOfensivos;
                            estadisticaJ.RebotesDefensivos = estadistica.RebotesDefensivos;
                            estadisticaJ.Robos = estadistica.Robos;
                            estadisticaJ.Tapones = estadistica.Tapones;
                            estadisticaJ.Intentos2Puntos = estadistica.Intentos2Puntos;
                            estadisticaJ.Aciertos2Puntos = estadistica.Aciertos2Puntos;
                            estadisticaJ.Intentos3Puntos = estadistica.Intentos3Puntos;
                            estadisticaJ.Aciertos3Puntos = estadistica.Aciertos3Puntos;
                            estadisticaJ.Intentos1Punto = estadistica.Intentos1Punto;
                            estadisticaJ.Aciertos1Punto = estadistica.Aciertos1Punto;
                            estadisticaJ.Faltas = estadistica.Faltas;
                            estadisticaJ.Minutos = estadistica.Minutos;
                            estadisticaJ.Perdidas = estadistica.Perdidas;
                            estadisticaJ.PartidoId = estadistica.PartidoId;
                            estadisticaJ.EquipoId = estadistica.EquipoId;
                            estadisticas.Add(estadisticaJ);
                        }
                        jugadorResponse.estadisticasJugador = estadisticas;
                    }

                    respuesta.exito = 1;
                    respuesta.mensaje = "Se obtuvo el jugador";
                    respuesta.data = jugadorResponse;
                }                
            }
            catch (Exception ex)
            {
                respuesta.mensaje = ex.Message;
            }
            return Ok(respuesta);
        }
    }
}

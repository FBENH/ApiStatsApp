using apiBask.Models.Request;
using apiBask.Models;
using apiBask.Services;
using Microsoft.AspNetCore.Mvc;
using apiBask.Models.Response;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using apiBask.Models.Parcial;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace apiBask.Controllers
{
    [ApiController]
    [Route("/Partido")]
    [Authorize]
    public class PartidoController : ControllerBase
    {
        private readonly PartidoService partidoService = new PartidoService();
        private readonly BasketContext _dbContext;

        public PartidoController(BasketContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("Crear")]
        public IActionResult CrearPartido(CrearPartidoRequest model)
        {
            Response respuesta = new Response();
            try
            {
                
                    using(var transaccion= _dbContext.Database.BeginTransaction()) 
                    {
                        try
                        {
                            Partido _partido = new Partido();
                            _partido.IdUsuario = model.Partido.IdUsuario;
                            _partido.EquipoVisitante = model.Partido.EquipoVisitante;
                            _partido.EquipoLocal = model.Partido.EquipoLocal;
                            _partido.Ganador = model.Partido.Ganador;
                            _partido.Fecha = model.Partido.Fecha;
                            _partido.Lugar = model.Partido.Lugar;
                            _partido.Activo = true;
                        _dbContext.Add(_partido);
                        _dbContext.SaveChanges();
                            //Estadisticas de jugadores
                            foreach(var stat in model.StatJ) 
                            {
                                EstadisticaJugador _stat = new EstadisticaJugador();
                                _stat.PartidoId = stat.PartidoId;
                                _stat.JugadorId = stat.JugadorId;
                                _stat.Puntos = stat.Puntos;
                                _stat.Asistencias = stat.Asistencias;
                                _stat.Rebotes = stat.Rebotes;
                                _stat.RebotesDefensivos = stat.RebotesDefensivos;
                                _stat.RebotesOfensivos = stat.RebotesOfensivos;
                                _stat.Robos = stat.Robos;
                                _stat.Tapones = stat.Tapones;
                                _stat.Intentos2Puntos = stat.Intentos2Puntos;
                                _stat.Aciertos2Puntos = stat.Aciertos2Puntos;
                                _stat.Intentos1Punto = stat.Intentos1Punto;
                                _stat.Aciertos1Punto = stat.Aciertos1Punto;
                                _stat.Intentos3Puntos = stat.Intentos3Puntos;
                                _stat.Aciertos3Puntos = stat.Aciertos3Puntos;
                                _stat.Faltas = stat.Faltas;                                
                                _stat.Minutos = TimeSpan.FromMilliseconds(stat.Minutos);
                                _stat.PartidoId = _partido.Id;
                                _stat.Perdidas = stat.Perdidas;
                                _stat.EquipoId = stat.EquipoId;
                            _dbContext.Add(_stat);                               
                            }
                        _dbContext.SaveChanges();
                            //Estadisticas de equipos
                            foreach (var stat in model.StatP) 
                            {
                                EstadisticaPartido _stat = new EstadisticaPartido();
                                _stat.PartidoId = stat.PartidoId;
                                _stat.EquipoId = stat.EquipoId;
                                _stat.Puntos = stat.Puntos;
                                _stat.Asistencias = stat.Asistencias;
                                _stat.Rebotes = stat.Rebotes;
                                _stat.RebotesDefensivos = stat.RebotesDefensivos;
                                _stat.RebotesOfensivos = stat.RebotesOfensivos;
                                _stat.Robos = stat.Robos;
                                _stat.Tapones = stat.Tapones;
                                _stat.Intentos2Puntos = stat.Intentos2Puntos;
                                _stat.Aciertos2Puntos = stat.Aciertos2Puntos;
                                _stat.Intentos1Punto = stat.Intentos1Punto;
                                _stat.Aciertos1Punto = stat.Aciertos1Punto;
                                _stat.Intentos3Puntos = stat.Intentos3Puntos;
                                _stat.Aciertos3Puntos = stat.Aciertos3Puntos;
                                _stat.Faltas = stat.Faltas;                                
                                _stat.PartidoId = _partido.Id;
                                _stat.Perdidas = stat.Perdidas;
                            _dbContext.Add(_stat);                             
                            }
                        _dbContext.SaveChanges();
                            transaccion.Commit();
                            respuesta.exito = 1;
                            respuesta.mensaje = "Se creó el partido correctamente.";
                            respuesta.data = _partido.Id;
                        }
                        catch (Exception ex)
                        {
                            transaccion.Rollback();
                            respuesta.mensaje = ex.Message;
                            return BadRequest(respuesta);
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
        [HttpDelete("Eliminar")]
        public IActionResult EliminarPartido(int id, string userId) 
        {
            Response respuesta = new Response();
            try
            {                
                var _partido = _dbContext.Partidos.Where(d => d.Id == id && d.IdUsuario == userId)
                .FirstOrDefault();
                if (_partido != null) 
                {
                    if (partidoService.TieneDependencias(_dbContext, id))
                    {
                        _partido.Activo = false;
                        _dbContext.Entry(_partido).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        _dbContext.SaveChanges();

                    }
                    else
                    {
                        _dbContext.Remove(_partido);
                        _dbContext.SaveChanges();
                    }
                }                
            }
            catch (Exception ex)
            {                
                respuesta.mensaje = ex.Message;
                return BadRequest(respuesta);
            }
            respuesta.exito = 1;
            respuesta.mensaje = "Se eliminó el partido.";
            return Ok(respuesta);
        }

        [HttpGet("ListarPorId")]
        public IActionResult ListarPorId(int idPartido) 
        {
            Response respuesta = new Response();
            try
            {                          
                var partido = _dbContext.Partidos.Where(p => p.Id == idPartido && p.Activo).FirstOrDefault();
                PartidoParcial model = new PartidoParcial();
                if(partido != null) 
                {
                    model.Lugar = partido.Lugar;
                    model.Fecha = partido.Fecha;
                    model.Ganador = partido.Ganador;
                    Equipo local = new Equipo();
                    local = _dbContext.Equipos.Where(e => e.Id == partido.EquipoLocal).FirstOrDefault();
                    Equipo visitante = new Equipo();
                    visitante = _dbContext.Equipos.Where(e => e.Id == partido.EquipoVisitante).FirstOrDefault();
                    model.Local = local.Nombre;
                    model.EquipoLocalId = partido.EquipoLocal;
                    model.Visitante = visitante.Nombre;
                    model.EquipoVisitanteId = partido.EquipoVisitante;
                    model.id = partido.Id;
                    respuesta.exito = 1;
                    respuesta.mensaje = "Se obtuvo el partido";
                    respuesta.data = model;
                }               
            }
            catch (Exception ex)
            {
                respuesta.mensaje = ex.Message;
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }

        [HttpGet("ListarTodos")]
        public IActionResult ListarPartidos(string idUsuario) 
        {
            Response respuesta = new Response();
            try
            {
                
                    List<Partido> partidos = _dbContext.Partidos.Include(p=> p.EquipoLocalNavigation).
                        Include(p=> p.EquipoVisitanteNavigation).Where(p=> p.Activo && p.IdUsuario == idUsuario).                        
                        ToList();
                    List<PartidoParcial> pp = new List<PartidoParcial>();
                    foreach(var p in partidos) 
                    {
                        PartidoParcial _partido = new PartidoParcial();
                        _partido.id = p.Id;
                        if(p.EquipoLocalNavigation != null) 
                        {
                        _partido.Local = p.EquipoLocalNavigation.Nombre;
                        _partido.EquipoLocalId = p.EquipoLocalNavigation.Id;
                        }
                        if(p.EquipoVisitanteNavigation != null) 
                        {
                        _partido.Visitante = p.EquipoVisitanteNavigation.Nombre;
                        _partido.EquipoVisitanteId = p.EquipoVisitanteNavigation.Id;
                        }                       
                        _partido.Ganador = p.Ganador;
                        if(_dbContext.Equipos.Where(e=> e.Id == p.Ganador).FirstOrDefault() != null) 
                        {
                            var equipo = _dbContext.Equipos.Where(e => e.Id == p.Ganador).FirstOrDefault();
                            if(equipo != null) 
                            {
                                var ganadorNombre = equipo.Nombre;
                                _partido.GanadorNombre = ganadorNombre;
                            }                           
                        }                        
                        _partido.Lugar = p.Lugar;
                        _partido.Fecha = p.Fecha;
                        pp.Add(_partido);
                    }
                    respuesta.exito = 1;
                    respuesta.mensaje = "Se obtuvieron los partidos";
                    respuesta.data = pp;                    
                
            }
            catch (Exception ex)
            {
                respuesta.mensaje = ex.Message;
                return BadRequest(respuesta);
            }

            return Ok(respuesta);
        }

        [HttpGet("ConStats")]
        public IActionResult PartidoConStats(int id) 
        {
            Response respuesta = new Response();
            try
            {
               
                    var partido = _dbContext.Partidos.Include(p => p.EstadisticaJugadors).
                        Include(p=> p.EstadisticaPartidos).
                        Include(p=> p.EquipoLocalNavigation).
                        Include(p=> p.EquipoVisitanteNavigation).FirstOrDefault(p =>
                    p.Id == id);

                    PartidoResponse partidoCompleto = new PartidoResponse();
                if(partido != null) 
                {
                    partidoCompleto.id = partido.Id;
                    partidoCompleto.Local = partido.EquipoLocalNavigation.Nombre;
                    partidoCompleto.Visitante = partido.EquipoVisitanteNavigation.Nombre;
                    partidoCompleto.EquipoLocalId = partido.EquipoLocal;
                    partidoCompleto.EquipoVisitanteId = partido.EquipoVisitante;
                    partidoCompleto.Ganador = partido.Ganador;
                    if (_dbContext.Equipos.Where(e => e.Id == partido.Ganador).FirstOrDefault() != null)
                    {
                        partidoCompleto.GanadorNombre = _dbContext.Equipos.Where(e => e.Id == partido.Ganador).FirstOrDefault().Nombre;
                    }
                    partidoCompleto.Lugar = partido.Lugar;
                    if (partido.EstadisticaJugadors != null)
                    {
                        List<JugadorResponse> _listaJugador = new List<JugadorResponse>();
                        foreach (var estadistica in partido.EstadisticaJugadors)
                        {
                            EstadisticaJResponse estadisticaJ = new EstadisticaJResponse();
                            JugadorResponse _jugador = new JugadorResponse();
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
                            _jugador.Id = estadistica.JugadorId;
                            var _J = _dbContext.Jugadors.Where(j => j.Id == _jugador.Id).FirstOrDefault();
                            if(_J != null) 
                            {
                                _jugador.Nombre = _J.Nombre;
                                _jugador.Numero = _J.Numero;
                            }   
                            if(_jugador.estadisticasJugador != null) 
                            {
                                _jugador.estadisticasJugador.Add(estadisticaJ);
                            }                            
                            _listaJugador.Add(_jugador);
                        }
                        partidoCompleto.Jugadores = _listaJugador;
                    }
                    if (partido.EstadisticaPartidos != null)
                    {
                        List<EstadisticaPResponse> estadisticas = new List<EstadisticaPResponse>();
                        foreach (var e in partido.EstadisticaPartidos)
                        {
                            EstadisticaPResponse estadistica = new EstadisticaPResponse();
                            estadistica.EquipoId = e.EquipoId;
                            estadistica.PartidoId = e.PartidoId;
                            estadistica.Puntos = e.Puntos;
                            estadistica.Asistencias = e.Asistencias;
                            estadistica.Rebotes = e.Rebotes;
                            estadistica.RebotesOfensivos = e.RebotesOfensivos;
                            estadistica.RebotesDefensivos = e.RebotesDefensivos;
                            estadistica.Robos = e.Robos;
                            estadistica.Tapones = e.Tapones;
                            estadistica.Intentos2Puntos = e.Intentos2Puntos;
                            estadistica.Aciertos2Puntos = e.Aciertos2Puntos;
                            estadistica.Intentos3Puntos = e.Intentos3Puntos;
                            estadistica.Aciertos3Puntos = e.Aciertos3Puntos;
                            estadistica.Intentos1Punto = e.Intentos1Punto;
                            estadistica.Aciertos1Punto = e.Aciertos1Punto;
                            estadistica.Faltas = e.Faltas;
                            estadistica.Perdidas = e.Perdidas;
                            estadistica.PartidoId = e.PartidoId;
                            estadisticas.Add(estadistica);
                        }
                        partidoCompleto.StatPartido = estadisticas;
                    }

                    respuesta.exito = 1;
                    respuesta.mensaje = "Se obtuvo el partido";
                    respuesta.data = partidoCompleto;
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

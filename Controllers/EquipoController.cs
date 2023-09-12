using apiBask.Models;
using apiBask.Models.Common;
using apiBask.Models.Request;
using apiBask.Models.Response;
using apiBask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace apiBask.Controllers
{
    [ApiController]
    [Route("/Equipo")]
    [Authorize]
    public class EquipoController : ControllerBase
    {
        private readonly EquipoService equipoService= new EquipoService();

        private readonly BasketContext _dbContext;

        public EquipoController(BasketContext dbContext)
        {
            _dbContext = dbContext;            
        }

        [HttpPost("Crear")]
        public IActionResult CrearEquipo(EquipoRequest model) 
        {
            Response respuesta = new Response();
            try
            {                
                    Equipo _equipo = new Equipo();
                    _equipo.IdUsuario = model.Usuario;
                    _equipo.Nombre= model.Nombre;
                    _equipo.Ciudad = model.Ciudad;                    
                    _equipo.Entrenador = model.Entrenador;
                    _equipo.Escudo = model.Escudo;
                    _equipo.Activo = true;
                    _dbContext.Add(_equipo);
                    _dbContext.SaveChanges();
                    respuesta.exito = 1;
                    respuesta.mensaje = "Se creó el equipo correctamente.";                
            }
            catch (Exception ex)
            {
                respuesta.mensaje = ex.Message;
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }

        [HttpPost("CrearEquipoRapido")]

        public IActionResult CrearEquipoRapido(EquipoRapidoRequest model) 
        {
            Response respuesta = new Response();
            try
            {                
                    using(var transaccion = _dbContext.Database.BeginTransaction()) 
                    {
                        Equipo _equipo = new Equipo();
                        _equipo.Nombre = model.Nombre;
                        _equipo.IdUsuario = model.IdUsuario;
                        _equipo.Activo = true;
                        List<Jugador> _jugadores = new List<Jugador>();
                        if (model.Jugadores != null)
                        {
                            foreach (var j in model.Jugadores)
                            {
                                Jugador jugador = new Jugador();
                                jugador.Numero = j.Numero;
                                jugador.Nombre = "N/D";
                                jugador.IdUsuario = j.Usuario;
                                jugador.Activo = true;
                                _jugadores.Add(jugador);
                            }
                            _equipo.Jugadors = _jugadores;
                        }
                        _dbContext.Equipos.Add(_equipo);
                        _dbContext.SaveChanges();
                        transaccion.Commit();
                        var ultimoEquipo = _dbContext.Equipos
                        .OrderByDescending(e => e.Id)
                        .FirstOrDefault();
                        respuesta.exito = 1;
                        respuesta.mensaje = "Se creo el equipo";
                        if(ultimoEquipo != null)
                        respuesta.data = ultimoEquipo.Id;
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
        public IActionResult EliminarEquipo(int id) 
        {
            Response respuesta = new Response();
            try
            {                
                var equipo = _dbContext.Equipos.Include(e=> e.Jugadors).FirstOrDefault(d => d.Id == id);
                if (equipo != null) 
                {
                    if (!equipoService.TieneDependencias(_dbContext, id))
                    {
                        _dbContext.Remove(equipo);
                        _dbContext.SaveChanges();
                    }
                    else
                    {
                        equipo.Activo = false;
                        var jugadoresAModificar = equipo.Jugadors.ToList();
                        foreach (var j in jugadoresAModificar)
                        {
                            j.EquipoId = null;
                            j.Equipo = null;
                            _dbContext.Entry(j).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        }
                        _dbContext.Entry(equipo).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
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
            respuesta.mensaje = "Se eliminó el equipo.";
            return Ok(respuesta);
        }
        [HttpPut("Modificar")]
        public IActionResult ModificarEquipo(EquipoRequest model) 
        {
            Response respuesta = new Response();
            try
            {                
                var _equipo = _dbContext.Equipos.Include(e=> e.Jugadors).FirstOrDefault(d => d.Id == model.Id);
                if (_equipo != null) 
                {
                    _equipo.Nombre = model.Nombre;
                    _equipo.Entrenador = model.Entrenador;
                    _equipo.Ciudad = model.Ciudad;
                    if (!model.Escudo.IsNullOrEmpty()) 
                    {
                        _equipo.Escudo = model.Escudo;
                    }                    
                    if (_equipo.Jugadors != null)
                    {
                        List<Jugador> jugadoresAModificar = new List<Jugador>();
                        jugadoresAModificar = _dbContext.Jugadors.Where(j => j.EquipoId == model.Id).ToList();
                        foreach (var j in jugadoresAModificar)
                        {
                            j.Equipo = model.Nombre;
                            _dbContext.Entry(j).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        }
                    }
                    _dbContext.Equipos.Entry(_equipo).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    _dbContext.SaveChanges();
                    respuesta.exito = 1;
                    respuesta.mensaje = "Se modificó el equipo con éxito.";
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
        public IActionResult ListarEquipos(string idUsuario) 
        {
            Response respuesta = new Response();
            List <Equipo> lista = new List<Equipo>();
            try
            {                
                lista = _dbContext.Equipos.Where(e=> e.Activo && e.IdUsuario == idUsuario).ToList();
                respuesta.exito = 1;
                respuesta.mensaje = "Se cargó la lista con los equipos";
                respuesta.data = lista;                
            }
            catch (Exception)
            {
                respuesta.mensaje = "Error al cargar la lista";
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }

        [HttpGet("ListarPorId")]
        public IActionResult ListarPorId( int idEquipo) 
        {
            Response respuesta = new Response();
            try
            {                     
                var equipo = _dbContext.Equipos.Include(e=> e.EstadisticaPartidos).
                Include(e=> e.Jugadors)
                .FirstOrDefault(e=> e.Id == idEquipo);
                EquipoResponse equipoResponse = new EquipoResponse();
                if(equipo != null) 
                {
                    equipoResponse.Id = equipo.Id;
                    equipoResponse.Nombre = equipo.Nombre;
                    equipoResponse.Ciudad = equipo.Ciudad;
                    equipoResponse.Escudo = equipo.Escudo;
                    equipoResponse.Entrenador = equipo.Entrenador;
                    if (equipo.Jugadors != null)
                    {
                        List<JugadorResponse> jugadores = new List<JugadorResponse>();
                        foreach (var j in equipo.Jugadors)
                        {
                            JugadorResponse jugador = new JugadorResponse();
                            jugador.Id = j.Id;
                            jugador.Nombre = j.Nombre;
                            jugador.Posicion = j.Posicion;
                            jugador.EquipoId = j.EquipoId;
                            jugador.Foto = j.Foto;
                            jugador.Altura = j.Altura;
                            jugador.Peso = j.Peso;
                            jugador.Numero = j.Numero;
                            jugadores.Add(jugador);
                        }
                        equipoResponse.Jugadores = jugadores;
                    }
                    if (equipo.EstadisticaPartidos != null)
                    {
                        List<EstadisticaPResponse> estadisticas = new List<EstadisticaPResponse>();
                        foreach (var e in equipo.EstadisticaPartidos)
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
                        equipoResponse.EstadisticasPartidos = estadisticas;
                    }
                    respuesta.exito = 1;
                    respuesta.mensaje = "Se obtuvo el equipo";
                    respuesta.data = equipoResponse;
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

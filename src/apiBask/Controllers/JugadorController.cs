using apiBask.Models;
using apiBask.Models.Request;
using apiBask.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Controllers
{
    [Route("/Jugadores")]
    public class JugadorController(BasketContext db) : ApiControllerBase
    {
        private IQueryable<Jugador> MisJugadores => db.Jugadors.Where(j => j.IdUsuario == UsuarioActual);

        private Task<Equipo?> MiEquipoAsync(int? id) =>
            db.Equipos.FirstOrDefaultAsync(e => e.Id == id && e.IdUsuario == UsuarioActual);

        [HttpPost("Crear")]
        public async Task<IActionResult> CrearJugador(JugadorRequest model)
        {
            var jugador = new Jugador
            {
                IdUsuario = UsuarioActual,
                Nombre = model.Nombre,
                Posicion = model.Posicion,
                Foto = model.Foto,
                Altura = model.Altura,
                Peso = model.Peso,
                Numero = model.Numero,
                Activo = true,
            };
            if (model.EquipoId != null)
            {
                var equipo = await MiEquipoAsync(model.EquipoId);
                if (equipo != null)
                {
                    jugador.EquipoId = equipo.Id;
                    jugador.Equipo = equipo.Nombre;
                }
            }
            db.Jugadors.Add(jugador);
            await db.SaveChangesAsync();
            return Exito("Se creó el jugador correctamente.");
        }

        [HttpDelete("Eliminar")]
        public async Task<IActionResult> EliminarJugador(int id)
        {
            var jugador = await MisJugadores.FirstOrDefaultAsync(j => j.Id == id);
            if (jugador == null) return NoEncontrado("No se encontró el jugador.");

            if (!await JugadorService.TieneDependenciasAsync(db, id))
                db.Remove(jugador);
            else
                jugador.Activo = false;
            await db.SaveChangesAsync();
            return Exito("Se eliminó el jugador.");
        }

        [HttpPut("Modificar")]
        public async Task<IActionResult> ModificarJugador(JugadorRequest model)
        {
            var jugador = await MisJugadores.FirstOrDefaultAsync(j => j.Id == model.id);
            if (jugador == null) return NoEncontrado("No se encontró el jugador.");

            jugador.Nombre = model.Nombre;
            jugador.Altura = model.Altura;
            jugador.Peso = model.Peso;
            if (!string.IsNullOrEmpty(model.Foto))
            {
                jugador.Foto = model.Foto;
            }
            jugador.Posicion = model.Posicion;
            jugador.Numero = model.Numero;
            if (model.EquipoId != null)
            {
                var equipo = await MiEquipoAsync(model.EquipoId);
                if (equipo != null)
                {
                    jugador.EquipoId = equipo.Id;
                    jugador.Equipo = equipo.Nombre;
                }
            }
            await db.SaveChangesAsync();
            return Exito("Se modificó el jugador con éxito.");
        }

        // idUsuario se acepta por compatibilidad con el front, pero se ignora.
        [HttpGet("Listar")]
        public async Task<IActionResult> ListarJugadores(string? idUsuario)
        {
            var lista = await MisJugadores.AsNoTracking().Where(j => j.Activo).ToListAsync();
            return Exito("Se cargó la lista con los jugadores", lista);
        }

        [HttpGet("ListarPorEquipo")]
        public async Task<IActionResult> ListarJugadoresEquipo(int equipoId)
        {
            var lista = await MisJugadores.AsNoTracking()
                .Where(j => j.EquipoId == equipoId && j.EquipoNavigation!.Activo)
                .ToListAsync();
            return Exito("Se obtuvieron los jugadores.", lista);
        }

        [HttpGet("ListarPorId")]
        public async Task<IActionResult> ListarPorId(int id)
        {
            var jugador = await MisJugadores.AsNoTracking()
                .Include(j => j.EquipoNavigation)
                .Include(j => j.EstadisticaJugadors)
                .FirstOrDefaultAsync(j => j.Id == id);
            if (jugador == null) return NoEncontrado("No se encontró el jugador.");

            var response = jugador.ToResponse();
            response.EquipoId = null;
            if (jugador.EquipoNavigation is { Activo: true } equipo)
            {
                response.EquipoId = equipo.Id;
                response.Equipo = equipo.ToResponse();
            }
            response.estadisticasJugador = jugador.EstadisticaJugadors.Select(e => e.ToResponse()).ToList();
            return Exito("Se obtuvo el jugador", response);
        }
    }
}

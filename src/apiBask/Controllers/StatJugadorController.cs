using apiBask.Models;
using apiBask.Models.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Controllers
{
    [Route("/StatJugador")]
    public class StatJugadorController(BasketContext db) : ApiControllerBase
    {
        [HttpPost("Crear")]
        public async Task<IActionResult> CrearStat(StatJugadorRequest model)
        {
            var esMio = await db.Partidos.AnyAsync(p => p.Id == model.PartidoId && p.UsuarioId == UsuarioActual)
                && await db.Jugadores.AnyAsync(j => j.Id == model.JugadorId && j.UsuarioId == UsuarioActual);
            if (!esMio) return Invalido("El partido y el jugador tienen que ser tuyos.");
            if (model.EquipoId == null) return Invalido("La estadística necesita el equipo del jugador.");

            var estadistica = new EstadisticaJugador { PartidoId = model.PartidoId };
            estadistica.CopiarDe(model);
            db.EstadisticasJugador.Add(estadistica);
            await db.SaveChangesAsync();
            return Exito("Se creó la estadística de jugador correctamente.");
        }

        [HttpGet("ListarPorJugador")]
        public async Task<IActionResult> ListarPorJugador(int idJugador)
        {
            var estadisticas = await db.EstadisticasJugador.AsNoTracking()
                .Where(e => e.JugadorId == idJugador && e.Jugador.UsuarioId == UsuarioActual)
                .ToListAsync();
            if (estadisticas.Count == 0) return NoEncontrado("El jugador no tiene estadísticas.");
            return Exito("", estadisticas.Select(e => e.ToResponse()));
        }
    }
}

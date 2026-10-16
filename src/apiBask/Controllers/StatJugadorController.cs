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
            var esMio = await db.Partidos.AnyAsync(p => p.Id == model.PartidoId && p.IdUsuario == UsuarioActual)
                && await db.Jugadors.AnyAsync(j => j.Id == model.JugadorId && j.IdUsuario == UsuarioActual);
            if (!esMio) return Invalido("El partido y el jugador tienen que ser tuyos.");

            var estadistica = new EstadisticaJugador { PartidoId = model.PartidoId };
            estadistica.CopiarDe(model);
            db.EstadisticaJugadors.Add(estadistica);
            await db.SaveChangesAsync();
            return Exito("Se creó la estadística de jugador correctamente.");
        }

        [HttpGet("ListarPorJugador")]
        public async Task<IActionResult> ListarPorJugador(int idJugador)
        {
            var estadisticas = await db.EstadisticaJugadors.AsNoTracking()
                .Where(e => e.JugadorId == idJugador && e.Jugador.IdUsuario == UsuarioActual)
                .ToListAsync();
            if (estadisticas.Count == 0) return NoEncontrado("El jugador no tiene estadísticas.");
            return Exito("", estadisticas);
        }
    }
}

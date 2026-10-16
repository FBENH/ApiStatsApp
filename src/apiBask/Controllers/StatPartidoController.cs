using apiBask.Models;
using apiBask.Models.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Controllers
{
    [Route("/StatPartido")]
    public class StatPartidoController(BasketContext db) : ApiControllerBase
    {
        [HttpPost("Crear")]
        public async Task<IActionResult> CrearStat(StatPartidoRequest model)
        {
            var esMio = await db.Partidos.AnyAsync(p => p.Id == model.PartidoId && p.UsuarioId == UsuarioActual)
                && await db.Equipos.AnyAsync(e => e.Id == model.EquipoId && e.UsuarioId == UsuarioActual);
            if (!esMio) return Invalido("El partido y el equipo tienen que ser tuyos.");

            var estadistica = new EstadisticaPartido { PartidoId = model.PartidoId };
            estadistica.CopiarDe(model);
            db.EstadisticasPartido.Add(estadistica);
            await db.SaveChangesAsync();
            return Exito("Se creó la estadística de partido correctamente.");
        }
    }
}

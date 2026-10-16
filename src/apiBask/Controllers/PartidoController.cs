using apiBask.Models;
using apiBask.Models.Request;
using apiBask.Models.Response;
using apiBask.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Controllers
{
    [Route("/Partido")]
    public class PartidoController(BasketContext db) : ApiControllerBase
    {
        private IQueryable<Partido> MisPartidos => db.Partidos.Where(p => p.UsuarioId == UsuarioActual);

        [HttpPost("Crear")]
        public async Task<IActionResult> CrearPartido(CrearPartidoRequest model)
        {
            var datos = model.Partido;
            if (datos.EquipoLocal == datos.EquipoVisitante)
                return Invalido("El local y el visitante tienen que ser equipos distintos.");
            if (datos.Ganador != null && datos.Ganador != datos.EquipoLocal && datos.Ganador != datos.EquipoVisitante)
                return Invalido("El ganador tiene que ser el local o el visitante.");
            if (model.StatJ.Any(s => s.EquipoId == null))
                return Invalido("Cada estadística de jugador necesita su equipo.");
            if (model.StatP.GroupBy(s => s.EquipoId).Any(g => g.Count() > 1)
                || model.StatJ.GroupBy(s => s.JugadorId).Any(g => g.Count() > 1))
                return Invalido("Hay estadísticas repetidas para el mismo equipo o jugador.");

            // Los equipos y jugadores del partido tienen que ser del usuario.
            var equipoIds = new[] { datos.EquipoLocal, datos.EquipoVisitante }
                .Concat(model.StatP.Select(s => s.EquipoId))
                .Concat(model.StatJ.Select(s => s.EquipoId!.Value))
                .Distinct().ToList();
            var jugadorIds = model.StatJ.Select(s => s.JugadorId).Distinct().ToList();
            var equiposPropios = await db.Equipos.CountAsync(e => equipoIds.Contains(e.Id) && e.UsuarioId == UsuarioActual);
            var jugadoresPropios = await db.Jugadores.CountAsync(j => jugadorIds.Contains(j.Id) && j.UsuarioId == UsuarioActual);
            if (equiposPropios != equipoIds.Count || jugadoresPropios != jugadorIds.Count)
                return Invalido("Los equipos y jugadores del partido tienen que ser tuyos.");

            // Partido y estadísticas se guardan juntos en un solo SaveChanges (una transacción).
            var partido = new Partido
            {
                UsuarioId = UsuarioActual,
                LocalId = datos.EquipoLocal,
                VisitanteId = datos.EquipoVisitante,
                GanadorId = datos.Ganador,
                Fecha = datos.Fecha,
                Lugar = datos.Lugar,
                Activo = true,
            };
            foreach (var stat in model.StatJ)
            {
                var estadistica = new EstadisticaJugador();
                estadistica.CopiarDe(stat);
                partido.EstadisticasJugador.Add(estadistica);
            }
            foreach (var stat in model.StatP)
            {
                var estadistica = new EstadisticaPartido();
                estadistica.CopiarDe(stat);
                partido.EstadisticasPartido.Add(estadistica);
            }
            db.Partidos.Add(partido);
            await db.SaveChangesAsync();
            return Exito("Se creó el partido correctamente.", partido.Id);
        }

        // userId se acepta por compatibilidad con el front, pero se ignora.
        [HttpDelete("Eliminar")]
        public async Task<IActionResult> EliminarPartido(int id, string? userId)
        {
            var partido = await MisPartidos.FirstOrDefaultAsync(p => p.Id == id);
            if (partido != null)
            {
                if (await PartidoService.TieneDependenciasAsync(db, id))
                    partido.Activo = false;
                else
                    db.Remove(partido);
                await db.SaveChangesAsync();
            }
            return Exito("Se eliminó el partido.");
        }

        [HttpGet("ListarPorId")]
        public async Task<IActionResult> ListarPorId(int idPartido)
        {
            var partido = await MisPartidos.AsNoTracking()
                .Include(p => p.Local)
                .Include(p => p.Visitante)
                .FirstOrDefaultAsync(p => p.Id == idPartido && p.Activo);
            if (partido == null) return NoEncontrado("No se encontró el partido.");

            var model = ParcialDe(partido);
            model.GanadorNombre = null;
            return Exito("Se obtuvo el partido", model);
        }

        // idUsuario se acepta por compatibilidad con el front, pero se ignora.
        [HttpGet("ListarTodos")]
        public async Task<IActionResult> ListarPartidos(string? idUsuario)
        {
            var partidos = await MisPartidos.AsNoTracking()
                .Include(p => p.Local)
                .Include(p => p.Visitante)
                .Where(p => p.Activo)
                .ToListAsync();
            return Exito("Se obtuvieron los partidos", partidos.Select(ParcialDe).ToList());
        }

        [HttpGet("ConStats")]
        public async Task<IActionResult> PartidoConStats(int id)
        {
            var partido = await MisPartidos.AsNoTracking()
                .Include(p => p.EstadisticasJugador).ThenInclude(e => e.Jugador)
                .Include(p => p.EstadisticasPartido)
                .Include(p => p.Local)
                .Include(p => p.Visitante)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NoEncontrado("No se encontró el partido.");

            var parcial = ParcialDe(partido);
            var response = new PartidoResponse
            {
                id = partido.Id,
                Local = parcial.Local,
                Visitante = parcial.Visitante,
                EquipoLocalId = partido.LocalId,
                EquipoVisitanteId = partido.VisitanteId,
                Ganador = partido.GanadorId,
                GanadorNombre = parcial.GanadorNombre,
                Lugar = partido.Lugar,
                Fecha = partido.Fecha,
                Jugadores = partido.EstadisticasJugador.Select(e => new JugadorResponse
                {
                    Id = e.JugadorId,
                    Nombre = e.Jugador.Nombre,
                    Numero = e.Jugador.Numero,
                    estadisticasJugador = [e.ToResponse()],
                }).ToList(),
                StatPartido = partido.EstadisticasPartido.Select(e => e.ToResponse()).ToList(),
            };
            return Exito("Se obtuvo el partido", response);
        }

        private static PartidoParcial ParcialDe(Partido p) => new()
        {
            id = p.Id,
            Local = p.Local.Nombre,
            EquipoLocalId = p.LocalId,
            Visitante = p.Visitante.Nombre,
            EquipoVisitanteId = p.VisitanteId,
            Ganador = p.GanadorId,
            // El ganador siempre es el local o el visitante.
            GanadorNombre = p.GanadorId == p.LocalId ? p.Local.Nombre
                : p.GanadorId == p.VisitanteId ? p.Visitante.Nombre
                : null,
            Lugar = p.Lugar,
            Fecha = p.Fecha,
            Estado = p.Estado,
        };
    }
}

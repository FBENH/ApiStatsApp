using apiBask.Models;
using apiBask.Models.Parcial;
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
        private IQueryable<Partido> MisPartidos => db.Partidos.Where(p => p.IdUsuario == UsuarioActual);

        [HttpPost("Crear")]
        public async Task<IActionResult> CrearPartido(CrearPartidoRequest model)
        {
            // Los equipos y jugadores del partido tienen que ser del usuario.
            var equipoIds = new[] { model.Partido.EquipoLocal, model.Partido.EquipoVisitante }
                .Concat(model.StatP.Select(s => s.EquipoId))
                .Concat(model.StatJ.Where(s => s.EquipoId != null).Select(s => s.EquipoId!.Value))
                .Distinct().ToList();
            var jugadorIds = model.StatJ.Select(s => s.JugadorId).Distinct().ToList();
            var equiposPropios = await db.Equipos.CountAsync(e => equipoIds.Contains(e.Id) && e.IdUsuario == UsuarioActual);
            var jugadoresPropios = await db.Jugadors.CountAsync(j => jugadorIds.Contains(j.Id) && j.IdUsuario == UsuarioActual);
            if (equiposPropios != equipoIds.Count || jugadoresPropios != jugadorIds.Count)
                return Invalido("Los equipos y jugadores del partido tienen que ser tuyos.");

            // Partido y estadísticas se guardan juntos en un solo SaveChanges (una transacción).
            var partido = new Partido
            {
                IdUsuario = UsuarioActual,
                EquipoLocal = model.Partido.EquipoLocal,
                EquipoVisitante = model.Partido.EquipoVisitante,
                Ganador = model.Partido.Ganador,
                Fecha = model.Partido.Fecha,
                Lugar = model.Partido.Lugar,
                Activo = true,
            };
            foreach (var stat in model.StatJ)
            {
                var estadistica = new EstadisticaJugador();
                estadistica.CopiarDe(stat);
                partido.EstadisticaJugadors.Add(estadistica);
            }
            foreach (var stat in model.StatP)
            {
                var estadistica = new EstadisticaPartido();
                estadistica.CopiarDe(stat);
                partido.EstadisticaPartidos.Add(estadistica);
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
                .Include(p => p.EquipoLocalNavigation)
                .Include(p => p.EquipoVisitanteNavigation)
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
                .Include(p => p.EquipoLocalNavigation)
                .Include(p => p.EquipoVisitanteNavigation)
                .Where(p => p.Activo)
                .ToListAsync();
            return Exito("Se obtuvieron los partidos", partidos.Select(ParcialDe).ToList());
        }

        [HttpGet("ConStats")]
        public async Task<IActionResult> PartidoConStats(int id)
        {
            var partido = await MisPartidos.AsNoTracking()
                .Include(p => p.EstadisticaJugadors).ThenInclude(e => e.Jugador)
                .Include(p => p.EstadisticaPartidos)
                .Include(p => p.EquipoLocalNavigation)
                .Include(p => p.EquipoVisitanteNavigation)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NoEncontrado("No se encontró el partido.");

            var parcial = ParcialDe(partido);
            var response = new PartidoResponse
            {
                id = partido.Id,
                Local = parcial.Local,
                Visitante = parcial.Visitante,
                EquipoLocalId = partido.EquipoLocal,
                EquipoVisitanteId = partido.EquipoVisitante,
                Ganador = partido.Ganador,
                GanadorNombre = parcial.GanadorNombre,
                Lugar = partido.Lugar,
                Jugadores = partido.EstadisticaJugadors.Select(e => new JugadorResponse
                {
                    Id = e.JugadorId,
                    Nombre = e.Jugador.Nombre,
                    Numero = e.Jugador.Numero,
                    estadisticasJugador = [e.ToResponse()],
                }).ToList(),
                StatPartido = partido.EstadisticaPartidos.Select(e => e.ToResponse()).ToList(),
            };
            return Exito("Se obtuvo el partido", response);
        }

        // El ganador siempre es el local o el visitante, así que su nombre sale de esos dos equipos.
        private static PartidoParcial ParcialDe(Partido p) => new()
        {
            id = p.Id,
            Local = p.EquipoLocalNavigation?.Nombre,
            EquipoLocalId = p.EquipoLocal,
            Visitante = p.EquipoVisitanteNavigation?.Nombre,
            EquipoVisitanteId = p.EquipoVisitante,
            Ganador = p.Ganador,
            GanadorNombre = p.Ganador == null ? null
                : p.Ganador == p.EquipoLocal ? p.EquipoLocalNavigation?.Nombre
                : p.Ganador == p.EquipoVisitante ? p.EquipoVisitanteNavigation?.Nombre
                : null,
            Lugar = p.Lugar,
            Fecha = p.Fecha,
        };
    }
}

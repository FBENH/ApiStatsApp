using apiBask.Models;
using apiBask.Models.Request;
using apiBask.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Controllers
{
    // Partidos que se anotan jugada a jugada. Cada jugada se guarda apenas se carga,
    // así el partido se puede retomar desde otro dispositivo si se corta la conexión.
    [Route("/Partido")]
    public class PartidoEnVivoController(BasketContext db) : ApiControllerBase
    {
        private const int MaxJugadasPorEnvio = 500;

        private IQueryable<Partido> MisPartidos =>
            db.Partidos.Where(p => p.UsuarioId == UsuarioActual && p.Activo);

        [HttpPost("Iniciar")]
        public async Task<IActionResult> Iniciar(IniciarPartidoRequest model)
        {
            if (model.EquipoLocal == model.EquipoVisitante)
                return Invalido("El local y el visitante tienen que ser equipos distintos.");
            if (model.DuracionCuartoMin is < 1 or > 20)
                return Invalido("Los cuartos tienen que durar entre 1 y 20 minutos.");
            var propios = await db.Equipos.CountAsync(e =>
                (e.Id == model.EquipoLocal || e.Id == model.EquipoVisitante) && e.UsuarioId == UsuarioActual);
            if (propios != 2) return Invalido("Los equipos del partido tienen que ser tuyos.");

            var partido = new Partido
            {
                UsuarioId = UsuarioActual,
                LocalId = model.EquipoLocal,
                VisitanteId = model.EquipoVisitante,
                Fecha = model.Fecha,
                Lugar = model.Lugar,
                DuracionCuartoMin = model.DuracionCuartoMin,
                Estado = EstadoPartido.EnCurso,
                Activo = true,
            };
            db.Partidos.Add(partido);
            await db.SaveChangesAsync();
            return Exito("Se inició el partido.", partido.Id);
        }

        [HttpGet("EnCurso")]
        public async Task<IActionResult> EnCurso()
        {
            var partidos = await MisPartidos.AsNoTracking()
                .Where(p => p.Estado == EstadoPartido.EnCurso)
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    id = p.Id,
                    local = p.Local.Nombre,
                    visitante = p.Visitante.Nombre,
                    equipoLocalId = p.LocalId,
                    equipoVisitanteId = p.VisitanteId,
                    fecha = p.Fecha,
                    lugar = p.Lugar,
                    duracionCuartoMin = p.DuracionCuartoMin,
                    jugadas = p.Jugadas.Count,
                })
                .ToListAsync();
            return Exito("Se obtuvieron los partidos en curso.", partidos);
        }

        [HttpGet("Jugadas")]
        public async Task<IActionResult> Jugadas(int partidoId)
        {
            if (!await MisPartidos.AnyAsync(p => p.Id == partidoId)) return NoEncontrado("No se encontró el partido.");
            var jugadas = await db.Jugadas.AsNoTracking()
                .Where(j => j.PartidoId == partidoId)
                .OrderBy(j => j.Numero)
                .Select(j => new JugadaDto(j.Id, j.Numero, j.Cuarto, j.RelojMs, j.Tipo, j.EquipoId, j.JugadorId,
                    j.SaleId, j.Acierto, j.Valor, j.Ofensivo, j.X, j.Y, j.RelacionadaId))
                .ToListAsync();
            return Exito("Se obtuvieron las jugadas.", jugadas);
        }

        // Guarda un lote de jugadas. Las que ya estaban (mismo id) se ignoran, así un reintento no duplica nada.
        [HttpPost("Jugadas")]
        public async Task<IActionResult> AgregarJugadas(int partidoId, List<JugadaDto> jugadas)
        {
            var partido = await MisPartidos.FirstOrDefaultAsync(p => p.Id == partidoId);
            if (partido == null) return NoEncontrado("No se encontró el partido.");
            if (jugadas.Count > MaxJugadasPorEnvio) return Invalido($"Se pueden mandar hasta {MaxJugadasPorEnvio} jugadas por vez.");
            if (jugadas.Select(j => j.Id).Distinct().Count() != jugadas.Count) return Invalido("Hay jugadas repetidas en el envío.");

            var error = Validar(partido, jugadas);
            if (error != null) return Invalido(error);
            var jugadorIds = jugadas.SelectMany(j => new[] { j.JugadorId, j.SaleId }).OfType<int>().Distinct().ToList();
            var jugadoresPropios = await db.Jugadores.CountAsync(j => jugadorIds.Contains(j.Id) && j.UsuarioId == UsuarioActual);
            if (jugadoresPropios != jugadorIds.Count) return Invalido("Los jugadores tienen que ser tuyos.");

            var ids = jugadas.Select(j => j.Id).ToList();
            var existentes = await db.Jugadas.Where(j => ids.Contains(j.Id)).Select(j => j.Id).ToListAsync();
            var ahora = DateTime.UtcNow;
            var nuevas = jugadas.Where(j => !existentes.Contains(j.Id)).Select(j => new Jugada
            {
                Id = j.Id,
                PartidoId = partidoId,
                Numero = j.Numero,
                Cuarto = j.Cuarto,
                RelojMs = j.RelojMs,
                Tipo = j.Tipo,
                EquipoId = j.EquipoId,
                JugadorId = j.JugadorId,
                SaleId = j.SaleId,
                Acierto = j.Acierto,
                Valor = j.Valor,
                Ofensivo = j.Ofensivo,
                X = j.X,
                Y = j.Y,
                RelacionadaId = j.RelacionadaId,
                CreadaEn = ahora,
            }).ToList();
            db.Jugadas.AddRange(nuevas);
            await db.SaveChangesAsync();
            if (partido.Estado == EstadoPartido.Finalizado) await RecalcularAsync(partido, null);
            return Exito("Se guardaron las jugadas.", nuevas.Count);
        }

        // Deshacer: borra una jugada. Si ya no existe, no es un error (puede ser un reintento).
        [HttpDelete("Jugadas")]
        public async Task<IActionResult> BorrarJugada(int partidoId, Guid id)
        {
            var partido = await MisPartidos.FirstOrDefaultAsync(p => p.Id == partidoId);
            if (partido == null) return NoEncontrado("No se encontró el partido.");
            var jugada = await db.Jugadas.FirstOrDefaultAsync(j => j.Id == id && j.PartidoId == partidoId);
            if (jugada != null)
            {
                db.Jugadas.Remove(jugada);
                await db.SaveChangesAsync();
                if (partido.Estado == EstadoPartido.Finalizado) await RecalcularAsync(partido, null);
            }
            return Exito("Se borró la jugada.");
        }

        // Calcula la planilla a partir de las jugadas y marca el partido como terminado.
        // Se puede llamar de nuevo después de corregir jugadas.
        [HttpPost("Finalizar")]
        public async Task<IActionResult> Finalizar(int id, FinalizarRequest? fin)
        {
            var partido = await MisPartidos.FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NoEncontrado("No se encontró el partido.");
            if (!await db.Jugadas.AnyAsync(j => j.PartidoId == id))
                return Invalido("El partido no tiene jugadas.");
            if (fin is { RelojMs: < 0 } or { Cuarto: < 1 })
                return Invalido("El cuarto o el reloj final no son válidos.");

            partido.Estado = EstadoPartido.Finalizado;
            await RecalcularAsync(partido, fin is { Cuarto: int c } ? (c, fin.RelojMs ?? 0) : null);
            return Exito("Se terminó el partido.", partido.Id);
        }

        private async Task RecalcularAsync(Partido partido, (int, int)? fin)
        {
            var jugadas = await db.Jugadas.AsNoTracking().Where(j => j.PartidoId == partido.Id).ToListAsync();
            var planilla = Planilla.Calcular(partido, jugadas, fin);

            // Las filas viejas se reemplazan dentro de la misma transacción.
            await using var transaccion = await db.Database.BeginTransactionAsync();
            await db.EstadisticasJugador.Where(e => e.PartidoId == partido.Id).ExecuteDeleteAsync();
            await db.EstadisticasPartido.Where(e => e.PartidoId == partido.Id).ExecuteDeleteAsync();
            db.EstadisticasJugador.AddRange(planilla.Jugadores);
            db.EstadisticasPartido.AddRange(planilla.Equipos);
            var local = planilla.Equipos.First(e => e.EquipoId == partido.LocalId).Puntos;
            var visitante = planilla.Equipos.First(e => e.EquipoId == partido.VisitanteId).Puntos;
            partido.GanadorId = local > visitante ? partido.LocalId : visitante > local ? partido.VisitanteId : null;
            await db.SaveChangesAsync();
            await transaccion.CommitAsync();
        }

        private static string? Validar(Partido partido, List<JugadaDto> jugadas)
        {
            foreach (var j in jugadas)
            {
                if (j.EquipoId != partido.LocalId && j.EquipoId != partido.VisitanteId)
                    return "El equipo de la jugada no juega este partido.";
                if (j.Cuarto < 1 || j.Cuarto > 10 || j.RelojMs < 0 || j.Numero < 0)
                    return "El cuarto, el reloj o el número de la jugada no son válidos.";
                if (j.X is < 0 or > 1 || j.Y is < 0 or > 1)
                    return "La ubicación del tiro tiene que estar entre 0 y 1.";
                var valida = j.Tipo switch
                {
                    TipoJugada.Tiro => j.Acierto != null && j.Valor is 2 or 3,
                    TipoJugada.Libre => j.Acierto != null,
                    TipoJugada.Cambio => j.JugadorId != null || j.SaleId != null,
                    _ => true,
                };
                if (!valida) return $"Faltan datos en una jugada de tipo {j.Tipo}.";
            }
            return null;
        }
    }
}

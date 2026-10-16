using apiBask.Models;
using apiBask.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Services
{
    // Guarda y borra jugadas, y termina partidos. Lo usan el controlador de partidos en vivo y el simulador.
    // Después de cada cambio avisa por SignalR (solo en partidos de liga).
    public class RegistroDePartido(BasketContext db, AvisosEnVivo avisos)
    {
        public const int MaxJugadasPorEnvio = 500;

        // Devuelve un mensaje si el envío no es válido. Los jugadores tienen que ser del dueño del partido.
        public async Task<string?> ValidarAsync(Partido partido, List<JugadaDto> jugadas)
        {
            if (partido.Estado == EstadoPartido.Programado) return "El partido todavía no se empezó.";
            if (jugadas.Count > MaxJugadasPorEnvio) return $"Se pueden mandar hasta {MaxJugadasPorEnvio} jugadas por vez.";
            if (jugadas.Select(j => j.Id).Distinct().Count() != jugadas.Count) return "Hay jugadas repetidas en el envío.";
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
            var jugadorIds = jugadas.SelectMany(j => new[] { j.JugadorId, j.SaleId }).OfType<int>().Distinct().ToList();
            var propios = await db.Jugadores.CountAsync(j => jugadorIds.Contains(j.Id) && j.UsuarioId == partido.UsuarioId);
            return propios == jugadorIds.Count ? null : "Los jugadores tienen que ser tuyos.";
        }

        // Las jugadas que ya estaban (mismo id) se ignoran, así un reintento no duplica nada. Devuelve cuántas se guardaron.
        public async Task<int> AgregarAsync(Partido partido, List<JugadaDto> jugadas)
        {
            var ids = jugadas.Select(j => j.Id).ToList();
            var existentes = await db.Jugadas.Where(j => ids.Contains(j.Id)).Select(j => j.Id).ToListAsync();
            var ahora = DateTime.UtcNow;
            var nuevas = jugadas.Where(j => !existentes.Contains(j.Id)).Select(j => new Jugada
            {
                Id = j.Id,
                PartidoId = partido.Id,
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
            await avisos.JugadasAsync(partido, nuevas);
            return nuevas.Count;
        }

        // Si ya no existe, no es un error (puede ser un reintento).
        public async Task BorrarAsync(Partido partido, Guid id)
        {
            var jugada = await db.Jugadas.FirstOrDefaultAsync(j => j.Id == id && j.PartidoId == partido.Id);
            if (jugada == null) return;
            db.Jugadas.Remove(jugada);
            await db.SaveChangesAsync();
            if (partido.Estado == EstadoPartido.Finalizado) await RecalcularAsync(partido, null);
            await avisos.JugadaBorradaAsync(partido, id);
        }

        // Calcula la planilla a partir de las jugadas y marca el partido como terminado.
        public async Task FinalizarAsync(Partido partido, (int Cuarto, int RelojMs)? fin)
        {
            partido.Estado = EstadoPartido.Finalizado;
            await RecalcularAsync(partido, fin);
            await avisos.PartidoAsync(partido);
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
    }
}

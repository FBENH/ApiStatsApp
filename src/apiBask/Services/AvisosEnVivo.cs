using apiBask.Controllers;
using apiBask.Hubs;
using apiBask.Models;
using apiBask.Models.Request;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Services
{
    // Manda por SignalR lo que pasa en los partidos de una liga. Los partidos sueltos no son públicos y no se avisan.
    // Mensajes:
    //   "jugadas"       (partidoId, JugadaDto[])  al grupo del partido
    //   "jugadaBorrada" (partidoId, id)           al grupo del partido
    //   "partido"       (PartidoDeLiga)           al grupo del partido y al de la liga: marcador y estado
    public class AvisosEnVivo(BasketContext db, IHubContext<PartidosHub> hub, ILogger<AvisosEnVivo> log)
    {
        public async Task JugadasAsync(Partido partido, IReadOnlyCollection<Jugada> jugadas)
        {
            if (partido.TemporadaId == null || jugadas.Count == 0) return;
            await Enviar(() => hub.Clients.Group(PartidosHub.GrupoPartido(partido.Id))
                .SendAsync("jugadas", partido.Id, jugadas.Select(Dto).ToList()));
            await PartidoAsync(partido);
        }

        public async Task JugadaBorradaAsync(Partido partido, Guid id)
        {
            if (partido.TemporadaId == null) return;
            await Enviar(() => hub.Clients.Group(PartidosHub.GrupoPartido(partido.Id)).SendAsync("jugadaBorrada", partido.Id, id));
            await PartidoAsync(partido);
        }

        public async Task PartidoAsync(Partido partido)
        {
            if (partido.TemporadaId is not int temporadaId) return;
            var datos = (await PublicoController.PartidosDeAsync(db, temporadaId, partido.Id)).FirstOrDefault();
            var slug = await db.Temporadas.Where(t => t.Id == temporadaId).Select(t => t.Liga.Slug).FirstAsync();
            if (datos == null) return;
            await Enviar(() => hub.Clients.Groups(PartidosHub.GrupoLiga(slug), PartidosHub.GrupoPartido(partido.Id))
                .SendAsync("partido", datos));
        }

        public static JugadaDto Dto(Jugada j) => new(j.Id, j.Numero, j.Cuarto, j.RelojMs, j.Tipo, j.EquipoId, j.JugadorId,
            j.SaleId, j.Acierto, j.Valor, j.Ofensivo, j.X, j.Y, j.RelacionadaId);

        // Un aviso que no sale no tiene que romper el guardado de la jugada: el cliente se pone al día al recargar.
        private async Task Enviar(Func<Task> envio)
        {
            try
            {
                await envio();
            }
            catch (Exception e)
            {
                log.LogWarning(e, "No se pudo mandar un aviso en vivo.");
            }
        }
    }
}

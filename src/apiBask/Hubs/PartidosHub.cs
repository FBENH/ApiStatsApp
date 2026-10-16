using apiBask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Hubs
{
    // Avisos en vivo para la página pública de una liga. No pide login, igual que /publico.
    // El cliente se suscribe a una liga (cambios de marcador de todos sus partidos)
    // o a un partido (cada jugada que se anota o se borra).
    [AllowAnonymous]
    public class PartidosHub(BasketContext db, LigaDemo demo) : Hub
    {
        public static string GrupoLiga(string slug) => "liga:" + slug;
        public static string GrupoPartido(int id) => "partido:" + id;

        public async Task SeguirLiga(string slug)
        {
            if (!await db.Ligas.AnyAsync(l => l.Slug == slug)) throw new HubException("No se encontró la liga.");
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoLiga(slug));
            if (demo.EsLaDemo(slug)) demo.Entra(Context.ConnectionId);
        }

        // Solo se pueden seguir partidos de una liga, que son los públicos.
        public async Task SeguirPartido(int id)
        {
            var slug = await db.Partidos.Where(p => p.Id == id && p.TemporadaId != null && p.Activo)
                .Select(p => p.Temporada!.Liga.Slug).FirstOrDefaultAsync();
            if (slug == null) throw new HubException("No se encontró el partido.");
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoPartido(id));
            if (demo.EsLaDemo(slug)) demo.Entra(Context.ConnectionId);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            demo.Sale(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public Task DejarPartido(int id) => Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoPartido(id));
    }
}

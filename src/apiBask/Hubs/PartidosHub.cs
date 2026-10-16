using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace apiBask.Hubs
{
    // Avisos en vivo para la página pública de una liga. No pide login, igual que /publico.
    // El cliente se suscribe a una liga (cambios de marcador de todos sus partidos)
    // o a un partido (cada jugada que se anota o se borra).
    [AllowAnonymous]
    public class PartidosHub(BasketContext db) : Hub
    {
        public static string GrupoLiga(string slug) => "liga:" + slug;
        public static string GrupoPartido(int id) => "partido:" + id;

        public async Task SeguirLiga(string slug)
        {
            if (!await db.Ligas.AnyAsync(l => l.Slug == slug)) throw new HubException("No se encontró la liga.");
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoLiga(slug));
        }

        // Solo se pueden seguir partidos de una liga, que son los públicos.
        public async Task SeguirPartido(int id)
        {
            if (!await db.Partidos.AnyAsync(p => p.Id == id && p.TemporadaId != null && p.Activo))
                throw new HubException("No se encontró el partido.");
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoPartido(id));
        }

        public Task DejarPartido(int id) => Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoPartido(id));
    }
}

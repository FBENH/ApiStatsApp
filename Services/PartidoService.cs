

namespace apiBask.Services
{
    public class PartidoService
    {
        public PartidoService() { }
        public bool TieneDependencias(BasketContext db, int id) 
        {
            return db.EstadisticaPartidos.Any(d=> d.PartidoId == id) ||
                   db.EstadisticaJugadors.Any(d=> d.PartidoId == id);
        }
    }
}

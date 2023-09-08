

namespace apiBask.Services
{
    public class JugadorService
    {
        public JugadorService() { }
        public bool TieneDependencias(BasketContext db, int jugadorId)
        {
            return db.EstadisticaJugadors.Any(d => d.JugadorId == jugadorId);
        }
    }
}

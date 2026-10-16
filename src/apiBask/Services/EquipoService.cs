

namespace apiBask.Services
{
    public class EquipoService
    {
        public EquipoService() { }
        public bool TieneDependencias(BasketContext db, int equipoId)
        {
            return db.Jugadors.Any(d => d.EquipoId == equipoId) ||
                   db.Partidos.Any(d => d.EquipoLocal == equipoId || d.EquipoVisitante == equipoId) ||
                   db.EstadisticaPartidos.Any(d => d.EquipoId == equipoId);
        }
    }
}

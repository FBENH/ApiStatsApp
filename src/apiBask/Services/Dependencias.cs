using Microsoft.EntityFrameworkCore;

namespace apiBask.Services
{
    // Si algo ya tiene estadísticas o partidos, se da de baja (Activo = false) en lugar de borrarse.
    public static class EquipoService
    {
        public static async Task<bool> TieneDependenciasAsync(BasketContext db, int equipoId) =>
            await db.Jugadors.AnyAsync(d => d.EquipoId == equipoId) ||
            await db.Partidos.AnyAsync(d => d.EquipoLocal == equipoId || d.EquipoVisitante == equipoId) ||
            await db.EstadisticaPartidos.AnyAsync(d => d.EquipoId == equipoId);
    }

    public static class JugadorService
    {
        public static Task<bool> TieneDependenciasAsync(BasketContext db, int jugadorId) =>
            db.EstadisticaJugadors.AnyAsync(d => d.JugadorId == jugadorId);
    }

    public static class PartidoService
    {
        public static async Task<bool> TieneDependenciasAsync(BasketContext db, int id) =>
            await db.EstadisticaPartidos.AnyAsync(d => d.PartidoId == id) ||
            await db.EstadisticaJugadors.AnyAsync(d => d.PartidoId == id);
    }
}

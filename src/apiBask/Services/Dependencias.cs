using Microsoft.EntityFrameworkCore;

namespace apiBask.Services
{
    // Si algo ya tiene estadísticas o partidos, se da de baja (Activo = false) en lugar de borrarse.
    public static class EquipoService
    {
        public static async Task<bool> TieneDependenciasAsync(BasketContext db, int equipoId) =>
            await db.Jugadores.AnyAsync(d => d.EquipoId == equipoId) ||
            await db.Partidos.AnyAsync(d => d.LocalId == equipoId || d.VisitanteId == equipoId) ||
            await db.EstadisticasPartido.AnyAsync(d => d.EquipoId == equipoId) ||
            await db.Inscripciones.AnyAsync(d => d.EquipoId == equipoId);
    }

    public static class JugadorService
    {
        public static async Task<bool> TieneDependenciasAsync(BasketContext db, int jugadorId) =>
            await db.EstadisticasJugador.AnyAsync(d => d.JugadorId == jugadorId) ||
            await db.Jugadas.AnyAsync(d => d.JugadorId == jugadorId || d.SaleId == jugadorId);
    }

    public static class PartidoService
    {
        public static async Task<bool> TieneDependenciasAsync(BasketContext db, int id) =>
            await db.EstadisticasPartido.AnyAsync(d => d.PartidoId == id) ||
            await db.EstadisticasJugador.AnyAsync(d => d.PartidoId == id) ||
            await db.Jugadas.AnyAsync(d => d.PartidoId == id);
    }
}

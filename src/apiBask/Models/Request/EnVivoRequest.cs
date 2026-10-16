using apiBask.Models;

namespace apiBask.Models.Request
{
    public class IniciarPartidoRequest
    {
        public int EquipoLocal { get; set; }
        public int EquipoVisitante { get; set; }
        public DateTime? Fecha { get; set; }
        public string? Lugar { get; set; }
        public int DuracionCuartoMin { get; set; } = 10;
        // Si se manda, se empieza a anotar ese partido del fixture de una liga en lugar de crear uno nuevo.
        public int? PartidoId { get; set; }
    }

    // Se usa para recibir y para devolver jugadas.
    public record JugadaDto(
        Guid Id,
        int Numero,
        int Cuarto,
        int? RelojMs,
        TipoJugada Tipo,
        int EquipoId,
        int? JugadorId = null,
        int? SaleId = null,
        bool? Acierto = null,
        int? Valor = null,
        bool? Ofensivo = null,
        double? X = null,
        double? Y = null,
        Guid? RelacionadaId = null);

    public record FinalizarRequest(int? Cuarto, int? RelojMs);
}

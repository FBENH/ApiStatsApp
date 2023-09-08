namespace apiBask.Models.Response
{
    public class JugadorResponse
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = null!;

        public string? Posicion { get; set; }

        public int? EquipoId { get; set; }

        public string? Foto { get; set; }

        public decimal? Altura { get; set; }

        public decimal? Peso { get; set; }
        

        public EquipoResponse? Equipo { get; set; }

        public List<EstadisticaJResponse>? estadisticasJugador { get; set; } = new List<EstadisticaJResponse>();

        public int Numero { get; set; }

        public JugadorResponse() { }
    }
}

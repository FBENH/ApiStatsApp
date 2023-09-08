namespace apiBask.Models.Response
{
    public class EquipoResponse
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = null!;

        public string? Ciudad { get; set; }

        public string? Escudo { get; set; }

        public string? Entrenador { get; set; }

        public List<JugadorResponse>? Jugadores { get; set; }

        public List<EstadisticaPResponse>? EstadisticasPartidos { get; set; }
    }
}

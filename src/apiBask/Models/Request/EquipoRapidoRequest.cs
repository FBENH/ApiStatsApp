namespace apiBask.Models.Request
{
    public class EquipoRapidoRequest
    {
        // Ignorado: el usuario sale del token. Se acepta para no romper al front.
        public string? IdUsuario { get; set; }

        public string Nombre { get; set; } = null!;

        public List<JugadorRequest>? Jugadores { get; set; }
    }
}

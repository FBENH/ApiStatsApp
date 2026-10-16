namespace apiBask.Models.Request
{
    public class EquipoRapidoRequest
    {
        public string IdUsuario { get; set; }

        public string Nombre { get; set; }

        public List<JugadorRequest>? Jugadores { get; set; }
    }
}

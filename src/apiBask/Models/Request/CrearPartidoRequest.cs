namespace apiBask.Models.Request
{
    public class CrearPartidoRequest
    {
        public PartidoRequest Partido { get; set; }
        public List<StatJugadorRequest> StatJ { get; set; }
        public List<StatPartidoRequest> StatP { get; set; }
    }
}

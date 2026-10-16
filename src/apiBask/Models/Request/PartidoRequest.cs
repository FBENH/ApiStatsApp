namespace apiBask.Models.Request
{
    public class PartidoRequest
    {
        public int EquipoLocal { get; set; }

        public int EquipoVisitante { get; set; }

        public int? Ganador { get; set; }

        public DateTime? Fecha { get; set; }

        public string? Lugar { get; set; }

        // Ignorado: el usuario sale del token. Se acepta para no romper al front.
        public string? IdUsuario { get; set; }
    }
}

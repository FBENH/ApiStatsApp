namespace apiBask.Models.Request
{
    public class PartidoRequest
    {
        public int EquipoLocal { get; set; }

        public int EquipoVisitante { get; set; }

        public int? Ganador { get; set; }

        public DateTime? Fecha { get; set; }

        public string? Lugar { get; set; }

        public string IdUsuario { get; set; } = null!;
    }
}

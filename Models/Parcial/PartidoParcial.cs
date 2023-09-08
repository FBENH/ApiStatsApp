namespace apiBask.Models.Parcial
{
    public class PartidoParcial
    {
        public DateTime? Fecha { get; set; }

        public int? id { get; set; }

        public string? Local { get; set; }
        public string? Visitante { get; set; }

        public int? EquipoLocalId { get; set; }

        public int? EquipoVisitanteId { get; set; }

        public int? Ganador { get; set; }

        public string? GanadorNombre { get; set; }

        public string? Lugar { get; set; }

        public PartidoParcial() { }
    }
}

namespace apiBask.Models.Request
{
    public class StatPartidoRequest
    {
        public int PartidoId { get; set; }

        public int EquipoId { get; set; }

        public int? Puntos { get; set; }

        public int? Asistencias { get; set; }

        public int? Rebotes { get; set; }

        public int? RebotesOfensivos { get; set; }

        public int? RebotesDefensivos { get; set; }

        public int? Robos { get; set; }

        public int? Tapones { get; set; }

        public int? Intentos2Puntos { get; set; }

        public int? Aciertos2Puntos { get; set; }

        public int? Intentos3Puntos { get; set; }

        public int? Aciertos3Puntos { get; set; }

        public int? Intentos1Punto { get; set; }

        public int? Aciertos1Punto { get; set; }

        public int? Faltas { get; set; }

        public int Perdidas { get; set; }


    }
}

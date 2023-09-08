namespace apiBask.Models.Request
{
    public class JugadorRequest
    {
        public int? id {  get; set; }
        public string Nombre { get; set; } = null!;

        public string? Posicion { get; set; }
        

        public int? EquipoId { get; set; }

        public string? Foto { get; set; }

        public decimal? Altura { get; set; }

        public decimal? Peso { get; set; }

        public string Usuario { get; set; } = null!;

        public int Numero { get; set; }
    }
}

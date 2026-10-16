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

        // Ignorado: el usuario sale del token. Se acepta para no romper al front.
        public string? Usuario { get; set; }

        public int Numero { get; set; }
    }
}

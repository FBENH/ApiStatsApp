namespace apiBask.Models.Request
{
    public class EquipoRequest
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;

        public string? Ciudad { get; set; }

        public string? Escudo { get; set; }

        public string? Entrenador { get; set; }

        // Ignorado: el usuario sale del token. Se acepta para no romper al front.
        public string? Usuario { get; set; }
    }
}

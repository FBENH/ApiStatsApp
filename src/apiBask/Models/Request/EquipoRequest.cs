namespace apiBask.Models.Request
{
    public class EquipoRequest
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;

        public string? Ciudad { get; set; }

        public string? Escudo { get; set; }

        public string? Entrenador { get; set; }

        public string Usuario { get; set; } = null!;
    }
}

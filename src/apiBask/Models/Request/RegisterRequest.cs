using System.ComponentModel.DataAnnotations;

namespace apiBask.Models.Request
{
    public class RegisterRequest
    {
        [Required, StringLength(16)]
        public string Usuario { get; set; } = null!;

        [Required]
        public string Pass { get; set; } = null!;
    }
}

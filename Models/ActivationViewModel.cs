using System.ComponentModel.DataAnnotations;

namespace Sistema_Ferreteria.Models
{
    public class ActivationViewModel
    {
        [Required]
        public string ActivationType { get; set; } = "Code"; // "Code" or "Login"

        // For Code Activation
        public string? Code { get; set; }

        // For Subscription Login
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}

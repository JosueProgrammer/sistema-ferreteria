using System.ComponentModel.DataAnnotations;

namespace Sistema_Ferreteria.Models.Seguridad;

public class LoginViewModel
{
    [Required(ErrorMessage = "La sucursal es obligatoria")]
    public string TenantId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El usuario es obligatorio")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool Recordarme { get; set; }
}

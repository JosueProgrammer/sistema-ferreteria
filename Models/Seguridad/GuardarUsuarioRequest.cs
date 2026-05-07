using System.ComponentModel.DataAnnotations;

namespace Sistema_Ferreteria.Models.Seguridad;

public class GuardarUsuarioRequest
{
    public int IdUsuario { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [MaxLength(255)]
    public string ContraseñaHash { get; set; } = string.Empty;

    public bool Estado { get; set; } = true;
}

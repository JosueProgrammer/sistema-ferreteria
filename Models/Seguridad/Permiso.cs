using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Ferreteria.Models.Seguridad;

[Table("Permisos")]
public class Permiso
{
    [Key]
    [Column("IdPermiso")]
    public int IdPermiso { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Modulo { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Descripcion { get; set; }

    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    // Navegación
    public virtual ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}


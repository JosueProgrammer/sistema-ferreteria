using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Common;

namespace Sistema_Ferreteria.Models.Seguridad;

[Table("RolPermiso")]
public class RolPermiso : ITenantEntity
{
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Key]
    [Column("IdRolPermiso")]
    public int IdRolPermiso { get; set; }

    [Column("IdRol")]
    public int IdRol { get; set; }

    [Column("IdPermiso")]
    public int IdPermiso { get; set; }

    // Navegación
    [ForeignKey("IdRol")]
    public virtual Rol Rol { get; set; } = null!;

    [ForeignKey("IdPermiso")]
    public virtual Permiso Permiso { get; set; } = null!;
}


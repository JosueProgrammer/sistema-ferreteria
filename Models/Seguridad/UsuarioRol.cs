using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Ferreteria.Models.Seguridad;

[Table("UsuarioRol")]
public class UsuarioRol
{
    [Key]
    [Column("IdUsuarioRol")]
    public int IdUsuarioRol { get; set; }

    [Column("IdUsuario")]
    public int IdUsuario { get; set; }

    [Column("IdRol")]
    public int IdRol { get; set; }

    [Column("FechaAsignacion")]
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;

    // Navegación
    [ForeignKey("IdUsuario")]
    public virtual Usuario Usuario { get; set; } = null!;

    [ForeignKey("IdRol")]
    public virtual Rol Rol { get; set; } = null!;
}


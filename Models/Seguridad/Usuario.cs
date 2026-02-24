using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Common;

namespace Sistema_Ferreteria.Models.Seguridad;

[Table("Usuarios")]
public class Usuario : ITenantEntity
{
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Key]
    [Column("IdUsuario")]
    public int IdUsuario { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("ContraseñaHash")]
    public string ContraseñaHash { get; set; } = string.Empty;

    public bool Estado { get; set; } = true;

    public bool Eliminado { get; set; } = false;

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    public virtual ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public virtual ICollection<Auditoria> Auditorias { get; set; } = new List<Auditoria>();
}


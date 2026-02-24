using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Common;

namespace Sistema_Ferreteria.Models.Seguridad;

[Table("Auditoria")]
public class Auditoria : ITenantEntity
{
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Key]
    [Column("IdAuditoria")]
    public long IdAuditoria { get; set; }

    [Column("IdUsuario")]
    public int? IdUsuario { get; set; }

    [Required]
    [MaxLength(50)]
    public string Modulo { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Accion { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Entidad { get; set; }

    [Column("IdEntidad")]
    public int? IdEntidad { get; set; }

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [MaxLength(50)]
    [Column("IpAddress")]
    public string? IpAddress { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // Navegación
    [ForeignKey("IdUsuario")]
    public virtual Usuario? Usuario { get; set; }
}


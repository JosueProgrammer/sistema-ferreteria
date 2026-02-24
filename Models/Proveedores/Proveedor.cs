using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Common;

namespace Sistema_Ferreteria.Models.Proveedores;

[Table("Proveedores")]
public class Proveedor : ITenantEntity
{
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Key]
    [Column("IdProveedor")]
    public int IdProveedor { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("TipoDocumento")]
    public string TipoDocumento { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("NumeroDocumento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("RazonSocial")]
    public string RazonSocial { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Direccion { get; set; }

    [MaxLength(100)]
    [Column("ContactoNombre")]
    public string? ContactoNombre { get; set; }

    [Column("PlazoPago")]
    public int PlazoPago { get; set; } = 0;

    public bool Estado { get; set; } = true;

    public bool Eliminado { get; set; } = false;

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public string? Observaciones { get; set; }
}


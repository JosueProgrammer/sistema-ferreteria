using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Seguridad;
using Sistema_Ferreteria.Models.Common;

namespace Sistema_Ferreteria.Models.Compras;

[Table("PagosCompra")]
public class PagoCompra : ITenantEntity
{
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Key]
    [Column("IdPagoCompra")]
    public long IdPagoCompra { get; set; }

    [Column("IdCompra")]
    public long IdCompra { get; set; }

    [Column("FechaPago")]
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("MetodoPago")]
    public string MetodoPago { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("NumeroComprobante")]
    public string? NumeroComprobante { get; set; }

    [Column("IdUsuario")]
    public int IdUsuario { get; set; }

    // Navegación
    [ForeignKey("IdCompra")]
    public virtual Compra Compra { get; set; } = null!;

    [ForeignKey("IdUsuario")]
    public virtual Usuario Usuario { get; set; } = null!;
}


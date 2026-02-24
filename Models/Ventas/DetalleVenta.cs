using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Inventario;
using Sistema_Ferreteria.Models.Common;

namespace Sistema_Ferreteria.Models.Ventas;

[Table("DetalleVenta")]
public class DetalleVenta : ITenantEntity
{
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Key]
    [Column("IdDetalleVenta")]
    public long IdDetalleVenta { get; set; }

    [Column("IdVenta")]
    public long IdVenta { get; set; }

    [Column("IdProducto")]
    public int IdProducto { get; set; }

    [Column("IdPresentacion")]
    public int IdPresentacion { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Cantidad { get; set; }

    [Column("CantidadBase", TypeName = "decimal(18,4)")]
    public decimal CantidadBase { get; set; }

    [Column("PrecioUnitario", TypeName = "decimal(18,2)")]
    public decimal PrecioUnitario { get; set; }

    [Column("DescuentoMonto", TypeName = "decimal(18,2)")]
    public decimal DescuentoMonto { get; set; } = 0;

    // Navegación
    [ForeignKey("IdVenta")]
    public virtual Venta Venta { get; set; } = null!;

    [ForeignKey("IdProducto")]
    public virtual Producto Producto { get; set; } = null!;

    [ForeignKey("IdPresentacion")]
    public virtual Presentacion Presentacion { get; set; } = null!;
}


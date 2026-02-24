using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Inventario;
using Sistema_Ferreteria.Models.Common;

namespace Sistema_Ferreteria.Models.Compras;

[Table("DetalleCompra")]
public class DetalleCompra : ITenantEntity
{
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Key]
    [Column("IdDetalleCompra")]
    public long IdDetalleCompra { get; set; }

    [Column("IdCompra")]
    public long IdCompra { get; set; }

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
    [ForeignKey("IdCompra")]
    public virtual Compra? Compra { get; set; }

    [ForeignKey("IdProducto")]
    public virtual Producto? Producto { get; set; }

    [ForeignKey("IdPresentacion")]
    public virtual Presentacion? Presentacion { get; set; }
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Seguridad;

namespace Sistema_Ferreteria.Models.Inventario;

[Table("Productos")]
public class Producto
{
    [Key]
    [Column("IdProducto")]
    public int IdProducto { get; set; }

    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("CodigoBarras")]
    public string? CodigoBarras { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [Column("IdCategoria")]
    public int IdCategoria { get; set; }

    [Column("IdUnidadBase")]
    public int IdUnidadBase { get; set; }

    [Column("StockBase", TypeName = "decimal(18,4)")]
    public decimal StockBase { get; set; } = 0;

    [Column("StockMinimo", TypeName = "decimal(18,4)")]
    public decimal StockMinimo { get; set; } = 0;

    [Column("PrecioBaseVenta", TypeName = "decimal(18,2)")]
    public decimal? PrecioBaseVenta { get; set; }

    [Column("PrecioBaseCompra", TypeName = "decimal(18,2)")]
    public decimal? PrecioBaseCompra { get; set; }

    public bool Estado { get; set; } = true;

    public bool Eliminado { get; set; } = false;

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    [ForeignKey("IdCategoria")]
    public virtual Categoria Categoria { get; set; } = null!;

    [ForeignKey("IdUnidadBase")]
    public virtual UnidadMedida UnidadBase { get; set; } = null!;

    public virtual ICollection<Presentacion> Presentaciones { get; set; } = new List<Presentacion>();
    public virtual ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
}


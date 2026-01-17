using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Ferreteria.Models.Inventario;

[Table("Presentaciones")]
public class Presentacion
{
    [Key]
    [Column("IdPresentacion")]
    public int IdPresentacion { get; set; }

    [Column("IdProducto")]
    public int IdProducto { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("NombrePresentacion")]
    public string NombrePresentacion { get; set; } = string.Empty;

    [Column("IdUnidadPresentacion")]
    public int IdUnidadPresentacion { get; set; }

    [Column("FactorConversion", TypeName = "decimal(18,6)")]
    public decimal FactorConversion { get; set; }

    [Column("PrecioVenta", TypeName = "decimal(18,2)")]
    public decimal PrecioVenta { get; set; }

    [Column("PrecioCompra", TypeName = "decimal(18,2)")]
    public decimal? PrecioCompra { get; set; }

    [MaxLength(50)]
    [Column("CodigoBarras")]
    public string? CodigoBarras { get; set; }

    [Column("EsPrincipal")]
    public bool EsPrincipal { get; set; } = false;

    public bool Estado { get; set; } = true;

    public bool Eliminado { get; set; } = false;

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    [ForeignKey("IdProducto")]
    public virtual Producto Producto { get; set; } = null!;

    [ForeignKey("IdUnidadPresentacion")]
    public virtual UnidadMedida UnidadPresentacion { get; set; } = null!;
}


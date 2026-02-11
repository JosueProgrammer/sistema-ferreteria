using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Sistema_Ferreteria.Models.Seguridad;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Sistema_Ferreteria.Models.Inventario;

[Table("Productos")]
public class Producto
{
    [Key]
    [Column("IdProducto")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El código es obligatorio")]
    [MaxLength(50)]
    [Display(Name = "Código SKU")]
    public string Codigo { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("CodigoBarras")]
    [Display(Name = "Código de Barras")]
    public string? CodigoBarras { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(200)]
    [Display(Name = "Nombre del Producto")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria")]
    [Column("IdCategoria")]
    [Display(Name = "Categoría")]
    public int IdCategoria { get; set; }

    [Required(ErrorMessage = "La unidad base es obligatoria")]
    [Column("IdUnidadBase")]
    [Display(Name = "Unidad Base")]
    public int IdUnidadBase { get; set; }

    [Column("StockBase", TypeName = "decimal(18,4)")]
    [Display(Name = "Stock Actual")]
    [Range(0, double.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
    public decimal StockBase { get; set; } = 0;

    [Column("StockMinimo", TypeName = "decimal(18,4)")]
    [Display(Name = "Stock Mínimo")]
    [Range(0, double.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
    public decimal StockMinimo { get; set; } = 0;

    [Column("PrecioBaseVenta", TypeName = "decimal(18,2)")]
    [Display(Name = "Precio Venta")]
    [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
    public decimal? PrecioBaseVenta { get; set; }

    [Column("PrecioBaseCompra", TypeName = "decimal(18,2)")]
    [Display(Name = "Precio Compra")]
    [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
    public decimal? PrecioBaseCompra { get; set; }

    public bool Estado { get; set; } = true;

    public bool Eliminado { get; set; } = false;

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    [ForeignKey("IdCategoria")]
    [ValidateNever]
    public virtual Categoria? Categoria { get; set; }

    [ForeignKey("IdUnidadBase")]
    [ValidateNever]
    public virtual UnidadMedida? UnidadBase { get; set; }

    [ValidateNever]
    public virtual ICollection<Presentacion> Presentaciones { get; set; } = new List<Presentacion>();
    
    [JsonIgnore]
    public virtual ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
}


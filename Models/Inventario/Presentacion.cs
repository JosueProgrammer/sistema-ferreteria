using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Sistema_Ferreteria.Models.Inventario;

[Table("Presentaciones")]
public class Presentacion
{
    [Key]
    [Column("IdPresentacion")]
    public int IdPresentacion { get; set; }

    [Column("IdProducto")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El nombre de la presentación es obligatorio")]
    [MaxLength(100)]
    [Column("NombrePresentacion")]
    [Display(Name = "Presentación")]
    public string NombrePresentacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La unidad es obligatoria")]
    [Column("IdUnidadPresentacion")]
    [Display(Name = "Unidad")]
    public int IdUnidadPresentacion { get; set; }

    [Column("FactorConversion", TypeName = "decimal(18,6)")]
    [Display(Name = "Factor de Conversión")]
    [Range(0.000001, double.MaxValue, ErrorMessage = "El factor debe ser mayor a 0")]
    public decimal FactorConversion { get; set; }

    [Column("PrecioVenta", TypeName = "decimal(18,2)")]
    [Display(Name = "Precio Venta")]
    [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
    public decimal PrecioVenta { get; set; }

    [Column("PrecioCompra", TypeName = "decimal(18,2)")]
    [Display(Name = "Precio Compra")]
    [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
    public decimal? PrecioCompra { get; set; }

    [MaxLength(50)]
    [Column("CodigoBarras")]
    [Display(Name = "Código de Barras")]
    public string? CodigoBarras { get; set; }

    [Column("EsPrincipal")]
    [Display(Name = "¿Es Principal?")]
    public bool EsPrincipal { get; set; } = false;

    public bool Estado { get; set; } = true;

    public bool Eliminado { get; set; } = false;

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    [ForeignKey("IdProducto")]
    [JsonIgnore]
    [ValidateNever]
    public virtual Producto? Producto { get; set; }

    [ForeignKey("IdUnidadPresentacion")]
    [ValidateNever]
    public virtual UnidadMedida? UnidadPresentacion { get; set; }
}


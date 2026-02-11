using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Seguridad;

namespace Sistema_Ferreteria.Models.Inventario;

[Table("MovimientosInventario")]
public class MovimientoInventario
{
    [Key]
    [Column("IdMovimiento")]
    public long IdMovimiento { get; set; }

    [Column("IdProducto")]
    public int IdProducto { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("TipoMovimiento")]
    public string TipoMovimiento { get; set; } = string.Empty; // Entrada, Salida, Ajuste, Merma, Corte

    [Column("CantidadBase", TypeName = "decimal(18,4)")]
    public decimal CantidadBase { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Observacion { get; set; }

    [Column("IdUsuario")]
    public int IdUsuario { get; set; }

    [Column("IdReferencia")]
    public int? IdReferencia { get; set; }

    [MaxLength(50)]
    [Column("TipoReferencia")]
    public string? TipoReferencia { get; set; }

    // Navegación
    [ForeignKey("IdProducto")]
    public virtual Producto? Producto { get; set; }

    [ForeignKey("IdUsuario")]
    public virtual Usuario? Usuario { get; set; }
}


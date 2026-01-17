using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Seguridad;

namespace Sistema_Ferreteria.Models.Ventas;

[Table("PagosVenta")]
public class PagoVenta
{
    [Key]
    [Column("IdPagoVenta")]
    public long IdPagoVenta { get; set; }

    [Column("IdVenta")]
    public long IdVenta { get; set; }

    [Column("FechaPago")]
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("MetodoPago")]
    public string MetodoPago { get; set; } = string.Empty; // Efectivo, Transferencia, Tarjeta, Cheque, Otro

    [MaxLength(50)]
    [Column("NumeroComprobante")]
    public string? NumeroComprobante { get; set; }

    [Column("IdUsuario")]
    public int IdUsuario { get; set; }

    // Navegación
    [ForeignKey("IdVenta")]
    public virtual Venta Venta { get; set; } = null!;

    [ForeignKey("IdUsuario")]
    public virtual Usuario Usuario { get; set; } = null!;
}


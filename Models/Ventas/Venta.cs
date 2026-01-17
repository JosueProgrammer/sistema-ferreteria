using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Seguridad;
using Sistema_Ferreteria.Models.Clientes;

namespace Sistema_Ferreteria.Models.Ventas;

[Table("Ventas")]
public class Venta
{
    [Key]
    [Column("IdVenta")]
    public long IdVenta { get; set; }

    [MaxLength(20)]
    [Column("NumeroFactura")]
    public string? NumeroFactura { get; set; }

    [Column("IdCliente")]
    public int? IdCliente { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Column("FechaVencimiento")]
    public DateTime? FechaVencimiento { get; set; }

    [Column("DescuentoMonto", TypeName = "decimal(18,2)")]
    public decimal DescuentoMonto { get; set; } = 0;

    [Column("ImpuestoMonto", TypeName = "decimal(18,2)")]
    public decimal ImpuestoMonto { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; } = 0;

    [Required]
    [MaxLength(20)]
    [Column("TipoPago")]
    public string TipoPago { get; set; } = string.Empty; // Contado, Credito, Mixto

    [MaxLength(20)]
    public string Estado { get; set; } = "Pendiente";

    [Column("IdUsuario")]
    public int IdUsuario { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public bool Eliminado { get; set; } = false;

    [Column("UsuarioAnulacion")]
    public int? UsuarioAnulacion { get; set; }

    [Column("FechaAnulacion")]
    public DateTime? FechaAnulacion { get; set; }

    [MaxLength(500)]
    [Column("MotivoAnulacion")]
    public string? MotivoAnulacion { get; set; }

    // Navegación
    [ForeignKey("IdCliente")]
    public virtual Cliente? Cliente { get; set; }

    [ForeignKey("IdUsuario")]
    public virtual Usuario Usuario { get; set; } = null!;

    [ForeignKey("UsuarioAnulacion")]
    public virtual Usuario? UsuarioAnulacionNavigation { get; set; }

    public virtual ICollection<DetalleVenta> DetalleVentas { get; set; } = new List<DetalleVenta>();
    public virtual ICollection<PagoVenta> PagosVenta { get; set; } = new List<PagoVenta>();
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Seguridad;
using Sistema_Ferreteria.Models.Proveedores;

namespace Sistema_Ferreteria.Models.Compras;

[Table("Compras")]
public class Compra
{
    [Key]
    [Column("IdCompra")]
    public long IdCompra { get; set; }

    [MaxLength(50)]
    [Column("NumeroFactura")]
    public string? NumeroFactura { get; set; }

    [Column("IdProveedor")]
    public int IdProveedor { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Column("FechaVencimiento")]
    public DateTime? FechaVencimiento { get; set; }

    [Column("DescuentoMonto", TypeName = "decimal(18,2)")]
    public decimal DescuentoMonto { get; set; } = 0;

    [Column("ImpuestoMonto", TypeName = "decimal(18,2)")]
    public decimal ImpuestoMonto { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; } = 0;

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
    [ForeignKey("IdProveedor")]
    public virtual Proveedor? Proveedor { get; set; }

    [ForeignKey("IdUsuario")]
    public virtual Usuario? Usuario { get; set; }

    [ForeignKey("UsuarioAnulacion")]
    public virtual Usuario? UsuarioAnulacionNavigation { get; set; }

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();
    public virtual ICollection<PagoCompra> PagosCompra { get; set; } = new List<PagoCompra>();
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Common;

namespace Sistema_Ferreteria.Models.Clientes;

[Table("Clientes")]
public class Cliente : ITenantEntity
{
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Key]
    [Column("IdCliente")]
    public int IdCliente { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("TipoDocumento")]
    public string TipoDocumento { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("NumeroDocumento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(500)]
    public string? Direccion { get; set; }

    [Column("LimiteCredito", TypeName = "decimal(18,2)")]
    public decimal LimiteCredito { get; set; } = 0;

    [Column("SaldoActual", TypeName = "decimal(18,2)")]
    public decimal SaldoActual { get; set; } = 0;

    [Column("DescuentoPorcentaje", TypeName = "decimal(5,2)")]
    public decimal DescuentoPorcentaje { get; set; } = 0;

    public bool Estado { get; set; } = true;

    public bool Eliminado { get; set; } = false;

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public string? Observaciones { get; set; }
}


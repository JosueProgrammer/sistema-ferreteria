using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sistema_Ferreteria.Models.Common;

namespace Sistema_Ferreteria.Models.Configuracion;

[Table("Configuracion")]
public class Configuracion : ITenantEntity
{
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Key]
    [Column("IdConfig")]
    public int IdConfig { get; set; }

    [Required]
    [MaxLength(100)]
    public string Clave { get; set; } = string.Empty;

    public string? Valor { get; set; }

    [MaxLength(20)]
    public string Tipo { get; set; } = "Texto";

    [MaxLength(50)]
    public string? Modulo { get; set; }

    [MaxLength(255)]
    public string? Descripcion { get; set; }
}


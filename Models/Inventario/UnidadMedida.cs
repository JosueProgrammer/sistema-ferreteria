using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Ferreteria.Models.Inventario;

[Table("UnidadesMedida")]
public class UnidadMedida
{
    [Key]
    [Column("IdUnidad")]
    public int IdUnidad { get; set; }

    [Required]
    [MaxLength(10)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Nombre { get; set; } = string.Empty;

    public bool Estado { get; set; } = true;

    // Navegación
    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    public virtual ICollection<Presentacion> Presentaciones { get; set; } = new List<Presentacion>();
}


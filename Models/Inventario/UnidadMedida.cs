using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Sistema_Ferreteria.Models.Inventario;

[Table("UnidadesMedida")]
public class UnidadMedida
{
    [Key]
    [Column("IdUnidad")]
    public int IdUnidad { get; set; }

    [Required(ErrorMessage = "El código es obligatorio")]
    [MaxLength(10)]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(50)]
    [Display(Name = "Nombre de Unidad")]
    public string Nombre { get; set; } = string.Empty;

    public bool Estado { get; set; } = true;

    // Navegación
    [JsonIgnore]
    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    
    [JsonIgnore]
    public virtual ICollection<Presentacion> Presentaciones { get; set; } = new List<Presentacion>();
}


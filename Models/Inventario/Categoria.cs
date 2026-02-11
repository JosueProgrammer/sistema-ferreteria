using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Sistema_Ferreteria.Models.Inventario;

[Table("Categorias")]
public class Categoria
{
    [Key]
    [Column("IdCategoria")]
    public int IdCategoria { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100)]
    [Display(Name = "Nombre de Categoría")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(255)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    public bool Estado { get; set; } = true;

    public bool Eliminado { get; set; } = false;

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    [JsonIgnore]
    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}


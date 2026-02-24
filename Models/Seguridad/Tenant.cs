using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Ferreteria.Models.Seguridad
{
    [Table("Tenants")]
    public class Tenant
    {
        [Key]
        [MaxLength(50)]
        public string IdTenant { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? IdentificadorFiscal { get; set; } 

        [MaxLength(200)]
        public string? Direccion { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? LogoUrl { get; set; }
    }
}

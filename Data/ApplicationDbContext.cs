using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Models.Seguridad;
using Sistema_Ferreteria.Models.Inventario;
using Sistema_Ferreteria.Models.Clientes;
using Sistema_Ferreteria.Models.Proveedores;
using Sistema_Ferreteria.Models.Ventas;
using Sistema_Ferreteria.Models.Compras;
using Sistema_Ferreteria.Models.Configuracion;

namespace Sistema_Ferreteria.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Seguridad
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Rol> Roles { get; set; }
    public DbSet<Permiso> Permisos { get; set; }
    public DbSet<RolPermiso> RolPermisos { get; set; }
    public DbSet<UsuarioRol> UsuarioRoles { get; set; }
    public DbSet<Auditoria> Auditorias { get; set; }

    // Inventario
    public DbSet<UnidadMedida> UnidadesMedida { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<Presentacion> Presentaciones { get; set; }
    public DbSet<MovimientoInventario> MovimientosInventario { get; set; }

    // Clientes
    public DbSet<Cliente> Clientes { get; set; }

    // Proveedores
    public DbSet<Proveedor> Proveedores { get; set; }

    // Ventas
    public DbSet<Venta> Ventas { get; set; }
    public DbSet<DetalleVenta> DetalleVentas { get; set; }
    public DbSet<PagoVenta> PagosVenta { get; set; }

    // Compras
    public DbSet<Compra> Compras { get; set; }
    public DbSet<DetalleCompra> DetalleCompras { get; set; }
    public DbSet<PagoCompra> PagosCompra { get; set; }

    // Configuración
    public DbSet<Configuracion> Configuraciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuraciones de nombres de tablas y columnas
        // (Entity Framework Core respetará los atributos [Table] y [Column] de los modelos)

        // Índices únicos
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => new { c.TipoDocumento, c.NumeroDocumento })
            .IsUnique();

        modelBuilder.Entity<Proveedor>()
            .HasIndex(p => new { p.TipoDocumento, p.NumeroDocumento })
            .IsUnique();

        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();

        modelBuilder.Entity<Presentacion>()
            .HasIndex(p => new { p.IdProducto, p.NombrePresentacion })
            .IsUnique();

        // Configuración de relaciones
        modelBuilder.Entity<RolPermiso>()
            .HasIndex(rp => new { rp.IdRol, rp.IdPermiso })
            .IsUnique();

        modelBuilder.Entity<UsuarioRol>()
            .HasIndex(ur => new { ur.IdUsuario, ur.IdRol })
            .IsUnique();
    }
}


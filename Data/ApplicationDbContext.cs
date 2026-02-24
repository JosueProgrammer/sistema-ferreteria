using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Models.Seguridad;
using Sistema_Ferreteria.Models.Inventario;
using Sistema_Ferreteria.Models.Clientes;
using Sistema_Ferreteria.Models.Proveedores;
using Sistema_Ferreteria.Models.Ventas;
using Sistema_Ferreteria.Models.Compras;
using Sistema_Ferreteria.Models.Configuracion;
using Sistema_Ferreteria.Models.Common;
using Sistema_Ferreteria.Services;

namespace Sistema_Ferreteria.Data;

public class ApplicationDbContext : DbContext
{
    private readonly ITenantService _tenantService;
    public string TenantId => _tenantService.GetTenantId() ?? "Default";

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService tenantService)
        : base(options)
    {
        _tenantService = tenantService;
    }

    // Seguridad
    public DbSet<Tenant> Tenants { get; set; }
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

        // Global Query Filters for Multi-Tenancy
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var body = System.Linq.Expressions.Expression.Equal(
                    System.Linq.Expressions.Expression.Property(parameter, nameof(ITenantEntity.TenantId)),
                    System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression.Constant(this), nameof(TenantId))
                );
                var lambda = System.Linq.Expressions.Expression.Lambda(body, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        // Configuraciones de nombres de tablas y columnas...
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

        // Relaciones...
        modelBuilder.Entity<RolPermiso>()
            .HasIndex(rp => new { rp.IdRol, rp.IdPermiso })
            .IsUnique();

        modelBuilder.Entity<UsuarioRol>()
            .HasIndex(ur => new { ur.IdUsuario, ur.IdRol })
            .IsUnique();
    }


    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaving();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        OnBeforeSaving();
        return base.SaveChanges();
    }

    private void OnBeforeSaving()
    {
        var entries = ChangeTracker.Entries<ITenantEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrEmpty(entry.Entity.TenantId))
                {
                    entry.Entity.TenantId = TenantId; // Assign resolved tenant
                }
            }
        }
    }
}


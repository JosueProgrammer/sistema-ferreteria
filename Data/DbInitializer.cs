using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Models.Seguridad;
using Microsoft.AspNetCore.Identity;

namespace Sistema_Ferreteria.Data;

public static class DbInitializer
{
    public static async Task Seed(ApplicationDbContext context)
    {
        var passwordHasher = new PasswordHasher<Usuario>();
        // 1. Permisos Base
        if (!await context.Permisos.IgnoreQueryFilters().AnyAsync())
        {
            var permisos = new List<Permiso>
            {
                new Permiso { Nombre = "Ver Dashboard", Modulo = "Dashboard", Codigo = "DASHBOARD_VER", Descripcion = "Ver resumen del sistema" },
                new Permiso { Nombre = "Ver Usuarios", Modulo = "Seguridad", Codigo = "USUARIOS_VER", Descripcion = "Listar usuarios" },
                new Permiso { Nombre = "Gestionar Usuarios", Modulo = "Seguridad", Codigo = "USUARIOS_GESTION", Descripcion = "Crear, editar y eliminar usuarios" },
                new Permiso { Nombre = "Gestionar Roles", Modulo = "Seguridad", Codigo = "ROLES_GESTION", Descripcion = "Configurar roles y permisos" },
                new Permiso { Nombre = "Ver Inventario", Modulo = "Inventario", Codigo = "INVENTARIO_VER", Descripcion = "Ver lista de productos" },
                new Permiso { Nombre = "Gestionar Productos", Modulo = "Inventario", Codigo = "PRODUCTOS_GESTION", Descripcion = "Administrar catálogo de productos" },
                new Permiso { Nombre = "Realizar Venta", Modulo = "Ventas", Codigo = "VENTAS_CREAR", Descripcion = "Acceso al POS" },
                new Permiso { Nombre = "Ver Compras", Modulo = "Compras", Codigo = "COMPRAS_VER", Descripcion = "Ver historial de compras" },
                new Permiso { Nombre = "Gestionar Compras", Modulo = "Compras", Codigo = "COMPRAS_GESTION", Descripcion = "Registrar y recibir compras" },
                new Permiso { Nombre = "Ver Reportes", Modulo = "Reportes", Codigo = "REPORTES_VER", Descripcion = "Acceso a gráficas y reportes" }
            };
            context.Permisos.AddRange(permisos);
            await context.SaveChangesAsync();
        }

        // 2. Roles Base
        if (!await context.Roles.IgnoreQueryFilters().AnyAsync())
        {
            var adminRole = new Rol { Nombre = "Administrador", Descripcion = "Acceso total al sistema", Estado = true };
            var vendedorRole = new Rol { Nombre = "Vendedor", Descripcion = "Ventas y consulta de stock", Estado = true };
            var inventarioRole = new Rol { Nombre = "Inventario", Descripcion = "Gestión de productos y compras", Estado = true };

            context.Roles.AddRange(adminRole, vendedorRole, inventarioRole);
            await context.SaveChangesAsync();

            // Asignar permisos al Admin (Todos)
            var todosPermisos = await context.Permisos.ToListAsync();
            foreach (var p in todosPermisos)
            {
                context.RolPermisos.Add(new RolPermiso { IdRol = adminRole.IdRol, IdPermiso = p.IdPermiso });
            }

            // Asignar permisos al Vendedor
            var permisosVendedor = (await context.Permisos.ToListAsync())
                .Where(p => p.Codigo == "VENTAS_CREAR" || p.Codigo == "INVENTARIO_VER" || p.Codigo == "DASHBOARD_VER");
            foreach (var p in permisosVendedor)
            {
                context.RolPermisos.Add(new RolPermiso { IdRol = vendedorRole.IdRol, IdPermiso = p.IdPermiso });
            }

            await context.SaveChangesAsync();
        }

        // 3. Usuario Admin Inicial
        if (!await context.Usuarios.IgnoreQueryFilters().AnyAsync())
        {
            var adminUser = new Usuario
            {
                Nombre = "Administrador del Sistema",
                NombreUsuario = "admin",
                Estado = true,
                FechaCreacion = DateTime.UtcNow
            };
            adminUser.ContraseñaHash = passwordHasher.HashPassword(adminUser, "admin123");
            context.Usuarios.Add(adminUser);
            await context.SaveChangesAsync();

            // Asignar Rol Admin
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Administrador");
            if (adminRole != null)
            {
                context.UsuarioRoles.Add(new UsuarioRol { IdUsuario = adminUser.IdUsuario, IdRol = adminRole.IdRol });
                await context.SaveChangesAsync();
            }
        }
    }
}

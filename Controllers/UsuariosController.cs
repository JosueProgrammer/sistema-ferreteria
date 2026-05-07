using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Sistema_Ferreteria.Models.Seguridad;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Identity;

using Sistema_Ferreteria.Filters;

namespace Sistema_Ferreteria.Controllers;

[Authorize]
public class UsuariosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuariosController(ApplicationDbContext context, IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [Permiso("USUARIOS_VER")]
    public async Task<IActionResult> Index()
    {
        var usuarios = await _context.Usuarios
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .Where(u => !u.Eliminado)
            .ToListAsync();

        var roles = await _context.Roles
            .Include(r => r.RolPermisos)
                .ThenInclude(rp => rp.Permiso)
            .Where(r => !r.Eliminado)
            .ToListAsync();

        var permisos = await _context.Permisos.ToListAsync();

        ViewBag.Roles = roles;
        ViewBag.Permisos = permisos;

        return View(usuarios);
    }

    [HttpPost]
    [Permiso("USUARIOS_GESTION")]
    public async Task<IActionResult> GuardarUsuario([FromBody] GuardarUsuarioRequest request, [FromQuery] int[]? rolesIds)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(e => !string.IsNullOrWhiteSpace(e)));
                return Json(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Datos de usuario inválidos." : errors });
            }

            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Json(new { success = false, message = "No se pudo determinar la sucursal de la sesión." });
            }

            var roleIdsSanitizados = (rolesIds ?? Array.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToArray();

            if (roleIdsSanitizados.Length > 0)
            {
                var rolesValidos = await _context.Roles
                    .IgnoreQueryFilters()
                    .CountAsync(r => r.TenantId == tenantId && !r.Eliminado && roleIdsSanitizados.Contains(r.IdRol));

                if (rolesValidos != roleIdsSanitizados.Length)
                {
                    return Json(new { success = false, message = "Uno o más roles no pertenecen a la sucursal actual." });
                }
            }

            if (request.IdUsuario == 0)
            {
                var existeUsuario = await _context.Usuarios
                    .AnyAsync(u => u.TenantId == tenantId && u.NombreUsuario == request.NombreUsuario && !u.Eliminado);
                if (existeUsuario)
                {
                    return Json(new { success = false, message = "El nombre de usuario ya está en uso en esta sucursal." });
                }

                if (string.IsNullOrWhiteSpace(request.ContraseñaHash))
                {
                    return Json(new { success = false, message = "La contraseña es obligatoria para crear usuarios." });
                }

                var nuevoUsuario = new Usuario
                {
                    TenantId = tenantId,
                    Nombre = request.Nombre,
                    NombreUsuario = request.NombreUsuario,
                    Estado = request.Estado,
                    FechaCreacion = DateTime.UtcNow
                };
                nuevoUsuario.ContraseñaHash = _passwordHasher.HashPassword(nuevoUsuario, request.ContraseñaHash);

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                // Asignar roles
                foreach (var rid in roleIdsSanitizados)
                {
                    _context.UsuarioRoles.Add(new UsuarioRol { IdUsuario = nuevoUsuario.IdUsuario, IdRol = rid, TenantId = tenantId });
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                var userDb = await _context.Usuarios
                    .Include(u => u.UsuarioRoles)
                    .FirstOrDefaultAsync(u => u.IdUsuario == request.IdUsuario && u.TenantId == tenantId && !u.Eliminado);
                
                if (userDb == null) return Json(new { success = false, message = "Usuario no encontrado" });

                var existeUsuario = await _context.Usuarios
                    .AnyAsync(u =>
                        u.TenantId == tenantId &&
                        u.NombreUsuario == request.NombreUsuario &&
                        u.IdUsuario != request.IdUsuario &&
                        !u.Eliminado);
                if (existeUsuario)
                {
                    return Json(new { success = false, message = "El nombre de usuario ya está en uso en esta sucursal." });
                }

                userDb.Nombre = request.Nombre;
                userDb.NombreUsuario = request.NombreUsuario;
                userDb.TenantId = tenantId;
                if (!string.IsNullOrWhiteSpace(request.ContraseñaHash))
                {
                    userDb.ContraseñaHash = _passwordHasher.HashPassword(userDb, request.ContraseñaHash);
                }
                userDb.Estado = request.Estado;

                // Actualizar roles
                _context.UsuarioRoles.RemoveRange(userDb.UsuarioRoles);
                foreach (var rid in roleIdsSanitizados)
                {
                    _context.UsuarioRoles.Add(new UsuarioRol { IdUsuario = userDb.IdUsuario, IdRol = rid, TenantId = tenantId });
                }

                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Usuario guardado correctamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }

    [HttpPost]
    [Permiso("ROLES_GESTION")]
    public async Task<IActionResult> GuardarRol([FromBody] Rol rol, [FromQuery] int[] permisosIds)
    {
        try
        {
            if (rol.IdRol == 0)
            {
                rol.FechaCreacion = DateTime.UtcNow;
                _context.Roles.Add(rol);
                await _context.SaveChangesAsync();

                foreach (var pid in permisosIds)
                {
                    _context.RolPermisos.Add(new RolPermiso { IdRol = rol.IdRol, IdPermiso = pid });
                }
            }
            else
            {
                var rolDb = await _context.Roles
                    .Include(r => r.RolPermisos)
                    .FirstOrDefaultAsync(r => r.IdRol == rol.IdRol);

                if (rolDb == null) return Json(new { success = false, message = "Rol no encontrado" });

                rolDb.Nombre = rol.Nombre;
                rolDb.Descripcion = rol.Descripcion;
                rolDb.Estado = rol.Estado;

                _context.RolPermisos.RemoveRange(rolDb.RolPermisos);
                foreach (var pid in permisosIds)
                {
                    _context.RolPermisos.Add(new RolPermiso { IdRol = rolDb.IdRol, IdPermiso = pid });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Rol guardado correctamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }

    [HttpPost]
    [Permiso("USUARIOS_GESTION")]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var user = await _context.Usuarios.FindAsync(id);
        if (user == null) return Json(new { success = false });
        user.Eliminado = true;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction("Login", "Cuentas");

        int userId = int.Parse(userIdClaim);
        var usuario = await _context.Usuarios
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(u => u.IdUsuario == userId);

        if (usuario == null) return NotFound();

        return View(usuario);
    }

    [HttpPost]
    public async Task<IActionResult> CambiarPassword(string passwordActual, string nuevaPassword)
    {
        try
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Json(new { success = false, message = "Sesión no válida" });

            int userId = int.Parse(userIdClaim);
            var usuario = await _context.Usuarios.FindAsync(userId);

            if (usuario == null) return Json(new { success = false, message = "Usuario no encontrado" });

            var result = _passwordHasher.VerifyHashedPassword(usuario, usuario.ContraseñaHash, passwordActual);
            if (result == PasswordVerificationResult.Failed)
            {
                return Json(new { success = false, message = "La contraseña actual es incorrecta" });
            }

            usuario.ContraseñaHash = _passwordHasher.HashPassword(usuario, nuevaPassword);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Contraseña actualizada con éxito" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ActualizarInformacion(string nombre, string nombreUsuario)
    {
        try
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Json(new { success = false, message = "Sesión no válida" });

            int userId = int.Parse(userIdClaim);
            var usuario = await _context.Usuarios.FindAsync(userId);

            if (usuario == null) return Json(new { success = false, message = "Usuario no encontrado" });

            // Verificar si el nombre de usuario ya existe para otro usuario
            var existe = await _context.Usuarios.AnyAsync(u =>
                u.TenantId == usuario.TenantId &&
                u.NombreUsuario == nombreUsuario &&
                u.IdUsuario != userId &&
                !u.Eliminado);
            if (existe) return Json(new { success = false, message = "El nombre de usuario ya está en uso" });

            usuario.Nombre = nombre;
            usuario.NombreUsuario = nombreUsuario;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Información actualizada correctamente. Los cambios se verán reflejados al iniciar sesión nuevamente." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }
}

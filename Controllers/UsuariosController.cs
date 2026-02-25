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
    public async Task<IActionResult> GuardarUsuario([FromBody] Usuario usuario, [FromQuery] int[] rolesIds)
    {
        try
        {
            if (usuario.IdUsuario == 0)
            {
                usuario.FechaCreacion = DateTime.UtcNow;
                // Hash password
                if (!string.IsNullOrEmpty(usuario.ContraseñaHash))
                {
                    usuario.ContraseñaHash = _passwordHasher.HashPassword(usuario, usuario.ContraseñaHash);
                }
                
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // Asignar roles
                if (rolesIds != null)
                {
                    foreach (var rid in rolesIds)
                    {
                        _context.UsuarioRoles.Add(new UsuarioRol { IdUsuario = usuario.IdUsuario, IdRol = rid });
                    }
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var userDb = await _context.Usuarios
                    .Include(u => u.UsuarioRoles)
                    .FirstOrDefaultAsync(u => u.IdUsuario == usuario.IdUsuario);
                
                if (userDb == null) return Json(new { success = false, message = "Usuario no encontrado" });

                userDb.Nombre = usuario.Nombre;
                userDb.NombreUsuario = usuario.NombreUsuario;
                if (!string.IsNullOrEmpty(usuario.ContraseñaHash))
                {
                    userDb.ContraseñaHash = _passwordHasher.HashPassword(userDb, usuario.ContraseñaHash);
                }
                userDb.Estado = usuario.Estado;

                // Actualizar roles
                _context.UsuarioRoles.RemoveRange(userDb.UsuarioRoles);
                if (rolesIds != null)
                {
                    foreach (var rid in rolesIds)
                    {
                        _context.UsuarioRoles.Add(new UsuarioRol { IdUsuario = userDb.IdUsuario, IdRol = rid });
                    }
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
                // Fallback para migración: si la contraseña en DB es texto plano y coincide
                if (usuario.ContraseñaHash != passwordActual)
                {
                    return Json(new { success = false, message = "La contraseña actual es incorrecta" });
                }
                // Si llegamos aquí, es porque coincidió en texto plano. 
                // Continuamos para que se guarde con el nuevo hash abajo.
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
            var existe = await _context.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario && u.IdUsuario != userId);
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

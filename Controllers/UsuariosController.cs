using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Sistema_Ferreteria.Models.Seguridad;
using Microsoft.AspNetCore.Authorization;

namespace Sistema_Ferreteria.Controllers;

[Authorize]
public class UsuariosController : Controller
{
    private readonly ApplicationDbContext _context;

    public UsuariosController(ApplicationDbContext context)
    {
        _context = context;
    }

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
    public async Task<IActionResult> GuardarUsuario([FromBody] Usuario usuario, [FromQuery] int[] rolesIds)
    {
        try
        {
            if (usuario.IdUsuario == 0)
            {
                usuario.FechaCreacion = DateTime.UtcNow;
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
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
                    userDb.ContraseñaHash = usuario.ContraseñaHash;
                }
                userDb.Estado = usuario.Estado;

                // Actualizar roles
                _context.UsuarioRoles.RemoveRange(userDb.UsuarioRoles);
                foreach (var rid in rolesIds)
                {
                    _context.UsuarioRoles.Add(new UsuarioRol { IdUsuario = userDb.IdUsuario, IdRol = rid });
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
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var user = await _context.Usuarios.FindAsync(id);
        if (user == null) return Json(new { success = false });
        user.Eliminado = true;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
}

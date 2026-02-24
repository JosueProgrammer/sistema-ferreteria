using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Sistema_Ferreteria.Models.Seguridad;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Sistema_Ferreteria.Controllers;

public class CuentasController : Controller
{
    private readonly ApplicationDbContext _context;

    public CuentasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var usuario = await _context.Usuarios
            .IgnoreQueryFilters() // Must ignore filters to find user across tenants if needed
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(u => u.NombreUsuario == model.Usuario && !u.Eliminado && u.Estado);

        if (usuario == null)
        {
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
            return View(model);
        }

        // TODO: En producción usar hashing real.
        if (usuario.ContraseñaHash != model.Password) 
        {
             ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
             return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim("NombreCompleto", usuario.Nombre),
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim("TenantId", usuario.TenantId) // Critical for multi-tenancy
        };

        foreach (var rol in usuario.UsuarioRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, rol.Rol.Nombre));
        }

        var claimsIdentity = new ClaimsIdentity(claims, "FerreteriaAuth");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.Recordarme
        };

        await HttpContext.SignInAsync("FerreteriaAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("FerreteriaAuth");
        return RedirectToAction("Login");
    }

    public IActionResult AccesoDenegado()
    {
        return View();
    }
}

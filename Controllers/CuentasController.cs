using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Sistema_Ferreteria.Models.Seguridad;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Sistema_Ferreteria.Controllers;

public class CuentasController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public CuentasController(ApplicationDbContext context, IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
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
                    .ThenInclude(r => r.RolPermisos)
                        .ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(u => u.NombreUsuario == model.Usuario && !u.Eliminado && u.Estado);

        if (usuario == null)
        {
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
            return View(model);
        }

        var result = _passwordHasher.VerifyHashedPassword(usuario, usuario.ContraseñaHash, model.Password);
        
        if (result == PasswordVerificationResult.Failed)
        {
            // Fallback para migración: si la contraseña guardada coincide exactamente con la plana
            // (esto significa que aún no ha sido hasheada)
            if (usuario.ContraseñaHash == model.Password)
            {
                // Hashear y actualizar ahora mismo
                usuario.ContraseñaHash = _passwordHasher.HashPassword(usuario, model.Password);
                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();
            }
            else
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(model);
            }
        }
        else if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Actualizar hash si el algoritmo cambió o se fortaleció
            usuario.ContraseñaHash = _passwordHasher.HashPassword(usuario, model.Password);
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim("NombreCompleto", usuario.Nombre),
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim("TenantId", usuario.TenantId) // Critical for multi-tenancy
        };

        // Agregar roles y permisos
        var permisosProcesados = new HashSet<string>();
        foreach (var ur in usuario.UsuarioRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, ur.Rol.Nombre));
            foreach (var rp in ur.Rol.RolPermisos)
            {
                if (rp.Permiso != null && !permisosProcesados.Contains(rp.Permiso.Codigo))
                {
                    claims.Add(new Claim("Permiso", rp.Permiso.Codigo));
                    permisosProcesados.Add(rp.Permiso.Codigo);
                }
            }
        }

        var claimsIdentity = new ClaimsIdentity(claims, "FerreteriaAuth");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.Recordarme
        };

        await HttpContext.SignInAsync("FerreteriaAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

        var isAdmin = claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Administrador");
        var tenantCount = await _context.Tenants.CountAsync(t => t.Activo);
        if (isAdmin && tenantCount >= 2)
            return RedirectToAction("SeleccionarTenant");

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> SeleccionarTenant()
    {
        var tenants = await _context.Tenants
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .ToListAsync();
        return View(tenants);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> SeleccionarTenant(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            TempData["Error"] = "Debe seleccionar una sucursal.";
            return RedirectToAction("SeleccionarTenant");
        }

        var tenantExists = await _context.Tenants
            .AnyAsync(t => t.IdTenant == tenantId && t.Activo);
        if (!tenantExists)
        {
            TempData["Error"] = "La sucursal seleccionada no es válida.";
            return RedirectToAction("SeleccionarTenant");
        }

        var currentClaims = User.Claims
            .Where(c => c.Type != "TenantId")
            .Select(c => new Claim(c.Type, c.Value))
            .ToList();
        currentClaims.Add(new Claim("TenantId", tenantId));

        var newIdentity = new ClaimsIdentity(currentClaims, "FerreteriaAuth");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true
        };

        await HttpContext.SignOutAsync("FerreteriaAuth");
        await HttpContext.SignInAsync("FerreteriaAuth", new ClaimsPrincipal(newIdentity), authProperties);

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

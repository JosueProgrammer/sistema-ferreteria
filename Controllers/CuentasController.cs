using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Sistema_Ferreteria.Models.Seguridad;
using Sistema_Ferreteria.Filters;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace Sistema_Ferreteria.Controllers;

[SkipLicenseCheck]
public class CuentasController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly ILogger<CuentasController> _logger;

    public CuentasController(
        ApplicationDbContext context,
        IPasswordHasher<Usuario> passwordHasher,
        ILogger<CuentasController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        ViewBag.Tenants = _context.Tenants
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var requestIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
        if (!ModelState.IsValid)
        {
            ViewBag.Tenants = await _context.Tenants
                .Where(t => t.Activo)
                .OrderBy(t => t.Nombre)
                .ToListAsync();
            return View(model);
        }

        var tenantValido = await _context.Tenants
            .AnyAsync(t => t.IdTenant == model.TenantId && t.Activo);
        if (!tenantValido)
        {
            _logger.LogWarning(
                "login_failed tenant_invalid ip:{Ip} usuario:{Usuario} tenant:{TenantId}",
                requestIp,
                model.Usuario,
                model.TenantId);
            ModelState.AddModelError(nameof(model.TenantId), "La sucursal seleccionada no es válida.");
            ViewBag.Tenants = await _context.Tenants
                .Where(t => t.Activo)
                .OrderBy(t => t.Nombre)
                .ToListAsync();
            return View(model);
        }

        var usuario = await _context.Usuarios
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
                    .ThenInclude(r => r.RolPermisos)
                        .ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(u =>
                u.NombreUsuario == model.Usuario &&
                u.TenantId == model.TenantId &&
                !u.Eliminado &&
                u.Estado);

        if (usuario == null)
        {
            _logger.LogWarning(
                "login_failed user_not_found ip:{Ip} usuario:{Usuario} tenant:{TenantId}",
                requestIp,
                model.Usuario,
                model.TenantId);
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
            ViewBag.Tenants = await _context.Tenants
                .Where(t => t.Activo)
                .OrderBy(t => t.Nombre)
                .ToListAsync();
            return View(model);
        }

        var now = DateTime.UtcNow;
        if (usuario.LockoutEnd.HasValue && usuario.LockoutEnd.Value > now)
        {
            _logger.LogWarning(
                "login_locked ip:{Ip} usuario:{Usuario} tenant:{TenantId} lockoutEnd:{LockoutEnd}",
                requestIp,
                model.Usuario,
                model.TenantId,
                usuario.LockoutEnd.Value);
            ModelState.AddModelError("", "Tu cuenta está bloqueada temporalmente por múltiples intentos fallidos. Inténtalo más tarde.");
            ViewBag.Tenants = await _context.Tenants
                .Where(t => t.Activo)
                .OrderBy(t => t.Nombre)
                .ToListAsync();
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
                usuario.AccessFailedCount += 1;
                if (usuario.AccessFailedCount >= 5)
                {
                    usuario.LockoutEnd = now.AddMinutes(15);
                    _logger.LogWarning(
                        "login_locked ip:{Ip} usuario:{Usuario} tenant:{TenantId} failedCount:{FailedCount} lockoutEnd:{LockoutEnd}",
                        requestIp,
                        model.Usuario,
                        model.TenantId,
                        usuario.AccessFailedCount,
                        usuario.LockoutEnd.Value);
                }

                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();
                _logger.LogWarning(
                    "login_failed bad_password ip:{Ip} usuario:{Usuario} tenant:{TenantId} failedCount:{FailedCount}",
                    requestIp,
                    model.Usuario,
                    model.TenantId,
                    usuario.AccessFailedCount);
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                ViewBag.Tenants = await _context.Tenants
                    .Where(t => t.Activo)
                    .OrderBy(t => t.Nombre)
                    .ToListAsync();
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

        if (usuario.AccessFailedCount > 0 || usuario.LockoutEnd.HasValue)
        {
            usuario.AccessFailedCount = 0;
            usuario.LockoutEnd = null;
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
        _logger.LogInformation(
            "login_success ip:{Ip} usuario:{Usuario} tenant:{TenantId}",
            requestIp,
            model.Usuario,
            model.TenantId);

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

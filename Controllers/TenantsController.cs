using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Sistema_Ferreteria.Models.Seguridad;
using Microsoft.AspNetCore.Authorization;

namespace Sistema_Ferreteria.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class TenantsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TenantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tenants
        public async Task<IActionResult> Index()
        {
            return View(await _context.Tenants.OrderByDescending(t => t.FechaCreacion).ToListAsync());
        }

        // GET: Tenants/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(m => m.IdTenant == id);
            
            if (tenant == null) return NotFound();

            return View(tenant);
        }

        // GET: Tenants/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tenants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdTenant,Nombre,IdentificadorFiscal,Direccion,Activo,LogoUrl")] Tenant tenant)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Tenants.AnyAsync(t => t.IdTenant == tenant.IdTenant))
                {
                    ModelState.AddModelError("IdTenant", "Este ID de Tenant ya está en uso.");
                    return View(tenant);
                }

                tenant.FechaCreacion = DateTime.UtcNow;
                _context.Add(tenant);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tenant);
        }

        // GET: Tenants/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return NotFound();
            
            return View(tenant);
        }

        // POST: Tenants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("IdTenant,Nombre,IdentificadorFiscal,Direccion,Activo,LogoUrl,FechaCreacion")] Tenant tenant)
        {
            if (id != tenant.IdTenant) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tenant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TenantExists(tenant.IdTenant)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tenant);
        }

        // GET: Tenants/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(m => m.IdTenant == id);
            
            if (tenant == null) return NotFound();

            return View(tenant);
        }

        // POST: Tenants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant != null)
            {
                // In a real system, you might want to prevent deleting the 'Default' tenant
                if (id == "Default")
                {
                    TempData["Error"] = "No se puede eliminar el tenant principal.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Tenants.Remove(tenant);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TenantExists(string id)
        {
            return _context.Tenants.Any(e => e.IdTenant == id);
        }
    }
}

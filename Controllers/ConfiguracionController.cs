using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;

namespace Sistema_Ferreteria.Controllers;

public class ConfiguracionController : Controller
{
    private readonly ApplicationDbContext _context;

    public ConfiguracionController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// API: Retorna todas las configuraciones como JSON para renderizado dinámico en el frontend.
    /// </summary>
    [HttpGet]
    [Route("Configuracion/Api/ObtenerConfiguraciones")]
    public async Task<IActionResult> ObtenerConfiguracionesJson()
    {
        var configuraciones = await _context.Configuraciones
            .OrderBy(c => c.Modulo)
            .ThenBy(c => c.Clave)
            .Select(c => new
            {
                c.IdConfig,
                c.Clave,
                c.Valor,
                c.Tipo,
                Modulo = c.Modulo ?? "General",
                c.Descripcion
            })
            .ToListAsync();

        // Debug: Agrupación por módulo (Console.WriteLine)
        var agrupado = configuraciones.GroupBy(c => c.Modulo).ToList();
        foreach (var grupo in agrupado)
        {
            Console.WriteLine($"[Configuracion] Módulo '{grupo.Key}' tiene {grupo.Count()} ítem(s)");
        }

        return Json(configuraciones);
    }

    // GET: Configuracion - Vista con contenedor; el formulario se genera dinámicamente vía JavaScript
    public IActionResult Index()
    {
        return View();
    }

    // POST: Configuracion/GuardarSeccion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarSeccion(string modulo, Dictionary<int, string> valores)
    {
        if (valores == null || !valores.Any())
        {
            TempData["ErrorMessage"] = "No se recibieron valores para guardar.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Obtener las configuraciones de este módulo
            var configuraciones = await _context.Configuraciones
                .Where(c => c.Modulo == modulo)
                .ToListAsync();

            // Actualizar valores
            foreach (var config in configuraciones)
            {
                if (valores.ContainsKey(config.IdConfig))
                {
                    config.Valor = valores[config.IdConfig];
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Configuración de '{modulo}' guardada correctamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al guardar: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
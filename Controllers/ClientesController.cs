using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Sistema_Ferreteria.Models.Clientes;
using Microsoft.AspNetCore.Authorization;

namespace Sistema_Ferreteria.Controllers
{
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var clientes = await _context.Clientes
                .Where(c => !c.Eliminado)
                .OrderByDescending(c => c.FechaCreacion)
                .ToListAsync();
            return View(clientes);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Cliente cliente)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Datos inválidos" });

                cliente.FechaCreacion = DateTime.UtcNow;
                cliente.Estado = true;
                cliente.Eliminado = false;
                
                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cliente registrado con éxito" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Editar([FromBody] Cliente cliente)
        {
            try
            {
                var existing = await _context.Clientes.FindAsync(cliente.IdCliente);
                if (existing == null) return Json(new { success = false, message = "Cliente no encontrado" });

                existing.Nombre = cliente.Nombre;
                existing.TipoDocumento = cliente.TipoDocumento;
                existing.NumeroDocumento = cliente.NumeroDocumento;
                existing.Telefono = cliente.Telefono;
                existing.Direccion = cliente.Direccion;
                existing.LimiteCredito = cliente.LimiteCredito;
                existing.DescuentoPorcentaje = cliente.DescuentoPorcentaje;
                existing.Observaciones = cliente.Observaciones;
                existing.Estado = cliente.Estado;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cliente actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return Json(new { success = false, message = "Cliente no encontrado" });

            cliente.Eliminado = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cliente eliminado correctamente" });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDetalle(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();
            return Json(cliente);
        }
    }
}

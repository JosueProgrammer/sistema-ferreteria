using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Sistema_Ferreteria.Models.Inventario;

using Microsoft.AspNetCore.Authorization;

namespace Sistema_Ferreteria.Controllers
{
    [Authorize]
    public class InventarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.UnidadBase)
                .Include(p => p.Presentaciones)
                .Where(p => !p.Eliminado)
                .ToListAsync();

            ViewBag.Categorias = await _context.Categorias.Where(c => !c.Eliminado).ToListAsync();
            ViewBag.UnidadesMedida = await _context.UnidadesMedida.Where(u => u.Estado).ToListAsync();

            return View(productos);
        }

        [HttpPost]
        public async Task<IActionResult> CrearProducto([FromBody] Producto producto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => {
                    var msg = e.ErrorMessage;
                    if (string.IsNullOrEmpty(msg) && e.Exception != null) msg = e.Exception.Message;
                    if (msg.Contains("could not be converted")) return "Formato numérico o de datos inválido.";
                    if (msg.Contains("is required")) return "Este campo es obligatorio.";
                    return msg;
                }).ToList();
                return Json(new { success = false, message = "Revisar los datos del formulario", errors });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try 
            {
                producto.FechaCreacion = DateTime.UtcNow;
                _context.Productos.Add(producto);
                await _context.SaveChangesAsync(); // Para obtener IdProducto

                // Si el producto viene con stock inicial, registrar movimiento
                if (producto.StockBase > 0)
                {
                    var defaultUser = await _context.Usuarios.FirstOrDefaultAsync();
                    var movimiento = new MovimientoInventario
                    {
                        IdProducto = producto.IdProducto,
                        TipoMovimiento = "Entrada",
                        CantidadBase = producto.StockBase,
                        Fecha = DateTime.UtcNow,
                        Observacion = "Stock inicial al crear producto",
                        IdUsuario = defaultUser?.IdUsuario ?? 1
                    };
                    _context.MovimientosInventario.Add(movimiento);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return Json(new { success = true, message = "Producto creado correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error al guardar en base de datos", errors = new[] { ex.Message } });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditarProducto([FromBody] Producto producto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => {
                    var msg = e.ErrorMessage;
                    if (string.IsNullOrEmpty(msg) && e.Exception != null) msg = e.Exception.Message;
                    if (msg.Contains("could not be converted")) return "Formato numérico o de datos inválido.";
                    if (msg.Contains("is required")) return "Este campo es obligatorio.";
                    return msg;
                }).ToList();
                return Json(new { success = false, message = "Revisar los datos del formulario", errors });
            }

            var existing = await _context.Productos
                .Include(p => p.Presentaciones)
                .FirstOrDefaultAsync(p => p.IdProducto == producto.IdProducto);

            if (existing == null) return Json(new { success = false, message = "Producto no encontrado" });

            try 
            {
                existing.Nombre = producto.Nombre;
                existing.Codigo = producto.Codigo;
                existing.CodigoBarras = producto.CodigoBarras;
                existing.Descripcion = producto.Descripcion;
                existing.IdCategoria = producto.IdCategoria;
                existing.IdUnidadBase = producto.IdUnidadBase;
                existing.StockMinimo = producto.StockMinimo;
                existing.PrecioBaseVenta = producto.PrecioBaseVenta;
                existing.PrecioBaseCompra = producto.PrecioBaseCompra;
                existing.Estado = producto.Estado;

                // Sincronizar Presentaciones
                // 1. Eliminar las que ya no están
                var presIdsToDelete = existing.Presentaciones
                    .Where(ep => !producto.Presentaciones.Any(p => p.IdPresentacion == ep.IdPresentacion))
                    .Select(ep => ep.IdPresentacion).ToList();
                
                foreach(var id in presIdsToDelete) {
                    var toDelete = existing.Presentaciones.First(p => p.IdPresentacion == id);
                    _context.Presentaciones.Remove(toDelete);
                }

                // 2. Actualizar o Agregar
                foreach(var p in producto.Presentaciones)
                {
                    var existingPres = existing.Presentaciones.FirstOrDefault(ep => ep.IdPresentacion == p.IdPresentacion && p.IdPresentacion != 0);
                    if (existingPres != null)
                    {
                        existingPres.NombrePresentacion = p.NombrePresentacion;
                        existingPres.IdUnidadPresentacion = p.IdUnidadPresentacion;
                        existingPres.FactorConversion = p.FactorConversion;
                        existingPres.PrecioVenta = p.PrecioVenta;
                    }
                    else
                    {
                        p.IdProducto = existing.IdProducto;
                        existing.Presentaciones.Add(p);
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Producto actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar", errors = new[] { ex.Message } });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            producto.Eliminado = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Producto eliminado correctamente" });
        }

        [HttpPost]
        public async Task<IActionResult> AjustarStock(int idProducto, decimal cantidad, string tipo, string observacion)
        {
            var producto = await _context.Productos.FindAsync(idProducto);
            if (producto == null) return Json(new { success = false, message = "Producto no encontrado" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var defaultUser = await _context.Usuarios.FirstOrDefaultAsync();
                int userId = defaultUser?.IdUsuario ?? 1;

                if (tipo == "Entrada") producto.StockBase += cantidad;
                else if (tipo == "Salida" || tipo == "Merma") producto.StockBase -= cantidad;
                else if (tipo == "Ajuste") producto.StockBase = cantidad; // Ajuste directo

                var movimiento = new MovimientoInventario
                {
                    IdProducto = idProducto,
                    TipoMovimiento = tipo,
                    CantidadBase = cantidad,
                    Fecha = DateTime.UtcNow,
                    Observacion = observacion,
                    IdUsuario = userId
                };

                _context.MovimientosInventario.Add(movimiento);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Inventario actualizado satisfactoriamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Categorías
        [HttpGet]
        public async Task<IActionResult> GetCategorias()
        {
            var categorias = await _context.Categorias.Where(c => !c.Eliminado).ToListAsync();
            return Json(categorias);
        }

        [HttpPost]
        public async Task<IActionResult> CrearCategoria([FromBody] Categoria categoria)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Datos de categoría inválidos", errors });
            }
            
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = categoria.IdCategoria });
        }

        // Unidades
        [HttpPost]
        public async Task<IActionResult> CrearUnidad([FromBody] UnidadMedida unidad)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Datos de unidad inválidos", errors });
            }

            _context.UnidadesMedida.Add(unidad);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = unidad.IdUnidad });
        }
    }
}

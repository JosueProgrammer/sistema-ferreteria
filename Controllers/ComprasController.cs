using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Sistema_Ferreteria.Models.Compras;
using Sistema_Ferreteria.Models.Inventario;
using Microsoft.AspNetCore.Authorization; // Added this using statement

namespace Sistema_Ferreteria.Controllers
{
    [Authorize]
public class ComprasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComprasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var compras = await _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Usuario)
                .Include(c => c.DetalleCompras)
                    .ThenInclude(d => d.Producto)
                .Include(c => c.DetalleCompras)
                    .ThenInclude(d => d.Presentacion)
                .Include(c => c.PagosCompra)
                .Where(c => !c.Eliminado)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            ViewBag.Proveedores = await _context.Proveedores.Where(p => p.Estado && !p.Eliminado).ToListAsync();
            // Para la búsqueda de productos en el formulario
            ViewBag.Productos = await _context.Productos
                .Include(p => p.Presentaciones)
                 .ThenInclude(pr => pr.UnidadPresentacion)
                .Include(p => p.UnidadBase)
                .Where(p => p.Estado && !p.Eliminado)
                .ToListAsync();

            return View(compras);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Compra compra)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => {
                    var msg = e.ErrorMessage;
                    if (string.IsNullOrEmpty(msg) && e.Exception != null) msg = e.Exception.Message;
                    if (msg.Contains("is required")) return "Este campo es obligatorio.";
                    return msg;
                }).ToList();
                return Json(new { success = false, message = "Datos inválidos", errors });
            }

            try
            {
                // Asignar datos por defecto
                compra.Fecha = DateTime.UtcNow;
                
                // Intentar obtener el primer usuario si no hay autenticación
                var defaultUser = await _context.Usuarios.FirstOrDefaultAsync();
                if (defaultUser == null)
                {
                    return Json(new { success = false, message = "Error: No existe ningún usuario registrado en el sistema para asociar la compra." });
                }
                compra.IdUsuario = defaultUser.IdUsuario;
                
                compra.Estado = "Pendiente";
                compra.Eliminado = false;

                // Calcular fecha vencimiento por defecto si no viene
                if (!compra.FechaVencimiento.HasValue)
                {
                    var prov = await _context.Proveedores.FindAsync(compra.IdProveedor);
                    if (prov != null && prov.PlazoPago > 0)
                    {
                        compra.FechaVencimiento = compra.Fecha.AddDays(prov.PlazoPago);
                    }
                }

                // Calcular total si no viene
                if (compra.Total == 0)
                {
                    compra.Total = compra.DetalleCompras.Sum(d => (d.Cantidad * d.PrecioUnitario) - d.DescuentoMonto);
                }

                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "¡Pedido guardado! La orden de compra ha sido registrada satisfactoriamente.", id = compra.IdCompra });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMsg = dbEx.InnerException?.Message ?? dbEx.Message;
                // Intentar traducir o limpiar mensajes comunes de DB (Postgres)
                string friendlyMsg = "Error de base de datos";
                if (innerMsg.Contains("FK_Compra_Proveedor")) friendlyMsg = "El proveedor seleccionado no es válido o ha sido eliminado.";
                else if (innerMsg.Contains("FK_DetalleCompra_Producto")) friendlyMsg = "Uno de los productos seleccionados no es válido.";
                else friendlyMsg = "Error al guardar en la base de datos: " + innerMsg;

                return Json(new { success = false, message = friendlyMsg });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error inesperado: " + ex.Message });
            }
        }

        public class PagoRequest {
            public long Id { get; set; }
            public long IdCompra { get; set; }
            public decimal Monto { get; set; }
            public string? Metodo { get; set; }
            public string? Comprobante { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarPago([FromBody] PagoRequest req)
        {
            var compra = await _context.Compras.FindAsync(req.IdCompra);
            if (compra == null) return Json(new { success = false, message = "Compra no encontrada." });

            var defaultUser = await _context.Usuarios.FirstOrDefaultAsync();
            int userId = defaultUser?.IdUsuario ?? 1;

            var pago = new PagoCompra
            {
                IdCompra = req.IdCompra,
                Monto = req.Monto,
                MetodoPago = req.Metodo ?? "Efectivo",
                NumeroComprobante = req.Comprobante ?? "",
                FechaPago = DateTime.UtcNow,
                IdUsuario = userId
            };

            _context.PagosCompra.Add(pago);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Pago registrado correctamente." });
        }

        [HttpPost]
        public async Task<IActionResult> PagarYRecibir([FromBody] PagoRequest req)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Registrar el pago
                var resPago = await RegistrarPago(req);
                if (resPago is JsonResult jsonPago)
                {
                    var data = jsonPago.Value as dynamic;
                    bool success = data?.success ?? false;
                    if (!success) throw new Exception(data?.message?.ToString() ?? "Error al registrar pago");
                }

                // 2. Recibir la mercadería
                var resRecibir = await Recibir(req.Id);
                if (resRecibir is JsonResult jsonRecibir)
                {
                    var data = jsonRecibir.Value as dynamic;
                    bool success = data?.success ?? false;
                    if (!success) throw new Exception(data?.message?.ToString() ?? "Error al recibir mercadería");
                }

                await transaction.CommitAsync();
                return Json(new { success = true, message = "¡Procesado! Se ha registrado el pago y el ingreso a inventario satisfactoriamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error al procesar operación combinada: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Recibir(long id)
        {
            var compra = await _context.Compras
                .Include(c => c.DetalleCompras)
                    .ThenInclude(d => d.Presentacion)
                .FirstOrDefaultAsync(c => c.IdCompra == id);

            if (compra == null) return Json(new { success = false, message = "Orden de compra no encontrada." });
            if (compra.Estado == "Recibida") return Json(new { success = false, message = "Esta orden ya ha sido marcada como recibida previamente." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                compra.Estado = "Recibida";

                var defaultUser = await _context.Usuarios.FirstOrDefaultAsync();
                int userId = defaultUser?.IdUsuario ?? 1;

                foreach (var detalle in compra.DetalleCompras)
                {
                    // 1. Actualizar el StockBase denormalizado en la tabla Productos
                    var producto = await _context.Productos.FindAsync(detalle.IdProducto);
                    if (producto != null)
                    {
                        producto.StockBase += detalle.CantidadBase;
                    }

                    // 2. Registrar el rastro histórico en MovimientosInventario
                    var movimiento = new MovimientoInventario
                    {
                        IdProducto = detalle.IdProducto,
                        TipoMovimiento = "Entrada",
                        CantidadBase = detalle.CantidadBase,
                        Fecha = DateTime.UtcNow,
                        Observacion = $"Entrada por Recepción de Compra #{compra.IdCompra}",
                        IdUsuario = userId,
                        IdReferencia = (int)compra.IdCompra,
                        TipoReferencia = "Compra"
                    };

                    _context.MovimientosInventario.Add(movimiento);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "¡Inventario Actualizado! La mercadería ha ingresado al stock correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error al procesar la entrada: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(long id)
        {
            var compra = await _context.Compras.FindAsync(id);
            if (compra == null) return Json(new { success = false, message = "No se encontró el registro a eliminar." });
            if (compra.Estado == "Recibida") return Json(new { success = false, message = "No es posible eliminar una compra que ya ha sido recibida en inventario." });

            compra.Eliminado = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "La orden de compra ha sido eliminada del sistema." });
        }

        [HttpPost]
        public async Task<IActionResult> CrearProveedor([FromBody] Sistema_Ferreteria.Models.Proveedores.Proveedor proveedor)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => {
                    var msg = e.ErrorMessage;
                    if (string.IsNullOrEmpty(msg) && e.Exception != null) msg = e.Exception.Message;
                    if (msg.Contains("is required")) return "Este campo es obligatorio.";
                    return msg;
                }).ToList();
                return Json(new { success = false, message = "Datos inválidos", errors });
            }

            try
            {
                _context.Proveedores.Add(proveedor);
                await _context.SaveChangesAsync();
                return Json(new { success = true, id = proveedor.IdProveedor, razonSocial = proveedor.RazonSocial });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}

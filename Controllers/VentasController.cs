using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Sistema_Ferreteria.Models.Ventas;
using Sistema_Ferreteria.Models.Inventario;
using Sistema_Ferreteria.Models.Clientes;
using Microsoft.AspNetCore.Authorization;

namespace Sistema_Ferreteria.Controllers
{
    [Authorize]
    public class VentasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VentasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.DetalleVentas)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.DetalleVentas)
                    .ThenInclude(d => d.Presentacion)
                .Include(v => v.PagosVenta)
                .Where(v => !v.Eliminado)
                .OrderByDescending(v => v.Fecha)
                .Take(20) // Solo las últimas 20 para el tique rápido del POS
                .ToListAsync();

            ViewBag.Clientes = await _context.Clientes.Where(c => c.Estado && !c.Eliminado).ToListAsync();
            ViewBag.Productos = await _context.Productos
                .Include(p => p.Presentaciones)
                    .ThenInclude(pr => pr.UnidadPresentacion)
                .Include(p => p.UnidadBase)
                .Where(p => p.Estado && !p.Eliminado && p.StockBase > 0)
                .ToListAsync();

            return View(ventas);
        }

        public async Task<IActionResult> Historial()
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.PagosVenta)
                .Where(v => !v.Eliminado)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            return View(ventas);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDetalle(long id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.DetalleVentas)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.DetalleVentas)
                    .ThenInclude(d => d.Presentacion)
                .Include(v => v.PagosVenta)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

            if (venta == null) return NotFound();

            return Json(new {
                factura = venta.NumeroFactura,
                fecha = venta.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                cliente = venta.Cliente?.Nombre ?? "Consumidor Final",
                vendedor = venta.Usuario?.Nombre ?? "Sistema",
                tipoPago = venta.TipoPago,
                estado = venta.Estado,
                total = venta.Total,
                pagado = venta.PagosVenta.Sum(p => p.Monto),
                detalles = venta.DetalleVentas.Select(d => new {
                    producto = d.Producto.Nombre,
                    presentacion = d.Presentacion.NombrePresentacion,
                    cantidad = d.Cantidad,
                    precio = d.PrecioUnitario,
                    subtotal = d.Cantidad * d.PrecioUnitario
                }),
                pagos = venta.PagosVenta.Select(p => new {
                    fecha = p.FechaPago.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    monto = p.Monto,
                    metodo = p.MetodoPago,
                    comprobante = p.NumeroComprobante
                })
            });
        }

        public class VentaRequest
        {
            public int? IdCliente { get; set; }
            public string TipoPago { get; set; } = "Contado";
            public string? Observaciones { get; set; }
            public List<DetalleRequest> Detalles { get; set; } = new();
            public PagoVentaRequest? PagoInicial { get; set; }
        }

        public class DetalleRequest
        {
            public int IdProducto { get; set; }
            public int IdPresentacion { get; set; }
            public decimal Cantidad { get; set; }
            public decimal CantidadBase { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Descuento { get; set; }
        }

        public class PagoVentaRequest
        {
            public decimal Monto { get; set; }
            public string Metodo { get; set; } = "Efectivo";
            public string? Comprobante { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] VentaRequest req)
        {
            if (req.Detalles == null || !req.Detalles.Any())
                return Json(new { success = false, message = "La venta debe tener al menos un producto." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var defaultUser = await _context.Usuarios.FirstOrDefaultAsync();
                int userId = defaultUser?.IdUsuario ?? 1;

                // 1. Crear la Venta
                var venta = new Venta
                {
                    IdCliente = req.IdCliente,
                    Fecha = DateTime.UtcNow,
                    TipoPago = req.TipoPago,
                    Estado = req.TipoPago == "Credito" ? "Pendiente" : "Completada",
                    IdUsuario = userId,
                    Observaciones = req.Observaciones,
                    Eliminado = false
                };

                if (req.TipoPago == "Credito")
                {
                    venta.FechaVencimiento = DateTime.UtcNow.AddDays(15); // Por defecto 15 días
                    
                    if (req.IdCliente.HasValue)
                    {
                        var cliente = await _context.Clientes.FindAsync(req.IdCliente.Value);
                        if (cliente != null)
                        {
                            decimal saldoNuevo = cliente.SaldoActual + req.Detalles.Sum(d => (d.Cantidad * d.PrecioUnitario) - d.Descuento);
                            if (cliente.LimiteCredito > 0 && saldoNuevo > cliente.LimiteCredito)
                                throw new Exception($"El cliente ha excedido su límite de crédito. Límite: C$ {cliente.LimiteCredito}");
                            
                            cliente.SaldoActual = saldoNuevo;
                        }
                    }
                }

                // Calcular totales
                venta.Total = req.Detalles.Sum(d => (d.Cantidad * d.PrecioUnitario) - d.Descuento);
                venta.DescuentoMonto = req.Detalles.Sum(d => d.Descuento);
                
                // Generar Numero Factura (Simulado o desde DB)
                // En un sistema real usaríamos una secuencia
                var ultimaVentaId = await _context.Ventas.OrderByDescending(v => v.IdVenta).Select(v => v.IdVenta).FirstOrDefaultAsync();
                venta.NumeroFactura = $"FAC-{(ultimaVentaId + 1):D6}";

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync(); // Guardar para tener IdVenta

                // 2. Agregar Detalles y Actualizar Stock
                foreach (var det in req.Detalles)
                {
                    var detalleVenta = new DetalleVenta
                    {
                        IdVenta = venta.IdVenta,
                        IdProducto = det.IdProducto,
                        IdPresentacion = det.IdPresentacion,
                        Cantidad = det.Cantidad,
                        CantidadBase = det.CantidadBase,
                        PrecioUnitario = det.PrecioUnitario,
                        DescuentoMonto = det.Descuento
                    };
                    _context.DetalleVentas.Add(detalleVenta);

                    // Actualizar Inventario
                    var producto = await _context.Productos.FindAsync(det.IdProducto);
                    if (producto == null) throw new Exception($"Producto {det.IdProducto} no encontrado.");
                    
                    if (producto.StockBase < det.CantidadBase)
                        throw new Exception($"Stock insuficiente para {producto.Nombre}. Disponible: {producto.StockBase}");

                    producto.StockBase -= det.CantidadBase;

                    // Registrar Movimiento
                    var movimiento = new MovimientoInventario
                    {
                        IdProducto = det.IdProducto,
                        TipoMovimiento = "Salida",
                        CantidadBase = det.CantidadBase,
                        Fecha = DateTime.UtcNow,
                        Observacion = $"Venta #{venta.IdVenta} - Factura {venta.NumeroFactura}",
                        IdUsuario = userId,
                        IdReferencia = (int)venta.IdVenta,
                        TipoReferencia = "Venta"
                    };
                    _context.MovimientosInventario.Add(movimiento);
                }

                // 3. Registrar Pago Inicial si existe
                if (req.PagoInicial != null && req.PagoInicial.Monto > 0)
                {
                    var pago = new PagoVenta
                    {
                        IdVenta = venta.IdVenta,
                        Monto = req.PagoInicial.Monto,
                        MetodoPago = req.PagoInicial.Metodo,
                        NumeroComprobante = req.PagoInicial.Comprobante,
                        FechaPago = DateTime.UtcNow,
                        IdUsuario = userId
                    };
                    _context.PagosVenta.Add(pago);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Venta realizada con éxito.", id = venta.IdVenta, factura = venta.NumeroFactura });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error al procesar la venta: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarPago([FromBody] PagoRequest req)
        {
            var venta = await _context.Ventas.FindAsync(req.IdVenta);
            if (venta == null) return Json(new { success = false, message = "Venta no encontrada." });

            var defaultUser = await _context.Usuarios.FirstOrDefaultAsync();
            int userId = defaultUser?.IdUsuario ?? 1;

            var pago = new PagoVenta
            {
                IdVenta = req.IdVenta,
                Monto = req.Monto,
                MetodoPago = req.Metodo ?? "Efectivo",
                NumeroComprobante = req.Comprobante,
                FechaPago = DateTime.UtcNow,
                IdUsuario = userId
            };

            _context.PagosVenta.Add(pago);
            
            // Actualizar Saldo Cliente
            if (venta.IdCliente.HasValue)
            {
                var cliente = await _context.Clientes.FindAsync(venta.IdCliente.Value);
                if (cliente != null)
                {
                    cliente.SaldoActual -= req.Monto;
                }
            }

            // Si el total ya está cubierto, marcar como completada
            var totalPagado = await _context.PagosVenta.Where(p => p.IdVenta == req.IdVenta).SumAsync(p => p.Monto) + req.Monto;
            if (totalPagado >= venta.Total)
            {
                venta.Estado = "Completada";
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Pago registrado correctamente." });
        }

        [HttpPost]
        public async Task<IActionResult> Anular(long id, string motivo)
        {
            var venta = await _context.Ventas
                .Include(v => v.DetalleVentas)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

            if (venta == null) return Json(new { success = false, message = "Venta no encontrada." });
            if (venta.Estado == "Anulada") return Json(new { success = false, message = "Esta venta ya ha sido anulada." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var defaultUser = await _context.Usuarios.FirstOrDefaultAsync();
                int userId = defaultUser?.IdUsuario ?? 1;

                venta.Estado = "Anulada";
                venta.FechaAnulacion = DateTime.UtcNow;
                venta.UsuarioAnulacion = userId;
                venta.MotivoAnulacion = motivo;

                // Restaurar Saldo Cliente si era crédito
                if (venta.TipoPago == "Credito" && venta.IdCliente.HasValue)
                {
                    var cliente = await _context.Clientes.FindAsync(venta.IdCliente.Value);
                    if (cliente != null)
                    {
                        cliente.SaldoActual -= venta.Total;
                    }
                }

                // Devolver Stock
                foreach (var det in venta.DetalleVentas)
                {
                    var producto = await _context.Productos.FindAsync(det.IdProducto);
                    if (producto != null)
                    {
                        producto.StockBase += det.CantidadBase;
                        
                        // Registrar Movimiento de Ajuste por Anulación
                        var movimiento = new MovimientoInventario
                        {
                            IdProducto = det.IdProducto,
                            TipoMovimiento = "Entrada",
                            CantidadBase = det.CantidadBase,
                            Fecha = DateTime.UtcNow,
                            Observacion = $"Anulación de Venta #{venta.IdVenta} - Factura {venta.NumeroFactura}",
                            IdUsuario = userId,
                            IdReferencia = (int)venta.IdVenta,
                            TipoReferencia = "AnulacionVenta"
                        };
                        _context.MovimientosInventario.Add(movimiento);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { success = true, message = "Venta anulada y stock retornado a inventario." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error al anular la venta: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CrearCliente([FromBody] Cliente cliente)
        {
            if (!ModelState.IsValid)
            {
                 var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                 return Json(new { success = false, message = "Datos inválidos", errors });
            }

            try
            {
                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
                return Json(new { success = true, id = cliente.IdCliente, nombre = cliente.Nombre });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public class PagoRequest {
            public long IdVenta { get; set; }
            public decimal Monto { get; set; }
            public string? Metodo { get; set; }
            public string? Comprobante { get; set; }
        }
    }
}

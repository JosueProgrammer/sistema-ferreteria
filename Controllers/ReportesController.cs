using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Ferreteria.Data;
using Sistema_Ferreteria.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

using Microsoft.AspNetCore.Authorization;
using Sistema_Ferreteria.Filters;
using Sistema_Ferreteria.Models.Ventas;
using Sistema_Ferreteria.Services.ReportesVentas;

namespace Sistema_Ferreteria.Controllers
{
    [Authorize]
    [Permiso("REPORTES_VER")]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var primerDiaMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var dashboard = new DashboardReporteVM
            {
                TotalVentasHoy = await _context.Ventas
                    .Where(v => v.Fecha >= hoy && !v.Eliminado && v.Estado != "Anulada")
                    .SumAsync(v => v.Total),
                
                CantidadVentasHoy = await _context.Ventas
                    .CountAsync(v => v.Fecha >= hoy && !v.Eliminado && v.Estado != "Anulada"),

                ProductosBajoStock = await _context.Productos
                    .CountAsync(p => p.StockBase <= p.StockMinimo && !p.Eliminado && p.Estado),

                // Ganancia Estimada del Mes
                GananciaEstimadaMes = await _context.DetalleVentas
                    .Include(d => d.Venta)
                    .Include(d => d.Producto)
                    .Where(d => d.Venta.Fecha >= primerDiaMes && !d.Venta.Eliminado && d.Venta.Estado != "Anulada")
                    .SumAsync(d => (d.PrecioUnitario - (d.Producto.PrecioBaseCompra ?? 0)) * d.CantidadBase),

                // Ventas últimos 7 días
                VentasUltimosDias = (await _context.Ventas
                    .Where(v => v.Fecha >= hoy.AddDays(-7) && !v.Eliminado && v.Estado != "Anulada")
                    .GroupBy(v => v.Fecha.Date)
                    .Select(g => new 
                    {
                        Fecha = g.Key,
                        Total = g.Sum(v => v.Total)
                    })
                    .OrderBy(x => x.Fecha)
                    .ToListAsync())
                    .Select(x => new VentaGraficoVM
                    {
                        Fecha = x.Fecha.ToString("dd/MM"),
                        Total = x.Total
                    }).ToList(),

                // Top 10 Productos
                ProductosMasVendidos = await _context.DetalleVentas
                    .Include(d => d.Venta)
                    .Include(d => d.Producto)
                    .Where(d => !d.Venta.Eliminado && d.Venta.Estado != "Anulada")
                    .GroupBy(d => d.Producto.Nombre)
                    .Select(g => new TopProductoVM
                    {
                        Nombre = g.Key,
                        Cantidad = g.Sum(d => d.CantidadBase),
                        TotalIngresos = g.Sum(d => d.PrecioUnitario * d.CantidadBase)
                    })
                    .OrderByDescending(x => x.Cantidad)
                    .Take(10)
                    .ToListAsync()
            };

            return View(dashboard);
        }

        public async Task<IActionResult> Ventas(DateTime? inicio, DateTime? fin, int? page, int? pageSize)
        {
            var range = ReportesVentasDateRange.TryNormalize(inicio, fin);
            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? ReportesVentasLimits.PageSizeDefault, 1, ReportesVentasLimits.PageSizeMax);

            string? tempAlert = null;
            if (TempData.TryGetValue("ReporteVentasError", out var tempObj) && tempObj is string tempStr && !string.IsNullOrWhiteSpace(tempStr))
                tempAlert = tempStr;

            if (!range.IsValid)
            {
                return View(new VentasReporteVM
                {
                    FechaInicio = range.FechaInicio,
                    FechaFin = range.FechaFin,
                    Page = p,
                    PageSize = ps,
                    ValidationAlert = range.ErrorMessage,
                    Detalles = new List<VentaDetalleReporteVM>()
                });
            }

            var q = VentasReporteQueryable(range.FechaInicio, range.FechaFin);

            var totalVendido = await q.Where(v => v.Estado != "Anulada").SumAsync(v => (decimal?)v.Total) ?? 0m;
            var totalFacturas = await q.CountAsync();
            var totalPages = totalFacturas == 0 ? 0 : (int)Math.Ceiling(totalFacturas / (double)ps);
            if (totalPages > 0 && p > totalPages)
                p = totalPages;

            var detalles = await q
                .OrderByDescending(v => v.Fecha)
                .ThenByDescending(v => v.IdVenta)
                .Skip((p - 1) * ps)
                .Take(ps)
                .Select(v => new VentaDetalleReporteVM
                {
                    IdVenta = v.IdVenta,
                    NumeroFactura = v.NumeroFactura ?? v.IdVenta.ToString(),
                    Fecha = v.Fecha,
                    Cliente = v.Cliente != null ? v.Cliente.Nombre : "Consumidor Final",
                    Total = v.Total,
                    Estado = v.Estado
                })
                .ToListAsync();

            var model = new VentasReporteVM
            {
                FechaInicio = range.FechaInicio,
                FechaFin = range.FechaFin,
                TotalVendido = totalVendido,
                TotalFacturas = totalFacturas,
                Page = p,
                PageSize = ps,
                TotalRegistros = totalFacturas,
                ValidationAlert = tempAlert,
                Detalles = detalles
            };

            return View(model);
        }

        public async Task<IActionResult> InventarioBajo()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.StockBase <= p.StockMinimo && !p.Eliminado && p.Estado)
                .OrderBy(p => p.StockBase)
                .Select(p => new InventarioBajoVM
                {
                    IdProducto = p.IdProducto,
                    Nombre = p.Nombre,
                    Codigo = p.Codigo,
                    StockActual = p.StockBase,
                    StockMinimo = p.StockMinimo,
                    Categoria = p.Categoria != null ? p.Categoria.Nombre : "Sin Categoría"
                })
                .ToListAsync();

            return View(productos);
        }

        public async Task<IActionResult> ExportarExcelVentas(DateTime? inicio, DateTime? fin)
        {
            var range = ReportesVentasDateRange.TryNormalize(inicio, fin);
            if (!range.IsValid)
            {
                TempData["ReporteVentasError"] = range.ErrorMessage;
                return RedirectToVentasConMensaje(range.FechaInicio, range.FechaFin);
            }

            var q = VentasReporteQueryable(range.FechaInicio, range.FechaFin);
            var totalFilas = await q.CountAsync();
            if (totalFilas > ReportesVentasLimits.MaxExcelExportRows)
            {
                TempData["ReporteVentasError"] =
                    $"Hay {totalFilas:N0} ventas en el periodo; el máximo para exportar a Excel es {ReportesVentasLimits.MaxExcelExportRows:N0}. Acorte el rango de fechas.";
                return RedirectToVentasConMensaje(range.FechaInicio, range.FechaFin);
            }

            // ClosedXML mantiene el libro en memoria; el tope de filas y lotes desde BD limitan picos.
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ventas");
            var currentRow = 1;

            worksheet.Cell(currentRow, 1).Value = "Reporte de Ventas";
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = $"Periodo: {range.FechaInicio.ToLocalTime():dd/MM/yyyy} - {range.FechaFin.ToLocalTime():dd/MM/yyyy}";
            currentRow += 2;

            worksheet.Cell(currentRow, 1).Value = "Factura";
            worksheet.Cell(currentRow, 2).Value = "Fecha";
            worksheet.Cell(currentRow, 3).Value = "Cliente";
            worksheet.Cell(currentRow, 4).Value = "Monto";
            worksheet.Cell(currentRow, 5).Value = "Estado";

            var headerRange = worksheet.Range(currentRow, 1, currentRow, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            DateTime? cursorFecha = null;
            long? cursorId = null;
            var escritas = 0;

            while (escritas < totalFilas)
            {
                var batchQuery = q
                    .OrderByDescending(v => v.Fecha)
                    .ThenByDescending(v => v.IdVenta);

                IQueryable<Venta> filtered = batchQuery;
                if (cursorFecha.HasValue && cursorId.HasValue)
                {
                    var cf = cursorFecha.Value;
                    var cid = cursorId.Value;
                    filtered = batchQuery.Where(v =>
                        v.Fecha < cf || (v.Fecha == cf && v.IdVenta < cid));
                }

                var take = Math.Min(ReportesVentasLimits.ExcelDbBatchSize, totalFilas - escritas);
                var batch = await filtered
                    .Take(take)
                    .Select(v => new VentaExportRow
                    {
                        IdVenta = v.IdVenta,
                        NumeroFactura = v.NumeroFactura,
                        Fecha = v.Fecha,
                        ClienteNombre = v.Cliente != null ? v.Cliente.Nombre : null,
                        Total = v.Total,
                        Estado = v.Estado
                    })
                    .ToListAsync();

                if (batch.Count == 0)
                    break;

                foreach (var v in batch)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = v.NumeroFactura ?? v.IdVenta.ToString();
                    worksheet.Cell(currentRow, 2).Value = v.Fecha.ToLocalTime();
                    worksheet.Cell(currentRow, 3).Value = v.ClienteNombre ?? "Consumidor Final";
                    worksheet.Cell(currentRow, 4).Value = v.Total;
                    worksheet.Cell(currentRow, 5).Value = v.Estado;
                    escritas++;
                }

                var ult = batch[^1];
                cursorFecha = ult.Fecha;
                cursorId = ult.IdVenta;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Ventas_{range.FechaInicio:yyyyMMdd}_{range.FechaFin:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportarPdfVentas(DateTime? inicio, DateTime? fin)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var range = ReportesVentasDateRange.TryNormalize(inicio, fin);
            if (!range.IsValid)
            {
                TempData["ReporteVentasError"] = range.ErrorMessage;
                return RedirectToVentasConMensaje(range.FechaInicio, range.FechaFin);
            }

            var q = VentasReporteQueryable(range.FechaInicio, range.FechaFin);
            var totalFilas = await q.CountAsync();
            if (totalFilas > ReportesVentasLimits.MaxPdfExportRows)
            {
                TempData["ReporteVentasError"] =
                    $"Hay {totalFilas:N0} ventas en el periodo; el máximo para PDF es {ReportesVentasLimits.MaxPdfExportRows:N0}. Use Excel o acorte el rango.";
                return RedirectToVentasConMensaje(range.FechaInicio, range.FechaFin);
            }

            var totalVendido = await q.Where(v => v.Estado != "Anulada").SumAsync(v => (decimal?)v.Total) ?? 0m;

            var ventasPdf = await q
                .OrderByDescending(v => v.Fecha)
                .ThenByDescending(v => v.IdVenta)
                .Select(v => new VentaExportRow
                {
                    IdVenta = v.IdVenta,
                    NumeroFactura = v.NumeroFactura,
                    Fecha = v.Fecha,
                    ClienteNombre = v.Cliente != null ? v.Cliente.Nombre : null,
                    Total = v.Total,
                    Estado = v.Estado
                })
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Ferretería ERP").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Reporte de Ventas").FontSize(14);
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}");
                            col.Item().Text($"Periodo: {range.FechaInicio:dd/MM/yyyy} - {range.FechaFin:dd/MM/yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                    {
                        x.Spacing(5);

                        x.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Factura");
                                header.Cell().Element(CellStyle).Text("Fecha");
                                header.Cell().Element(CellStyle).Text("Cliente");
                                header.Cell().Element(CellStyle).AlignRight().Text("Monto");
                                header.Cell().Element(CellStyle).AlignCenter().Text("Estado");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var v in ventasPdf)
                            {
                                table.Cell().Element(RowStyle).Text(v.NumeroFactura ?? v.IdVenta.ToString());
                                table.Cell().Element(RowStyle).Text(v.Fecha.ToLocalTime().ToString("dd/MM/yyyy"));
                                table.Cell().Element(RowStyle).Text(v.ClienteNombre ?? "Consumidor Final");
                                table.Cell().Element(RowStyle).AlignRight().Text($"C$ {v.Total:N2}");
                                table.Cell().Element(RowStyle).AlignCenter().Text(v.Estado);

                                static IContainer RowStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                }
                            }
                        });

                        x.Item().AlignRight().Text(text =>
                        {
                            text.Span("Total Vendido: ").SemiBold();
                            text.Span($"C$ {totalVendido:N2}").FontSize(14).SemiBold().FontColor(Colors.Green.Medium);
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return File(stream.ToArray(), "application/pdf",
                $"Ventas_{range.FechaInicio:yyyyMMdd}_{range.FechaFin:yyyyMMdd}.pdf");
        }

        private IQueryable<Venta> VentasReporteQueryable(DateTime fechaInicioUtc, DateTime fechaFinUtc) =>
            _context.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= fechaInicioUtc && v.Fecha <= fechaFinUtc && !v.Eliminado);

        private IActionResult RedirectToVentasConMensaje(DateTime fechaInicioUtc, DateTime fechaFinUtc) =>
            RedirectToAction(nameof(Ventas), new
            {
                inicio = fechaInicioUtc.ToString("yyyy-MM-dd"),
                fin = fechaFinUtc.ToString("yyyy-MM-dd")
            });
    }
}

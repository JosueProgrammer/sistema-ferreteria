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

        public async Task<IActionResult> Ventas(DateTime? inicio, DateTime? fin)
        {
            var fechaInicio = inicio.HasValue ? DateTime.SpecifyKind(inicio.Value, DateTimeKind.Utc) : DateTime.UtcNow.Date.AddDays(-30);
            var fechaFin = fin.HasValue ? DateTime.SpecifyKind(fin.Value, DateTimeKind.Utc) : DateTime.UtcNow;

            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Where(v => v.Fecha >= fechaInicio && v.Fecha <= fechaFin && !v.Eliminado)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            var model = new VentasReporteVM
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                TotalVendido = ventas.Where(v => v.Estado != "Anulada").Sum(v => v.Total),
                TotalFacturas = ventas.Count,
                Detalles = ventas.Select(v => new VentaDetalleReporteVM
                {
                    IdVenta = v.IdVenta,
                    NumeroFactura = v.NumeroFactura ?? v.IdVenta.ToString(),
                    Fecha = v.Fecha,
                    Cliente = v.Cliente?.Nombre ?? "Consumidor Final",
                    Total = v.Total,
                    Estado = v.Estado
                }).ToList()
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
            var fechaInicio = inicio.HasValue ? DateTime.SpecifyKind(inicio.Value, DateTimeKind.Utc) : DateTime.UtcNow.Date.AddDays(-30);
            var fechaFin = fin.HasValue ? DateTime.SpecifyKind(fin.Value, DateTimeKind.Utc) : DateTime.UtcNow;

            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Where(v => v.Fecha >= fechaInicio && v.Fecha <= fechaFin && !v.Eliminado)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Ventas");
                var currentRow = 1;

                // Título
                worksheet.Cell(currentRow, 1).Value = "Reporte de Ventas";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = $"Periodo: {fechaInicio.ToLocalTime():dd/MM/yyyy} - {fechaFin.ToLocalTime():dd/MM/yyyy}";
                currentRow += 2;

                // Cabeceras
                worksheet.Cell(currentRow, 1).Value = "Factura";
                worksheet.Cell(currentRow, 2).Value = "Fecha";
                worksheet.Cell(currentRow, 3).Value = "Cliente";
                worksheet.Cell(currentRow, 4).Value = "Monto";
                worksheet.Cell(currentRow, 5).Value = "Estado";

                var headerRange = worksheet.Range(currentRow, 1, currentRow, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                foreach (var v in ventas)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = v.NumeroFactura ?? v.IdVenta.ToString();
                    worksheet.Cell(currentRow, 2).Value = v.Fecha.ToLocalTime();
                    worksheet.Cell(currentRow, 3).Value = v.Cliente?.Nombre ?? "Consumidor Final";
                    worksheet.Cell(currentRow, 4).Value = v.Total;
                    worksheet.Cell(currentRow, 5).Value = v.Estado;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Ventas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.xlsx");
                }
            }
        }

        public async Task<IActionResult> ExportarPdfVentas(DateTime? inicio, DateTime? fin)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var fechaInicio = inicio.HasValue ? DateTime.SpecifyKind(inicio.Value, DateTimeKind.Utc) : DateTime.UtcNow.Date.AddDays(-30);
            var fechaFin = fin.HasValue ? DateTime.SpecifyKind(fin.Value, DateTimeKind.Utc) : DateTime.UtcNow;

            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Where(v => v.Fecha >= fechaInicio && v.Fecha <= fechaFin && !v.Eliminado)
                .OrderByDescending(v => v.Fecha)
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
                            col.Item().Text($"Periodo: {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}");
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

                            foreach (var v in ventas)
                            {
                                table.Cell().Element(RowStyle).Text(v.NumeroFactura ?? v.IdVenta.ToString());
                                table.Cell().Element(RowStyle).Text(v.Fecha.ToLocalTime().ToString("dd/MM/yyyy"));
                                table.Cell().Element(RowStyle).Text(v.Cliente?.Nombre ?? "Consumidor Final");
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
                            text.Span($"C$ {ventas.Where(v => v.Estado != "Anulada").Sum(v => v.Total):N2}").FontSize(14).SemiBold().FontColor(Colors.Green.Medium);
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
                });
            });

            using (var stream = new MemoryStream())
            {
                document.GeneratePdf(stream);
                return File(stream.ToArray(), "application/pdf", $"Ventas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.pdf");
            }
        }
    }
}

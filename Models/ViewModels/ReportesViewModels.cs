using System;
using System.Collections.Generic;

namespace Sistema_Ferreteria.Models.ViewModels
{
    public class DashboardReporteVM
    {
        public decimal TotalVentasHoy { get; set; }
        public int CantidadVentasHoy { get; set; }
        public decimal GananciaEstimadaMes { get; set; }
        public int ProductosBajoStock { get; set; }
        public List<VentaGraficoVM> VentasUltimosDias { get; set; } = new();
        public List<TopProductoVM> ProductosMasVendidos { get; set; } = new();
    }

    public class VentaGraficoVM
    {
        public string Fecha { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public class VentasReporteVM
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal TotalVendido { get; set; }
        public int TotalFacturas { get; set; }
        public List<VentaDetalleReporteVM> Detalles { get; set; } = new();
    }

    public class VentaDetalleReporteVM
    {
        public long IdVenta { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public class TopProductoVM
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal TotalIngresos { get; set; }
    }

    public class InventarioBajoVM
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public string Categoria { get; set; } = string.Empty;
    }
}

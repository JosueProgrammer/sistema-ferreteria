namespace Sistema_Ferreteria.Services.ReportesVentas;

/// <summary>Fila proyectada para exportaciones (Excel/PDF) sin materializar la entidad completa.</summary>
public sealed class VentaExportRow
{
    public long IdVenta { get; set; }
    public string? NumeroFactura { get; set; }
    public DateTime Fecha { get; set; }
    public string? ClienteNombre { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
}

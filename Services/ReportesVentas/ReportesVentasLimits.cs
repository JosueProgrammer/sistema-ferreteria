namespace Sistema_Ferreteria.Services.ReportesVentas;

/// <summary>
/// Límites de reporte de ventas (pantalla y exportaciones). ClosedXML mantiene el libro en memoria; el tope de filas protege la RAM.
/// </summary>
public static class ReportesVentasLimits
{
    public const int MaxReportCalendarDays = 90;
    public const int PageSizeDefault = 50;
    public const int PageSizeMax = 200;
    public const int MaxExcelExportRows = 10_000;
    public const int MaxPdfExportRows = 2_000;
    public const int ExcelDbBatchSize = 1_000;
}

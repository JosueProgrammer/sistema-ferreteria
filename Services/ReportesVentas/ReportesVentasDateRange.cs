namespace Sistema_Ferreteria.Services.ReportesVentas;

public readonly record struct ReportesVentasDateRangeResult(
    bool IsValid,
    string? ErrorMessage,
    DateTime FechaInicio,
    DateTime FechaFin);

/// <summary>
/// Normaliza y valida el rango de fechas para reportes de ventas (UTC).
/// </summary>
public static class ReportesVentasDateRange
{
    /// <summary>
    /// Si <paramref name="fin"/> es null, el fin es <see cref="DateTime.UtcNow"/> (instante actual).
    /// Si hay fecha de fin explícita, se incluye todo ese día civil en UTC.
    /// </summary>
    public static ReportesVentasDateRangeResult TryNormalize(DateTime? inicio, DateTime? fin)
    {
        var fechaInicio = inicio.HasValue
            ? DateTime.SpecifyKind(inicio.Value.Date, DateTimeKind.Utc)
            : DateTime.UtcNow.Date.AddDays(-30);

        var fechaFin = fin.HasValue
            ? DateTime.SpecifyKind(fin.Value.Date, DateTimeKind.Utc).AddDays(1).AddTicks(-1)
            : DateTime.UtcNow;

        if (fechaFin < fechaInicio)
            return new ReportesVentasDateRangeResult(false, "La fecha fin no puede ser anterior a la fecha inicio.", fechaInicio, fechaFin);

        var inclusiveCalendarDays = (fechaFin.Date - fechaInicio.Date).Days + 1;
        if (inclusiveCalendarDays > ReportesVentasLimits.MaxReportCalendarDays)
        {
            return new ReportesVentasDateRangeResult(
                false,
                $"El rango no puede superar {ReportesVentasLimits.MaxReportCalendarDays} días calendario. Acorte las fechas.",
                fechaInicio,
                fechaFin);
        }

        return new ReportesVentasDateRangeResult(true, null, fechaInicio, fechaFin);
    }
}

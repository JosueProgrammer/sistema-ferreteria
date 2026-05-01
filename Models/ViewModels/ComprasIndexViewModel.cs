using Sistema_Ferreteria.Models.Compras;

namespace Sistema_Ferreteria.Models.ViewModels;

public class ComprasIndexViewModel
{
    public IReadOnlyList<Compra> Compras { get; init; } = Array.Empty<Compra>();

    public int PaginaActual { get; init; } = 1;

    public int TamanoPagina { get; init; } = 15;

    public int TotalRegistros { get; init; }

    public int? IdProveedorFiltro { get; init; }

    /// <summary>Búsqueda por número de pedido (exacto si es numérico) o texto en número de factura.</summary>
    public string? Buscar { get; init; }

    /// <summary>Estado del pedido: Pendiente, Recibida, o vacío (todos).</summary>
    public string? EstadoFiltro { get; init; }

    /// <summary>Situación de pago: pagado, parcial, pendiente, o vacío (todos).</summary>
    public string? PagoFiltro { get; init; }

    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    public bool TieneFiltrosActivos =>
        IdProveedorFiltro.HasValue
        || !string.IsNullOrWhiteSpace(Buscar)
        || !string.IsNullOrEmpty(EstadoFiltro)
        || !string.IsNullOrEmpty(PagoFiltro)
        || FechaDesde.HasValue
        || FechaHasta.HasValue;

    public int TotalPaginas =>
        TamanoPagina <= 0 || TotalRegistros == 0
            ? 0
            : (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina);

    public int RegistroInicio =>
        TotalRegistros == 0 ? 0 : (PaginaActual - 1) * TamanoPagina + 1;

    public int RegistroFin => Math.Min(PaginaActual * TamanoPagina, TotalRegistros);
}

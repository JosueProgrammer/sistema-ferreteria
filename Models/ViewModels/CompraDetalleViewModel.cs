using Sistema_Ferreteria.Models.Compras;

namespace Sistema_Ferreteria.Models.ViewModels;

public record CompraDetallePresentacionOpcion(int IdPresentacion, string Nombre);

public class CompraDetalleViewModel
{
    public Compra Compra { get; init; } = null!;

    public IReadOnlyList<DetalleCompra> Lineas { get; init; } = Array.Empty<DetalleCompra>();

    public int PaginaLineas { get; init; } = 1;

    public int TamanoLineas { get; init; } = 15;

    public int TotalLineas { get; init; }

    /// <summary>True si alguna línea del resultado filtrado tiene descuento (para mostrar columna Desc.).</summary>
    public bool TieneDescuentoEnFiltrado { get; init; }

    public string? Buscar { get; init; }

    public int? IdPresentacionFiltro { get; init; }

    public bool SoloConDescuento { get; init; }

    public IReadOnlyList<CompraDetallePresentacionOpcion> PresentacionesDelPedido { get; init; } =
        Array.Empty<CompraDetallePresentacionOpcion>();

    public int TotalPaginasLineas =>
        TamanoLineas <= 0 || TotalLineas == 0
            ? 0
            : (int)Math.Ceiling(TotalLineas / (double)TamanoLineas);

    public int RegistroInicioLineas =>
        TotalLineas == 0 ? 0 : (PaginaLineas - 1) * TamanoLineas + 1;

    public int RegistroFinLineas => Math.Min(PaginaLineas * TamanoLineas, TotalLineas);
}

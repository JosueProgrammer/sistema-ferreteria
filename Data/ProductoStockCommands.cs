using Microsoft.EntityFrameworkCore;

namespace Sistema_Ferreteria.Data;

/// <summary>
/// Actualizaciones atómicas de <see cref="Models.Inventario.Producto.StockBase"/> vía SQL.
/// Incluye siempre <c>TenantId</c> porque <see cref="DatabaseFacade.ExecuteSqlInterpolatedAsync"/> no aplica el filtro global de EF.
/// </summary>
public static class ProductoStockCommands
{
    public static Task<int> DecrementStockIfAvailableAsync(
        ApplicationDbContext context,
        int idProducto,
        decimal cantidadBase,
        CancellationToken cancellationToken = default)
    {
        var tenantId = context.TenantId;
        return context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Productos""
            SET ""StockBase"" = ""StockBase"" - {cantidadBase}
            WHERE ""IdProducto"" = {idProducto}
              AND ""TenantId"" = {tenantId}
              AND ""StockBase"" >= {cantidadBase}", cancellationToken);
    }

    public static Task<int> IncrementStockAsync(
        ApplicationDbContext context,
        int idProducto,
        decimal cantidadBase,
        CancellationToken cancellationToken = default)
    {
        var tenantId = context.TenantId;
        return context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Productos""
            SET ""StockBase"" = ""StockBase"" + {cantidadBase}
            WHERE ""IdProducto"" = {idProducto}
              AND ""TenantId"" = {tenantId}", cancellationToken);
    }

    public static Task<int> SetStockBaseAsync(
        ApplicationDbContext context,
        int idProducto,
        decimal nuevoStockBase,
        CancellationToken cancellationToken = default)
    {
        var tenantId = context.TenantId;
        return context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Productos""
            SET ""StockBase"" = {nuevoStockBase}
            WHERE ""IdProducto"" = {idProducto}
              AND ""TenantId"" = {tenantId}", cancellationToken);
    }
}

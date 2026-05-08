using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema_Ferreteria.Migrations
{
    /// <inheritdoc />
    public partial class AddVentasReportIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Ventas_TenantId_Eliminado_Fecha_IdVenta",
                table: "Ventas",
                columns: new[] { "TenantId", "Eliminado", "Fecha", "IdVenta" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ventas_TenantId_Eliminado_Fecha_IdVenta",
                table: "Ventas");
        }
    }
}

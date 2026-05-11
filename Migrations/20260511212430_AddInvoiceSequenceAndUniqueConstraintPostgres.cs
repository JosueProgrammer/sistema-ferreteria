using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema_Ferreteria.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceSequenceAndUniqueConstraintPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "VentaNumeroFacturaSequence");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroFactura",
                table: "Ventas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValueSql: "'FAC-' || lpad(nextval('\"VentaNumeroFacturaSequence\"')::text, 6, '0')",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_TenantId_NumeroFactura",
                table: "Ventas",
                columns: new[] { "TenantId", "NumeroFactura" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ventas_TenantId_NumeroFactura",
                table: "Ventas");

            migrationBuilder.DropSequence(
                name: "VentaNumeroFacturaSequence");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroFactura",
                table: "Ventas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldDefaultValueSql: "'FAC-' || lpad(nextval('\"VentaNumeroFacturaSequence\"')::text, 6, '0')");
        }
    }
}

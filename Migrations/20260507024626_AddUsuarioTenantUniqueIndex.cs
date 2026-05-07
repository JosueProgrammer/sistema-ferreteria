using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema_Ferreteria.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioTenantUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Usuario",
                table: "Usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TenantId_Usuario",
                table: "Usuarios",
                columns: new[] { "TenantId", "Usuario" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_TenantId_Usuario",
                table: "Usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Usuario",
                table: "Usuarios",
                column: "Usuario",
                unique: true);
        }
    }
}

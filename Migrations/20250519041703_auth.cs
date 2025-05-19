using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class auth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "products",
                type: "numeric(18)",
                precision: 18,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,0)",
                oldPrecision: 18,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "products",
                type: "numeric(18,0)",
                precision: 18,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18)",
                oldPrecision: 18,
                oldNullable: true);
        }
    }
}

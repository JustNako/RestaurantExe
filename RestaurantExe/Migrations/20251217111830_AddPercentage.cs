using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantExe.Migrations
{
    /// <inheritdoc />
    public partial class AddPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalPrice",
                table: "Orders",
                newName: "ServicePercent");

            migrationBuilder.AddColumn<decimal>(
                name: "ItemsTotal",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemsTotal",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "ServicePercent",
                table: "Orders",
                newName: "TotalPrice");
        }
    }
}

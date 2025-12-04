using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentBazaar.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPricePerDayToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PricePerDay",
                table: "Products",
                type: "decimal(18, 2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerDay",
                table: "Products");
        }
    }
}


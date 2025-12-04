using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentBazaar.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsForRentToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsForRent",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsForRent",
                table: "Products");
        }
    }
}


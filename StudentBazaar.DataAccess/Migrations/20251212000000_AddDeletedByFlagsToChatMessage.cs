using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentBazaar.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedByFlagsToChatMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeletedByReceiver",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DeletedBySender",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedByReceiver",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "DeletedBySender",
                table: "ChatMessages");
        }
    }
}


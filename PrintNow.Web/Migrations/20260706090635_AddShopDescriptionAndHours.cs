using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintNow.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddShopDescriptionAndHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Shops",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingHoursText",
                table: "Shops",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "OperatingHoursText",
                table: "Shops");
        }
    }
}

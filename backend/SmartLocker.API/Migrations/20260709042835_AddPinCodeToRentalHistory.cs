using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLocker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPinCodeToRentalHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PinCode",
                table: "RentalHistories",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PinCode",
                table: "RentalHistories");
        }
    }
}

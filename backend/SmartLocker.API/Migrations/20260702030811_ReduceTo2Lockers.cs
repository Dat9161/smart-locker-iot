using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartLocker.API.Migrations
{
    /// <inheritdoc />
    public partial class ReduceTo2Lockers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Lockers",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Lockers",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Lockers",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Lockers",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.UpdateData(
                table: "Lockers",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Tủ 1");

            migrationBuilder.UpdateData(
                table: "Lockers",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Tủ 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Lockers",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Tủ A1");

            migrationBuilder.UpdateData(
                table: "Lockers",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Tủ A2");

            migrationBuilder.InsertData(
                table: "Lockers",
                columns: new[] { "Id", "Name", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 3, "Tủ A3", "available", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Tủ B1", "available", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "Tủ B2", "available", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "Tủ B3", "available", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }
    }
}

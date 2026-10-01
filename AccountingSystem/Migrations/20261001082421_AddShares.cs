using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 12, 54, 18, 192, DateTimeKind.Local).AddTicks(6573));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 12, 54, 18, 192, DateTimeKind.Local).AddTicks(5224));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 12, 54, 18, 193, DateTimeKind.Local).AddTicks(685));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 12, 54, 18, 193, DateTimeKind.Local).AddTicks(694));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEA67tiNTqXqIoeEt5N5bn1E3aUI9QDaFKaik9GIWqbASKrgJD76vLEJbe9LwRoYDjA==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 12, 54, 18, 190, DateTimeKind.Local).AddTicks(3880));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 12, 54, 18, 192, DateTimeKind.Local).AddTicks(1538));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 11, 11, 7, 980, DateTimeKind.Local).AddTicks(9017));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 11, 11, 7, 980, DateTimeKind.Local).AddTicks(4716));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 11, 11, 7, 982, DateTimeKind.Local).AddTicks(2129));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 11, 11, 7, 982, DateTimeKind.Local).AddTicks(2202));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKH7iEiZD13cyB4KJLQNMy02l774dgvvMw1LibS9Ch+gEKiAXIIXO/TIH9CP+SOskg==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 11, 11, 7, 975, DateTimeKind.Local).AddTicks(2846));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 1, 11, 11, 7, 979, DateTimeKind.Local).AddTicks(3021));
        }
    }
}

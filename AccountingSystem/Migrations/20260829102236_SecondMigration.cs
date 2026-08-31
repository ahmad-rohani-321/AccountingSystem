using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class SecondMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 29, 14, 52, 33, 870, DateTimeKind.Local).AddTicks(2870));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 29, 14, 52, 33, 870, DateTimeKind.Local).AddTicks(1465));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 29, 14, 52, 33, 870, DateTimeKind.Local).AddTicks(7096));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 8, 29, 14, 52, 33, 870, DateTimeKind.Local).AddTicks(7103));

            migrationBuilder.UpdateData(
                table: "StockTransactionTypes",
                keyColumn: "ID",
                keyValue: 10,
                column: "Name",
                value: "خرید واپسي");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFlx0XKeLPLXtHy1HmUYpeZaPtiMP33lkQ7zWwxZL5wQyLXr8aI5N5zEO0PKswrAcA==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 8, 29, 14, 52, 33, 868, DateTimeKind.Local).AddTicks(2570));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 29, 14, 52, 33, 869, DateTimeKind.Local).AddTicks(7773));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 20, 17, 45, 18, 610, DateTimeKind.Local).AddTicks(3984));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 20, 17, 45, 18, 610, DateTimeKind.Local).AddTicks(2304));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 20, 17, 45, 18, 610, DateTimeKind.Local).AddTicks(9014));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 8, 20, 17, 45, 18, 610, DateTimeKind.Local).AddTicks(9022));

            migrationBuilder.UpdateData(
                table: "StockTransactionTypes",
                keyColumn: "ID",
                keyValue: 10,
                column: "Name",
                value: "خرید تغیر");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAECAOIilPM4qeggSQ2DVkm5rd9UaZrpB641Rng/eW4awU97akNeV5e4hHGhOB5evkkg==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 8, 20, 17, 45, 18, 607, DateTimeKind.Local).AddTicks(9675));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 20, 17, 45, 18, 609, DateTimeKind.Local).AddTicks(7952));
        }
    }
}

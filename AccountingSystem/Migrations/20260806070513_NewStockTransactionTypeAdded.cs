using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class NewStockTransactionTypeAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 6, 11, 35, 10, 149, DateTimeKind.Local).AddTicks(8193));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 6, 11, 35, 10, 149, DateTimeKind.Local).AddTicks(6309));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 6, 11, 35, 10, 150, DateTimeKind.Local).AddTicks(3602));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 8, 6, 11, 35, 10, 150, DateTimeKind.Local).AddTicks(3614));

            migrationBuilder.UpdateData(
                table: "StockTransactionTypes",
                keyColumn: "ID",
                keyValue: 4,
                column: "Name",
                value: "له ګدام څخه انتقال");

            migrationBuilder.InsertData(
                table: "StockTransactionTypes",
                columns: new[] { "ID", "Name" },
                values: new object[] { 12, "ګدام ته انتقال" });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEM2Z04MCXNIn4gGAlFmTuCyyqjYL233b6v5HPoWEcFRA9lKHNMhrkA4EoCCYJ+SOiA==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 8, 6, 11, 35, 10, 147, DateTimeKind.Local).AddTicks(3059));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 6, 11, 35, 10, 149, DateTimeKind.Local).AddTicks(1585));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "StockTransactionTypes",
                keyColumn: "ID",
                keyValue: 12);

            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 7, 28, 14, 59, 20, 734, DateTimeKind.Local).AddTicks(1891));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 7, 28, 14, 59, 20, 733, DateTimeKind.Local).AddTicks(9667));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 7, 28, 14, 59, 20, 734, DateTimeKind.Local).AddTicks(6159));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 7, 28, 14, 59, 20, 734, DateTimeKind.Local).AddTicks(6167));

            migrationBuilder.UpdateData(
                table: "StockTransactionTypes",
                keyColumn: "ID",
                keyValue: 4,
                column: "Name",
                value: "د ګدامونو ترمنځ انتقال");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKZSiJ/Ud2uZpLa6rXAERmjOejzfLM1g6mDoVTUEm6AbubGp0W0yj/5unfGXwLaYeg==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 7, 28, 14, 59, 20, 731, DateTimeKind.Local).AddTicks(7443));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 7, 28, 14, 59, 20, 733, DateTimeKind.Local).AddTicks(5755));
        }
    }
}

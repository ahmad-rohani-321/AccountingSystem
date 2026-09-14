using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class StockBalanceAddedToSaleDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockItemId",
                table: "SalesDetails",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 7, 11, 55, 17, 172, DateTimeKind.Local).AddTicks(73));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 7, 11, 55, 17, 171, DateTimeKind.Local).AddTicks(8468));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 7, 11, 55, 17, 172, DateTimeKind.Local).AddTicks(4620));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 9, 7, 11, 55, 17, 172, DateTimeKind.Local).AddTicks(4628));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMlu5Mg9aInQz2ZXe0yAnnM9vbYWfUW+PiHBxu+izYkggIG1nYGb/7VCbgdKliua6w==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 9, 7, 11, 55, 17, 169, DateTimeKind.Local).AddTicks(6035));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 7, 11, 55, 17, 171, DateTimeKind.Local).AddTicks(4504));

            migrationBuilder.CreateIndex(
                name: "IX_SalesDetails_StockItemId",
                table: "SalesDetails",
                column: "StockItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesDetails_StockBalances_StockItemId",
                table: "SalesDetails",
                column: "StockItemId",
                principalTable: "StockBalances",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesDetails_StockBalances_StockItemId",
                table: "SalesDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalesDetails_StockItemId",
                table: "SalesDetails");

            migrationBuilder.DropColumn(
                name: "StockItemId",
                table: "SalesDetails");

            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 5, 14, 0, 3, 163, DateTimeKind.Local).AddTicks(6314));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 5, 14, 0, 3, 163, DateTimeKind.Local).AddTicks(4642));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 5, 14, 0, 3, 164, DateTimeKind.Local).AddTicks(1367));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 9, 5, 14, 0, 3, 164, DateTimeKind.Local).AddTicks(1393));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMqRjkGx7pvLglRbygxqlFwZ2zJ+WqE8PTDFNVYv9q9BVZOOhBwy/O3zpbjUN82KFg==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 9, 5, 14, 0, 3, 161, DateTimeKind.Local).AddTicks(3478));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 5, 14, 0, 3, 163, DateTimeKind.Local).AddTicks(263));
        }
    }
}

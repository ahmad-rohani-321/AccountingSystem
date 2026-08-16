using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class JournalEntryMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 15, 9, 46, 9, 571, DateTimeKind.Local).AddTicks(6962));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 15, 9, 46, 9, 571, DateTimeKind.Local).AddTicks(5382));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 15, 9, 46, 9, 572, DateTimeKind.Local).AddTicks(1606));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 8, 15, 9, 46, 9, 572, DateTimeKind.Local).AddTicks(1614));

            migrationBuilder.InsertData(
                table: "JournalEntryTransactionTypes",
                columns: new[] { "ID", "TypeName" },
                values: new object[,]
                {
                    { 13, "متفرقه عواید" },
                    { 14, "مصارف" }
                });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMmkmz6Z6DgIR43imeF1bZLu9iocWZX9lqheSGhl4nKeIpyRDPHJ7+iTB51LLgENTg==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 8, 15, 9, 46, 9, 569, DateTimeKind.Local).AddTicks(5421));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 15, 9, 46, 9, 571, DateTimeKind.Local).AddTicks(1165));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "JournalEntryTransactionTypes",
                keyColumn: "ID",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "JournalEntryTransactionTypes",
                keyColumn: "ID",
                keyValue: 14);

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
    }
}

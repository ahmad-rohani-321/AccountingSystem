using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeIdAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeID",
                table: "Salery",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 21, 11, 10, 2, 699, DateTimeKind.Local).AddTicks(9840));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 21, 11, 10, 2, 699, DateTimeKind.Local).AddTicks(6959));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 21, 11, 10, 2, 700, DateTimeKind.Local).AddTicks(9574));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 9, 21, 11, 10, 2, 700, DateTimeKind.Local).AddTicks(9620));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMn4iO1OJ6v35gsQthMoDJeScSQRncj/ffC4UFvYgvq8IiErBzPFH3ilU33Ktu64vw==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 9, 21, 11, 10, 2, 694, DateTimeKind.Local).AddTicks(8009));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 21, 11, 10, 2, 698, DateTimeKind.Local).AddTicks(9643));

            migrationBuilder.CreateIndex(
                name: "IX_Salery_EmployeeID",
                table: "Salery",
                column: "EmployeeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Salery_Accounts_EmployeeID",
                table: "Salery",
                column: "EmployeeID",
                principalTable: "Accounts",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Salery_Accounts_EmployeeID",
                table: "Salery");

            migrationBuilder.DropIndex(
                name: "IX_Salery_EmployeeID",
                table: "Salery");

            migrationBuilder.DropColumn(
                name: "EmployeeID",
                table: "Salery");

            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 20, 12, 52, 4, 241, DateTimeKind.Local).AddTicks(4550));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 20, 12, 52, 4, 241, DateTimeKind.Local).AddTicks(470));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 20, 12, 52, 4, 242, DateTimeKind.Local).AddTicks(4093));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 9, 20, 12, 52, 4, 242, DateTimeKind.Local).AddTicks(4105));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHtnMwYG/OP0LwpItoj2KSoTbDOnR+31rJ1Yt25ySrcGVOB1wEgvRCC4St9mrtTE0Q==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 9, 20, 12, 52, 4, 237, DateTimeKind.Local).AddTicks(7858));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 9, 20, 12, 52, 4, 240, DateTimeKind.Local).AddTicks(3452));
        }
    }
}

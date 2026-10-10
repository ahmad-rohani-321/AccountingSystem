using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class SharesCalculatorAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharesCalculators",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Expences = table.Column<decimal>(type: "TEXT", nullable: false),
                    OurLoans = table.Column<decimal>(type: "TEXT", nullable: false),
                    OthersLoans = table.Column<decimal>(type: "TEXT", nullable: false),
                    Purchases = table.Column<decimal>(type: "TEXT", nullable: false),
                    Sales = table.Column<decimal>(type: "TEXT", nullable: false),
                    Saleries = table.Column<decimal>(type: "TEXT", nullable: false),
                    MainCurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharesCalculators", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SharesCalculators_Currencies_MainCurrencyId",
                        column: x => x.MainCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharesCalculators_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SharesDividers",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SharesCalculatorID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharesDividers", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SharesDividers_SharesCalculators_SharesCalculatorID",
                        column: x => x.SharesCalculatorID,
                        principalTable: "SharesCalculators",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharesDividers_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 10, 9, 50, 23, 685, DateTimeKind.Local).AddTicks(1704));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 10, 9, 50, 23, 684, DateTimeKind.Local).AddTicks(7643));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 10, 9, 50, 23, 686, DateTimeKind.Local).AddTicks(3579));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 10, 10, 9, 50, 23, 686, DateTimeKind.Local).AddTicks(3597));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFCeadf9QEdgvqWYfRSFrz6kRFvoPVot/aaJ1HrQcDp9xJ6msiNkE7v/vUlJXzQK+Q==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 10, 10, 9, 50, 23, 679, DateTimeKind.Local).AddTicks(839));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 10, 10, 9, 50, 23, 683, DateTimeKind.Local).AddTicks(4733));

            migrationBuilder.CreateIndex(
                name: "IX_SharesCalculators_CreatedByUserId",
                table: "SharesCalculators",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharesCalculators_MainCurrencyId",
                table: "SharesCalculators",
                column: "MainCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SharesDividers_CreatedByUserId",
                table: "SharesDividers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharesDividers_SharesCalculatorID",
                table: "SharesDividers",
                column: "SharesCalculatorID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharesDividers");

            migrationBuilder.DropTable(
                name: "SharesCalculators");

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
    }
}

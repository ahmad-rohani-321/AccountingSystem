using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class ThirdMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseExpenseDetails");

            migrationBuilder.DropTable(
                name: "PurchaseVariousExpenses");

            migrationBuilder.DropTable(
                name: "PurchaseExpenses");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "ItemsPrices",
                newName: "SalePrice");

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseBaseCurrencyPrice",
                table: "StockBalances",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseBaseCurrencyPrice",
                table: "StockBalances");

            migrationBuilder.RenameColumn(
                name: "SalePrice",
                table: "ItemsPrices",
                newName: "Price");

            migrationBuilder.CreateTable(
                name: "PurchaseExpenses",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    PurchaseID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    TotalExpense = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseExpenses", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurchaseExpenses_Purchases_PurchaseID",
                        column: x => x.PurchaseID,
                        principalTable: "Purchases",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseExpenses_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseExpenseDetails",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    PurchaseDetailItemID = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchaseExpenseID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ItemPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    PerExpense = table.Column<decimal>(type: "TEXT", nullable: false),
                    PerTamamShud = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalExpense = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalTamamShud = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseExpenseDetails", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurchaseExpenseDetails_PurchaseDetails_PurchaseDetailItemID",
                        column: x => x.PurchaseDetailItemID,
                        principalTable: "PurchaseDetails",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseExpenseDetails_PurchaseExpenses_PurchaseExpenseID",
                        column: x => x.PurchaseExpenseID,
                        principalTable: "PurchaseExpenses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseExpenseDetails_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseVariousExpenses",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchaseExpenseID = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseVariousExpenses", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurchaseVariousExpenses_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseVariousExpenses_Currencies_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseVariousExpenses_PurchaseExpenses_PurchaseExpenseID",
                        column: x => x.PurchaseExpenseID,
                        principalTable: "PurchaseExpenses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseVariousExpenses_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseExpenseDetails_CreatedByUserId",
                table: "PurchaseExpenseDetails",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseExpenseDetails_PurchaseDetailItemID",
                table: "PurchaseExpenseDetails",
                column: "PurchaseDetailItemID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseExpenseDetails_PurchaseExpenseID",
                table: "PurchaseExpenseDetails",
                column: "PurchaseExpenseID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseExpenses_CreatedByUserId",
                table: "PurchaseExpenses",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseExpenses_PurchaseID",
                table: "PurchaseExpenses",
                column: "PurchaseID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseVariousExpenses_AccountID",
                table: "PurchaseVariousExpenses",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseVariousExpenses_CreatedByUserId",
                table: "PurchaseVariousExpenses",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseVariousExpenses_CurrencyID",
                table: "PurchaseVariousExpenses",
                column: "CurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseVariousExpenses_PurchaseExpenseID",
                table: "PurchaseVariousExpenses",
                column: "PurchaseExpenseID");
        }
    }
}

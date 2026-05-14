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
            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: false),
                    SaleNo = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsRefunded = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsHolded = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Sales_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sales_Currencies_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sales_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderDetails",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemID = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitID = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderDetails", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SalesOrderDetails_Items_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Items",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrderDetails_UnitConversion_UnitID",
                        column: x => x.UnitID,
                        principalTable: "UnitConversion",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrderDetails_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrders_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesDetails",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemID = table.Column<int>(type: "INTEGER", nullable: false),
                    SaleID = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    PerPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Profit = table.Column<decimal>(type: "TEXT", nullable: false),
                    WarehouseID = table.Column<int>(type: "INTEGER", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    UnitConversionID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesDetails", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SalesDetails_Items_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Items",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesDetails_Sales_SaleID",
                        column: x => x.SaleID,
                        principalTable: "Sales",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesDetails_UnitConversion_UnitConversionID",
                        column: x => x.UnitConversionID,
                        principalTable: "UnitConversion",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesDetails_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesDetails_WareHouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "WareHouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 5, 14, 18, 1, 54, 611, DateTimeKind.Local).AddTicks(7682));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 5, 14, 18, 1, 54, 611, DateTimeKind.Local).AddTicks(5906));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 5, 14, 18, 1, 54, 612, DateTimeKind.Local).AddTicks(1708));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 5, 14, 18, 1, 54, 612, DateTimeKind.Local).AddTicks(1716));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHWZPoQD3J0RZ4iiS+yPlOKYjCLxo2Jl0K2gidoECKhZ7vaBKgFaoPp8KTAy/9LQQA==");

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 5, 14, 18, 1, 54, 610, DateTimeKind.Local).AddTicks(2337));

            migrationBuilder.CreateIndex(
                name: "IX_Sales_AccountID",
                table: "Sales",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CreatedByUserId",
                table: "Sales",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CurrencyID",
                table: "Sales",
                column: "CurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDetails_CreatedByUserId",
                table: "SalesDetails",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDetails_ItemID",
                table: "SalesDetails",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDetails_SaleID",
                table: "SalesDetails",
                column: "SaleID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDetails_UnitConversionID",
                table: "SalesDetails",
                column: "UnitConversionID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDetails_WarehouseID",
                table: "SalesDetails",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderDetails_CreatedByUserId",
                table: "SalesOrderDetails",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderDetails_ItemID",
                table: "SalesOrderDetails",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderDetails_UnitID",
                table: "SalesOrderDetails",
                column: "UnitID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_AccountID",
                table: "SalesOrders",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CreatedByUserId",
                table: "SalesOrders",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesDetails");

            migrationBuilder.DropTable(
                name: "SalesOrderDetails");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 5, 14, 16, 48, 36, 119, DateTimeKind.Local).AddTicks(6891));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 5, 14, 16, 48, 36, 119, DateTimeKind.Local).AddTicks(5223));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 5, 14, 16, 48, 36, 120, DateTimeKind.Local).AddTicks(1682));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 5, 14, 16, 48, 36, 120, DateTimeKind.Local).AddTicks(1690));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAECC+f8RvCmuGqkGsrJI8AcDudFYKibhBdZhnh/bPP5MNXBWnX3epwMuOINIVQELjMA==");

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 5, 14, 16, 48, 36, 117, DateTimeKind.Local).AddTicks(6304));
        }
    }
}

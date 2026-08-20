using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class SaleAndPurchaseEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseOrderDetails");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "SalesOrderDetails");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.AddColumn<bool>(
                name: "CanAffectStock",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanAffectStock",
                table: "Purchases",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AccountContacts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 19, 9, 56, 9, 88, DateTimeKind.Local).AddTicks(3966));

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 19, 9, 56, 9, 88, DateTimeKind.Local).AddTicks(2423));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 19, 9, 56, 9, 88, DateTimeKind.Local).AddTicks(8944));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 8, 19, 9, 56, 9, 88, DateTimeKind.Local).AddTicks(8952));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEO8oHjHEBEcGEmqCiKYFDSfzpEFVNHFFbybt0cPY+REOtAJYZQLPRoBHmgU8pV+j7A==");

            migrationBuilder.UpdateData(
                table: "UserRole",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "65a02658-9b8d-4505-95af-5edd8634bb35", "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01" },
                column: "CreationDate",
                value: new DateTime(2026, 8, 19, 9, 56, 9, 86, DateTimeKind.Local).AddTicks(584));

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 8, 19, 9, 56, 9, 87, DateTimeKind.Local).AddTicks(7798));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanAffectStock",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CanAffectStock",
                table: "Purchases");

            migrationBuilder.CreateTable(
                name: "PurchaseOrderDetails",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ItemID = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderDetails", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetails_Items_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Items",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetails_UnitConversion_UnitID",
                        column: x => x.UnitID,
                        principalTable: "UnitConversion",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetails_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_User_CreatedByUserId",
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
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ItemID = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true)
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
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_CreatedByUserId",
                table: "PurchaseOrderDetails",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_ItemID",
                table: "PurchaseOrderDetails",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_UnitID",
                table: "PurchaseOrderDetails",
                column: "UnitID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_AccountID",
                table: "PurchaseOrders",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedByUserId",
                table: "PurchaseOrders",
                column: "CreatedByUserId");

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
    }
}

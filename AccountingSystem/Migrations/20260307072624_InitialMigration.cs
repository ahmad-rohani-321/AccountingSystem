using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockTransactionTypes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactionTypes", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    ProfilePhoto = table.Column<string>(type: "TEXT", nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaim_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Categories_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Units_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserClaim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaim_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogin",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogin", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogin_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRole_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserToken",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserToken", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserToken_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WareHouses",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WareHouses", x => x.ID);
                    table.ForeignKey(
                        name: "FK_WareHouses_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NativeName = table.Column<string>(type: "TEXT", nullable: true),
                    AliasName = table.Column<string>(type: "TEXT", nullable: true),
                    SKU = table.Column<string>(type: "TEXT", nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinimumQuantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Items_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Items_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Items_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockBalances",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    BatchNo = table.Column<string>(type: "TEXT", nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    WarehouseID = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBalances", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StockBalances_Items_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Items",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockBalances_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockBalances_WareHouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "WareHouses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnitConversion",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemID = table.Column<int>(type: "INTEGER", nullable: false),
                    MainUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubUnitID = table.Column<int>(type: "INTEGER", nullable: false),
                    MainAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    SubAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExchangedAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitConversion", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UnitConversion_Items_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Items",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnitConversion_Units_MainUnitId",
                        column: x => x.MainUnitId,
                        principalTable: "Units",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnitConversion_Units_SubUnitID",
                        column: x => x.SubUnitID,
                        principalTable: "Units",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnitConversion_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockTransactions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    UnitID = table.Column<int>(type: "INTEGER", nullable: false),
                    TransactionID = table.Column<int>(type: "INTEGER", nullable: false),
                    StockBalanceID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StockTransactions_StockBalances_StockBalanceID",
                        column: x => x.StockBalanceID,
                        principalTable: "StockBalances",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransactions_StockTransactionTypes_TransactionID",
                        column: x => x.TransactionID,
                        principalTable: "StockTransactionTypes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Units_UnitID",
                        column: x => x.UnitID,
                        principalTable: "Units",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransactions_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "StockTransactionTypes",
                columns: new[] { "ID", "Name" },
                values: new object[,]
                {
                    { 1, "ابتدایي موجودي" },
                    { 2, "ګدام ته داخلول" },
                    { 3, "له ګدام څخه ایستل" },
                    { 4, "د ګدامونو ترمنځ انتقال" },
                    { 5, "خرید" },
                    { 6, "خرید واپسي" },
                    { 7, "فروش" },
                    { 8, "فروش واپسي" },
                    { 9, "ضایعات" }
                });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "FirstName", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "ProfilePhoto", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01", 0, "7a3c2e1d-9b8a-4f6e-8c2b-5d4f3a2b1c9e", "admin", true, "admin", "admin", false, null, "ADMIN", "ADMIN", "AQAAAAIAAYagAAAAEM3H7UK4F7dJQkybnrP1/uCr9R0f5qEHGfu6MUUa0GnCdVD+rlr88DC9t+nqhM459Q==", null, false, "", "2c9a4d9b-4f5a-4b8b-9a7c-2b1c3d4e5f61", false, "admin" });

            migrationBuilder.InsertData(
                table: "WareHouses",
                columns: new[] { "ID", "CreatedByUserId", "CreationDate", "Description", "IsActive", "Name" },
                values: new object[] { 1, "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01", new DateTime(2026, 3, 7, 11, 56, 24, 133, DateTimeKind.Local).AddTicks(9294), "اصلي ګدام د ټولو موادو لپاره دی.", true, "عمومي ګدام" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CreatedByUserId",
                table: "Categories",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId",
                table: "Items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CreatedByUserId",
                table: "Items",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_UnitId",
                table: "Items",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Role",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaim_RoleId",
                table: "RoleClaim",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_CreatedByUserId",
                table: "StockBalances",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_ItemID",
                table: "StockBalances",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_WarehouseID",
                table: "StockBalances",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_CreatedByUserId",
                table: "StockTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_StockBalanceID",
                table: "StockTransactions",
                column: "StockBalanceID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_TransactionID",
                table: "StockTransactions",
                column: "TransactionID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_UnitID",
                table: "StockTransactions",
                column: "UnitID");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversion_CreatedByUserId",
                table: "UnitConversion",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversion_ItemID",
                table: "UnitConversion",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversion_MainUnitId",
                table: "UnitConversion",
                column: "MainUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversion_SubUnitID",
                table: "UnitConversion",
                column: "SubUnitID");

            migrationBuilder.CreateIndex(
                name: "IX_Units_CreatedByUserId",
                table: "Units",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "User",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "User",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClaim_UserId",
                table: "UserClaim",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogin_UserId",
                table: "UserLogin",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                table: "UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WareHouses_CreatedByUserId",
                table: "WareHouses",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleClaim");

            migrationBuilder.DropTable(
                name: "StockTransactions");

            migrationBuilder.DropTable(
                name: "UnitConversion");

            migrationBuilder.DropTable(
                name: "UserClaim");

            migrationBuilder.DropTable(
                name: "UserLogin");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "UserToken");

            migrationBuilder.DropTable(
                name: "StockBalances");

            migrationBuilder.DropTable(
                name: "StockTransactionTypes");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "WareHouses");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class JournalEntryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurrencyExchanges_Currencies_MainCurrencyID",
                table: "CurrencyExchanges");

            migrationBuilder.DropForeignKey(
                name: "FK_CurrencyExchanges_Currencies_SubCurrencyID",
                table: "CurrencyExchanges");

            migrationBuilder.DropIndex(
                name: "IX_CurrencyExchanges_MainCurrencyID",
                table: "CurrencyExchanges");

            migrationBuilder.DropIndex(
                name: "IX_CurrencyExchanges_SubCurrencyID",
                table: "CurrencyExchanges");

            migrationBuilder.CreateTable(
                name: "JournalEntryTransactionTypes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryTransactionTypes", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Debit = table.Column<decimal>(type: "TEXT", nullable: false),
                    Credit = table.Column<decimal>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", nullable: false),
                    ChequePhoto = table.Column<string>(type: "TEXT", nullable: true),
                    TransactionTypeID = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountBalanceID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.ID);
                    table.ForeignKey(
                        name: "FK_JournalEntries_AccountBalances_AccountBalanceID",
                        column: x => x.AccountBalanceID,
                        principalTable: "AccountBalances",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JournalEntries_JournalEntryTransactionTypes_TransactionTypeID",
                        column: x => x.TransactionTypeID,
                        principalTable: "JournalEntryTransactionTypes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JournalEntries_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "AccountTypes",
                columns: new[] { "ID", "Name" },
                values: new object[,]
                {
                    { 8, "پورونه" },
                    { 9, "شریک" },
                    { 10, "کارمند" }
                });

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 3, 11, 21, 10, 35, 719, DateTimeKind.Local).AddTicks(3697));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 3, 11, 21, 10, 35, 719, DateTimeKind.Local).AddTicks(3717));

            migrationBuilder.InsertData(
                table: "JournalEntryTransactionTypes",
                columns: new[] { "ID", "TypeName" },
                values: new object[,]
                {
                    { 1, "اولنی بلانس" },
                    { 2, "د اسعارو تبادله" },
                    { 3, "نقد جمع" },
                    { 4, "نقد منفي" },
                    { 5, "فروش" },
                    { 6, "خرید" },
                    { 7, "فروش مکمل واپسي" },
                    { 8, "خرید مکمل واپسي" },
                    { 9, "فروش واپسي" },
                    { 10, "خرید واپسي" },
                    { 11, "د حسابونو تبادله" }
                });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMfFOz9RHz6cw4lRH7/aITaHAMHuQuJk8fzJlfODr6neMRryLnzksCjZ/aQLphLM4Q==");

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 3, 11, 21, 10, 35, 716, DateTimeKind.Local).AddTicks(6536));

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_AccountBalanceID",
                table: "JournalEntries",
                column: "AccountBalanceID");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_CreatedByUserId",
                table: "JournalEntries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TransactionTypeID",
                table: "JournalEntries",
                column: "TransactionTypeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "JournalEntryTransactionTypes");

            migrationBuilder.DeleteData(
                table: "AccountTypes",
                keyColumn: "ID",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "AccountTypes",
                keyColumn: "ID",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "AccountTypes",
                keyColumn: "ID",
                keyValue: 10);

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 3, 11, 13, 22, 2, 577, DateTimeKind.Local).AddTicks(4174));

            migrationBuilder.UpdateData(
                table: "Currencies",
                keyColumn: "ID",
                keyValue: 2,
                column: "CreationDate",
                value: new DateTime(2026, 3, 11, 13, 22, 2, 577, DateTimeKind.Local).AddTicks(4198));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEO/A5+rarplRSdWcma8qtkIUyaDv9sNGEwKnxZ+fQy3JeX0gSVzkieYdo3aZaosEmA==");

            migrationBuilder.UpdateData(
                table: "WareHouses",
                keyColumn: "ID",
                keyValue: 1,
                column: "CreationDate",
                value: new DateTime(2026, 3, 11, 13, 22, 2, 575, DateTimeKind.Local).AddTicks(8137));

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchanges_MainCurrencyID",
                table: "CurrencyExchanges",
                column: "MainCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchanges_SubCurrencyID",
                table: "CurrencyExchanges",
                column: "SubCurrencyID");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchanges_Currencies_MainCurrencyID",
                table: "CurrencyExchanges",
                column: "MainCurrencyID",
                principalTable: "Currencies",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchanges_Currencies_SubCurrencyID",
                table: "CurrencyExchanges",
                column: "SubCurrencyID",
                principalTable: "Currencies",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

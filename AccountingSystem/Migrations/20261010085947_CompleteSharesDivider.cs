using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class CompleteSharesDivider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SharesDividers_SharesCalculatorID",
                table: "SharesDividers");

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "SharesDividers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "SharesDividers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "SharesDividers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Percentage",
                table: "SharesDividers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "SharesDividers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShareAmount",
                table: "SharesDividers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "SharesDividers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DividableAmount",
                table: "SharesCalculators",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Payables",
                table: "SharesCalculators",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Receivables",
                table: "SharesCalculators",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleProfit",
                table: "SharesCalculators",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.InsertData(
                table: "JournalEntryTransactionTypes",
                columns: new[] { "ID", "TypeName" },
                values: new object[] { 16, "د ونډو وېش" });

            migrationBuilder.CreateIndex(
                name: "IX_SharesDividers_AccountId",
                table: "SharesDividers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SharesDividers_CurrencyId",
                table: "SharesDividers",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SharesDividers_SharesCalculatorID_AccountId",
                table: "SharesDividers",
                columns: new[] { "SharesCalculatorID", "AccountId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SharesDividers_Accounts_AccountId",
                table: "SharesDividers",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SharesDividers_Currencies_CurrencyId",
                table: "SharesDividers",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharesDividers_Accounts_AccountId",
                table: "SharesDividers");

            migrationBuilder.DropForeignKey(
                name: "FK_SharesDividers_Currencies_CurrencyId",
                table: "SharesDividers");

            migrationBuilder.DropIndex(
                name: "IX_SharesDividers_AccountId",
                table: "SharesDividers");

            migrationBuilder.DropIndex(
                name: "IX_SharesDividers_CurrencyId",
                table: "SharesDividers");

            migrationBuilder.DropIndex(
                name: "IX_SharesDividers_SharesCalculatorID_AccountId",
                table: "SharesDividers");

            migrationBuilder.DeleteData(
                table: "JournalEntryTransactionTypes",
                keyColumn: "ID",
                keyValue: 16);

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "SharesDividers");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "SharesDividers");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SharesDividers");

            migrationBuilder.DropColumn(
                name: "Percentage",
                table: "SharesDividers");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "SharesDividers");

            migrationBuilder.DropColumn(
                name: "ShareAmount",
                table: "SharesDividers");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "SharesDividers");

            migrationBuilder.DropColumn(
                name: "DividableAmount",
                table: "SharesCalculators");

            migrationBuilder.DropColumn(
                name: "Payables",
                table: "SharesCalculators");

            migrationBuilder.DropColumn(
                name: "Receivables",
                table: "SharesCalculators");

            migrationBuilder.DropColumn(
                name: "SaleProfit",
                table: "SharesCalculators");

            migrationBuilder.CreateIndex(
                name: "IX_SharesDividers_SharesCalculatorID",
                table: "SharesDividers",
                column: "SharesCalculatorID");
        }
    }
}

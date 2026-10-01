using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Migrations
{
    /// <inheritdoc />
    public partial class TreasureAccountAddedToMonthlySalery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TreasureAccountID",
                table: "Salery",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Salery_TreasureAccountID",
                table: "Salery",
                column: "TreasureAccountID");

            migrationBuilder.AddForeignKey(
                name: "FK_Salery_Accounts_TreasureAccountID",
                table: "Salery",
                column: "TreasureAccountID",
                principalTable: "Accounts",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Salery_Accounts_TreasureAccountID",
                table: "Salery");

            migrationBuilder.DropIndex(
                name: "IX_Salery_TreasureAccountID",
                table: "Salery");

            migrationBuilder.DropColumn(
                name: "TreasureAccountID",
                table: "Salery");
        }
    }
}

using AccountingSystem.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261001080000_SharesTableAdded")]
    public partial class SharesTableAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shares",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shares", x => x.ID);
                    table.ForeignKey("FK_Shares_Accounts_AccountId", x => x.AccountId, "Accounts", "ID", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_Shares_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "ID", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_Shares_User_CreatedByUserId", x => x.CreatedByUserId, "User", "Id");
                });

            migrationBuilder.CreateIndex("IX_Shares_AccountId", "Shares", "AccountId");
            migrationBuilder.CreateIndex("IX_Shares_CurrencyId", "Shares", "CurrencyId");
            migrationBuilder.CreateIndex("IX_Shares_CreatedByUserId", "Shares", "CreatedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Shares");
        }
    }
}

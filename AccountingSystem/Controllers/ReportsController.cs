using AccountingSystem.Models.Inventory;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AccountingSystem.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        #region Accounts list
        [HttpPost]
        public IActionResult PersonAccounts([FromForm] string datasource)
        {
            Reports.Account.PersonAccounts personAccounts = new Reports.Account.PersonAccounts();
            var reportData = string.IsNullOrWhiteSpace(datasource)
                ? new List<AccountListItem>()
                : JsonSerializer.Deserialize<List<AccountListItem>>(datasource) ?? new List<AccountListItem>();

            personAccounts.DataSource = reportData;
            return PartialView("_Report", personAccounts);
        }

        [HttpPost]
        public IActionResult NormalAccount([FromForm] string datasource)
        {
            Reports.Account.NormalAccounts personAccounts = new Reports.Account.NormalAccounts();
            var reportData = string.IsNullOrWhiteSpace(datasource)
                ? new List<AccountListItem>()
                : JsonSerializer.Deserialize<List<AccountListItem>>(datasource) ?? new List<AccountListItem>();

            personAccounts.DataSource = reportData;
            return PartialView("_Report", personAccounts);
        }

        [HttpPost]
        public IActionResult ContributorAccounts([FromForm] string dataSource)
        {
            Reports.Account.ContributorAccounts contributorAccounts = new Reports.Account.ContributorAccounts();
            var reportData = string.IsNullOrWhiteSpace(dataSource)
                ? new List<AccountListItem>()
                : JsonSerializer.Deserialize<List<AccountListItem>>(dataSource) ?? new List<AccountListItem>();

            contributorAccounts.DataSource = reportData;
            return PartialView("_Report", contributorAccounts);
        }
        #endregion

        #region Inventory
        [HttpPost]
        public IActionResult ItemsList([FromForm] string dataSource)
        {
            Reports.Inventory.ItemsList inventory = new Reports.Inventory.ItemsList();
            var reportData = string.IsNullOrWhiteSpace(dataSource)
                ? new List<Item>()
                : JsonSerializer.Deserialize<List<Item>>(dataSource) ?? new List<Item>();

            inventory.DataSource = reportData;
            return PartialView("_Report", inventory);
        }

        [HttpPost]
        public IActionResult StockItemsList([FromForm] string dataSource)
        {
            Reports.Inventory.StockItemsList inventory = new Reports.Inventory.StockItemsList();
            var reportData = string.IsNullOrWhiteSpace(dataSource)
                ? new List<StockBalance>()
                : JsonSerializer.Deserialize<List<StockBalance>>(dataSource) ?? new List<StockBalance>();

            inventory.DataSource = reportData;
            return PartialView("_Report", inventory);
        }

        [HttpPost]
        public IActionResult LeastItemReport([FromForm] string dataSource)
        {
            Reports.Inventory.LeastItemsReport inventory = new Reports.Inventory.LeastItemsReport();
            var reportData = string.IsNullOrWhiteSpace(dataSource)
                ? new List<LeastItemReportRow>()
                : JsonSerializer.Deserialize<List<LeastItemReportRow>>(dataSource) ?? new List<LeastItemReportRow>();

            inventory.DataSource = reportData;
            return PartialView("_Report", inventory);
        }
        #endregion

        #region JournalEntry
        [HttpPost]
        public IActionResult JournalEntry([FromForm] string dataSource)
        {
            Reports.JournalEntry.JournalEntryReport journalEntry = new Reports.JournalEntry.JournalEntryReport();
            var reportData = string.IsNullOrWhiteSpace(dataSource)
                ? new List<JournalEntryReportRow>()
                : JsonSerializer.Deserialize<List<JournalEntryReportRow>>(
                      dataSource,
                      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                  ) ?? new List<JournalEntryReportRow>();

            journalEntry.DataSource = reportData;
            return PartialView("_Report", journalEntry);
        }
        #endregion

        private sealed class JournalEntryReportRow
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public string AccountName { get; set; } = string.Empty;
            public string Currency { get; set; } = string.Empty;
            public string TransactionType { get; set; } = string.Empty;
            public decimal Credit { get; set; }
            public decimal Debit { get; set; }
            public decimal Balance { get; set; }
            public string Remarks { get; set; } = string.Empty;
        }

        private sealed class LeastItemReportRow
        {
            public string NativeName { get; set; } = string.Empty;
            public string AliasName { get; set; } = string.Empty;
            public string WarehouseName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public string UnitName { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal MinimumQuantity { get; set; }
        }
    }
}

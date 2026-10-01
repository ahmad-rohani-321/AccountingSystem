using AccountingSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AccountingSystem.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        [HttpGet("GetSalesDashboard")]
        public async Task<ActionResult> GetSalesDashboard(string period = "month", DateTime? startDate = null, DateTime? endDate = null)
        {
            if (!TryGetDateRange(period, startDate, endDate, out var start, out var end))
            {
                return BadRequest("صحیح نېټه او موده انتخاب کړئ.");
            }

            var sales = await _context.Sales.AsNoTracking()
                .Where(x => x.CreationDate >= start && x.CreationDate < end)
                .Select(x => new
                {
                    x.ID, x.SaleNo, x.CreationDate, x.TotalAmount, x.ReceivedAmount,
                    x.RemainingAmount, x.IsHolded, x.IsRefunded, x.CanAffectStock,
                    x.CurrencyID, CurrencyName = x.Currency.CurrencyName,
                    CustomerName = x.Account.Name,
                    SellerName = x.CreatedByUser.FirstName + " " + x.CreatedByUser.LastName
                }).ToListAsync();

            var completed = sales.Where(x => !x.IsHolded && !x.IsRefunded).ToList();
            var saleIds = completed.Select(x => x.ID).ToArray();
            var details = await _context.SalesDetails.AsNoTracking()
                .Where(x => saleIds.Contains(x.SaleID))
                .Select(x => new
                {
                    x.SaleID, x.ItemID, ItemName = x.Item.NativeName,
                    x.Quantity, x.Profit, x.UnitConversion.ExchangedAmount
                }).ToListAsync();
            var profitBySale = details.GroupBy(x => x.SaleID)
                .ToDictionary(x => x.Key, x => x.Sum(d => d.Profit));

            var journal = await _context.JournalEntries.AsNoTracking()
                .Where(x => (x.TransactionTypeID == 5 || x.TransactionTypeID == 8 || x.TransactionTypeID == 9) &&
                    x.CreationDate >= start && x.CreationDate < end)
                .Select(x => new
                {
                    x.TransactionTypeID, x.Debit, x.Credit,
                    CurrencyName = x.AccountBalance.Currency.CurrencyName
                }).ToListAsync();

            var currencySummary = completed.GroupBy(x => new { x.CurrencyID, x.CurrencyName })
                .Select(x => new
                {
                    x.Key.CurrencyID, x.Key.CurrencyName,
                    SalesCount = x.Count(),
                    TotalAmount = x.Sum(s => s.TotalAmount),
                    ReceivedAmount = x.Sum(s => s.ReceivedAmount),
                    RemainingAmount = x.Sum(s => s.RemainingAmount),
                    Profit = x.Sum(s => profitBySale.GetValueOrDefault(s.ID))
                }).OrderBy(x => x.CurrencyName).ToList();

            var topItems = details.Where(x => x.ExchangedAmount > 0)
                .GroupBy(x => new { x.ItemID, x.ItemName })
                .Select(x => new { x.Key.ItemID, x.Key.ItemName, Quantity = x.Sum(d => d.Quantity / d.ExchangedAmount), SalesCount = x.Select(d => d.SaleID).Distinct().Count() })
                .OrderByDescending(x => x.Quantity).Take(10).ToList();

            var topCustomers = completed.GroupBy(x => x.CustomerName)
                .Select(x => new { CustomerName = x.Key, SalesCount = x.Count() })
                .OrderByDescending(x => x.SalesCount).Take(10).ToList();

            var topSellers = completed.GroupBy(x => x.SellerName)
                .Select(x => new { SellerName = x.Key, SalesCount = x.Count() })
                .OrderByDescending(x => x.SalesCount).Take(10).ToList();

            var monthly = (end - start).TotalDays > 62;
            var calendar = new PersianCalendar();
            var chart = completed.GroupBy(x => monthly ? x.CreationDate.Date.AddDays(1 - calendar.GetDayOfMonth(x.CreationDate)) : x.CreationDate.Date)
                .Select(x => new { Date = x.Key, SalesCount = x.Count() })
                .OrderBy(x => x.Date).ToList();

            var journalActivity = journal.GroupBy(x => new { x.TransactionTypeID, x.CurrencyName })
                .Select(x => new
                {
                    x.Key.TransactionTypeID, x.Key.CurrencyName,
                    EntryCount = x.Count(), Debit = x.Sum(j => j.Debit), Credit = x.Sum(j => j.Credit)
                }).OrderBy(x => x.TransactionTypeID).ThenBy(x => x.CurrencyName).ToList();

            return Ok(new
            {
                StartDate = start, EndDate = end.AddDays(-1),
                CompletedCount = completed.Count,
                HoldCount = sales.Count(x => x.IsHolded && !x.IsRefunded),
                RefundedCount = sales.Count(x => x.IsRefunded),
                StockAffectedCount = completed.Count(x => x.CanAffectStock),
                CurrencySummary = currencySummary,
                Chart = chart,
                TopItems = topItems,
                TopCustomers = topCustomers,
                TopSellers = topSellers,
                JournalActivity = journalActivity,
                RecentSales = sales.OrderByDescending(x => x.CreationDate).ThenByDescending(x => x.ID).Take(15)
                    .Select(x => new { x.SaleNo, x.CreationDate, x.CustomerName, x.CurrencyName, x.TotalAmount,
                        Profit = !x.IsHolded && !x.IsRefunded ? (decimal?)profitBySale.GetValueOrDefault(x.ID) : null,
                        x.IsHolded, x.IsRefunded })
            });
        }

        private bool TryGetDateRange(string period, DateTime? startDate, DateTime? endDate, out DateTime start, out DateTime end)
        {
            var today = DateTime.Today;
            var calendar = new PersianCalendar();
            start = today;
            end = today.AddDays(1);
            switch (period)
            {
                case "today": return true;
                case "week": start = today.AddDays(-(((int)today.DayOfWeek + 1) % 7)); return true;
                case "month": start = today.AddDays(1 - calendar.GetDayOfMonth(today)); return true;
                case "sixMonths": start = calendar.AddMonths(today, -6); return true;
                case "year": start = calendar.ToDateTime(calendar.GetYear(today), 1, 1, 0, 0, 0, 0); return true;
                case "custom":
                    if (!startDate.HasValue || !endDate.HasValue || startDate.Value.Date > endDate.Value.Date ||
                        endDate.Value.Date == DateTime.MaxValue.Date || startDate.Value.Date < calendar.MinSupportedDateTime.Date)
                        return false;
                    start = startDate.Value.Date;
                    end = endDate.Value.Date.AddDays(1);
                    return true;
                default: return false;
            }
        }
    }
}

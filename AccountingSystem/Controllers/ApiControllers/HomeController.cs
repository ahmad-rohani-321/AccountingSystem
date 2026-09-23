using AccountingSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AccountingSystem.Controllers.ApiControllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class HomeController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet("GetSummaryCounts")]
    public async Task<ActionResult> GetSummaryCounts()
    {
        return Ok(new
        {
            ItemsCount = await _context.Items.CountAsync(x => x.IsActive),
            SuppliersCount = await _context.Accounts.CountAsync(x => x.AccountTypeID == 3),
            CustomersCount = await _context.Accounts.CountAsync(x => x.AccountTypeID == 4),
            BothAccountsCount = await _context.Accounts.CountAsync(x => x.AccountTypeID == 5),
            WarehousesCount = await _context.WareHouses.CountAsync(x => x.IsActive)
        });
    }

    [HttpGet("GetSalesAnalytics")]
    public async Task<ActionResult> GetSalesAnalytics(string period = "today", DateTime? startDate = null, DateTime? endDate = null)
    {
        if (!TryGetDateRange(period, startDate, endDate, out var start, out var end))
            return BadRequest("صحیح نېټه او موده انتخاب کړئ.");

        var sales = await _context.Sales.AsNoTracking()
            .Where(x => !x.IsHolded && !x.IsRefunded && x.CreationDate >= start && x.CreationDate < end)
            .Select(x => new
            {
                x.CreatedByUserId,
                SellerName = x.CreatedByUser.FirstName + " " + x.CreatedByUser.LastName,
                x.CreationDate
            }).ToListAsync();

        var topSellers = sales.GroupBy(x => new { x.CreatedByUserId, x.SellerName })
            .Select(x => new { x.Key.SellerName, SalesCount = x.Count(), x.Key.CreatedByUserId })
            .OrderByDescending(x => x.SalesCount).ThenBy(x => x.CreatedByUserId).Take(5).ToList();

        // Count completed sales so amounts in different currencies are never added together.
        var monthly = (end - start).TotalDays > 62;
        var calendar = new PersianCalendar();
        var counts = sales.GroupBy(x => monthly
                ? x.CreationDate.Date.AddDays(1 - calendar.GetDayOfMonth(x.CreationDate))
                : x.CreationDate.Date)
            .ToDictionary(x => x.Key, x => x.Count());
        var chart = new List<object>();
        for (var date = monthly ? start.AddDays(1 - calendar.GetDayOfMonth(start)) : start;
             date < end; date = monthly ? calendar.AddMonths(date, 1) : date.AddDays(1))
        {
            chart.Add(new { Date = date, SalesCount = counts.GetValueOrDefault(date) });
            if (monthly && (end - date).TotalDays <= calendar.GetDaysInMonth(calendar.GetYear(date), calendar.GetMonth(date)))
                break;
        }

        return Ok(new { TopSellers = topSellers, Chart = chart });
    }

    [HttpGet("GetActiveCurrencies")]
    public async Task<ActionResult> GetActiveCurrencies()
    {
        var currencies = await _context.Currencies.AsNoTracking().Where(x => x.IsActive)
            .OrderByDescending(x => x.IsMainCurrency).ThenBy(x => x.ID)
            .Select(x => new { x.CurrencyName, x.CurrencySymbole }).ToListAsync();
        return Ok(currencies);
    }

    [HttpGet("GetLatestCurrencyPrices")]
    public async Task<ActionResult> GetLatestCurrencyPrices()
    {
        var prices = await _context.CurrencyExchanges.AsNoTracking()
            .Where(x => !_context.CurrencyExchanges.Any(p =>
                p.MainCurrencyID == x.MainCurrencyID && p.SubCurrencyID == x.SubCurrencyID &&
                (p.CreationDate > x.CreationDate || (p.CreationDate == x.CreationDate && p.ID > x.ID))))
            .OrderByDescending(x => x.CreationDate).ThenByDescending(x => x.ID)
            .Select(x => new
            {
                CurrencyName = x.SubCurrency.CurrencyName,
                MainCurrencyName = x.MainCurrency.CurrencyName,
                x.CurrencyExchangeRate
            }).ToListAsync();
        return Ok(prices);
    }

    [HttpGet("GetTopSoldItems")]
    public async Task<ActionResult> GetTopSoldItems(string period = "today")
    {
        if (!TryGetDateRange(period, null, null, out var start, out var end))
            return BadRequest("صحیح موده انتخاب کړئ.");

        var details = await _context.SalesDetails.AsNoTracking()
            .Where(x => !x.Sale.IsHolded && !x.Sale.IsRefunded &&
                x.Sale.CreationDate >= start && x.Sale.CreationDate < end)
            .Select(x => new
            {
                x.ItemID,
                ItemName = x.Item.NativeName,
                UnitName = x.Item.Unit.Name,
                x.Quantity,
                x.UnitConversion.ExchangedAmount
            }).ToListAsync();

        if (details.Any(x => x.ExchangedAmount <= 0))
            return BadRequest("د واحد د تبدیل معلومات ناسم دي.");

        var items = details.GroupBy(x => new { x.ItemID, x.ItemName, x.UnitName })
            .Select(x => new
            {
                x.Key.ItemID,
                x.Key.ItemName,
                x.Key.UnitName,
                Quantity = x.Sum(d => d.Quantity / d.ExchangedAmount)
            }).OrderByDescending(x => x.Quantity).ThenBy(x => x.ItemID).Take(15).ToList();
        return Ok(items);
    }

    private bool TryGetDateRange(string period, DateTime? startDate, DateTime? endDate, out DateTime start, out DateTime end)
    {
        var today = DateTime.Today;
        var calendar = new PersianCalendar();
        start = today;
        end = today.AddDays(1);
        switch (period)
        {
            case "today":
                return true;
            case "week":
                start = today.AddDays(-(((int)today.DayOfWeek + 1) % 7));
                return true;
            case "month":
                start = today.AddDays(1 - calendar.GetDayOfMonth(today));
                return true;
            case "sixMonths":
                start = calendar.AddMonths(today, -6);
                return true;
            case "year":
                start = calendar.ToDateTime(calendar.GetYear(today), 1, 1, 0, 0, 0, 0);
                return true;
            case "custom":
                if (!startDate.HasValue || !endDate.HasValue || startDate.Value.Date > endDate.Value.Date ||
                    endDate.Value.Date == DateTime.MaxValue.Date || startDate.Value.Date < calendar.MinSupportedDateTime.Date)
                    return false;
                start = startDate.Value.Date;
                end = endDate.Value.Date.AddDays(1);
                return true;
            default:
                return false;
        }
    }
}

using System.Globalization;
using AccountingSystem.Data;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StockJournalController(ApplicationDbContext db) : ControllerBase
{
    private readonly ApplicationDbContext _db = db;

    [HttpGet]
    public async Task<object> Get(
        DataSourceLoadOptions loadOptions,
        string itemIds,
        string warehouseIds,
        string startDate,
        string endDate)
    {
        var parsedItemIds = ParseIds(itemIds);
        var parsedWarehouseIds = ParseIds(warehouseIds);
        var parsedStartDate = ParseDate(startDate);
        var parsedEndDate = ParseDate(endDate);

        var query = _db.StockTransactions
            .AsNoTracking()
            .Where(t =>
                t.StockBalance != null &&
                t.StockBalance.Item != null &&
                t.StockBalance.Item.IsActive &&
                t.StockBalance.Item.Unit != null &&
                t.StockBalance.Item.Unit.IsActive &&
                t.StockBalance.Warehouse != null &&
                t.StockBalance.Warehouse.IsActive &&
                t.Unit != null &&
                t.Unit.IsActive);

        if (parsedItemIds.Count > 0)
            query = query.Where(t => parsedItemIds.Contains(t.StockBalance.ItemID));

        if (parsedWarehouseIds.Count > 0)
            query = query.Where(t => parsedWarehouseIds.Contains(t.StockBalance.WarehouseID));

        if (parsedStartDate.HasValue)
        {
            var start = parsedStartDate.Value.Date;
            query = query.Where(t => t.CreationDate >= start);
        }

        if (parsedEndDate.HasValue)
        {
            var endExclusive = parsedEndDate.Value.Date.AddDays(1);
            query = query.Where(t => t.CreationDate < endExclusive);
        }

        var shapedQuery = query
            .OrderByDescending(t => t.CreationDate)
            .ThenByDescending(t => t.ID)
            .Select(t => new
            {
                t.ID,
                Item = t.StockBalance.Item.NativeName,
                TransactionTypeName = t.Transaction.Name,
                Unit = t.Unit.Name,
                t.Quantity,
                Stock = t.StockBalance.Warehouse.Name,
                t.CreationDate,
                Remarks = t.Remarks ?? string.Empty,
                ItemID = t.StockBalance.ItemID,
                WarehouseID = t.StockBalance.WarehouseID
            });

        return await DataSourceLoader.LoadAsync(shapedQuery, loadOptions);
    }

    private static List<int> ParseIds(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : 0)
            .Where(value => value > 0)
            .Distinct()
            .ToList();
    }

    private static DateTime? ParseDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate))
            return exactDate;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return parsedDate;

        return null;
    }
}

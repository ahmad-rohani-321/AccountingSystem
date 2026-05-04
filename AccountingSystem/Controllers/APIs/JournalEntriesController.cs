using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models.Inventory;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class JournalEntriesController(ApplicationDbContext db) : ApiControllerBase
{
    private readonly ApplicationDbContext _db = db;
    private static readonly int[] AllowedAccountTypeIds = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    [HttpGet]
    public async Task<object> Get([FromQuery] string accountIds, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var today = DateTime.Today;
        var effectiveFromDate = fromDate?.Date ?? today;
        var effectiveToDate = (toDate?.Date ?? today).AddDays(1);
        var selectedAccountIds = string.IsNullOrWhiteSpace(accountIds)
            ? []
            : accountIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var parsedId) ? parsedId : 0)
                .Where(parsedId => parsedId > 0)
                .Distinct()
                .ToArray();

        var query = _db.JournalEntries
            .AsNoTracking()
            .Where(j => j.CreationDate >= effectiveFromDate && j.CreationDate < effectiveToDate);

        if (selectedAccountIds.Length > 0)
        {
            query = query.Where(j => selectedAccountIds.Contains(j.AccountBalance.AccountID));
        }

        var rows = await query
            .Select(j => new
            {
                Id = j.ID,
                Date = j.CreationDate,
                AccountName = j.AccountBalance.Account.Name,
                Currency = j.AccountBalance.Currency.CurrencyName,
                TransactionType = j.TransactionType.TypeName,
                Credit = j.Credit,
                Debit = j.Debit,
                Balance = j.Balance,
                Remarks = j.Remarks
            })
            .ToListAsync();

        return rows;
    }

    [HttpGet("page-data")]
    public async Task<IActionResult> GetPageData()
    {
        var activeCurrencies = await _db.Currencies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.ID)
            .Select(c => new
            {
                c.ID,
                c.CurrencyName,
                c.IsMainCurrency
            })
            .ToListAsync();

        var accountOptions = await _db.Accounts
            .AsNoTracking()
            .Where(a => a.IsActive && AllowedAccountTypeIds.Contains(a.AccountTypeID))
            .OrderBy(a => a.Name)
            .Select(a => new
            {
                a.ID,
                a.Name,
                a.Code,
                a.AccountTypeID,
                AccountTypeName = a.AccountType.Name
            })
            .ToListAsync();

        var accountBalances = await _db.AccountBalances
            .AsNoTracking()
            .Where(ab =>
                ab.Account.IsActive &&
                AllowedAccountTypeIds.Contains(ab.Account.AccountTypeID) &&
                ab.Currency.IsActive)
            .Select(ab => new
            {
                ab.AccountID,
                ab.CurrencyID,
                ab.Balance
            })
            .ToListAsync();

        return Ok(new
        {
            ActiveCurrencies = activeCurrencies,
            AccountOptions = accountOptions,
            AccountBalances = accountBalances
        });
    }

    [HttpGet("item-prices")]
    public async Task<object> GetItemPrices(DataSourceLoadOptions loadOptions)
    {
        var rows = await _db.Items
            .AsNoTracking()
            .Where(i => i.IsActive)
            .OrderBy(i => i.NativeName)
            .Select(i => new
            {
                ItemID = i.ID,
                ItemName = i.NativeName,
                Price = _db.ItemsPrices
                    .Where(ip => ip.ItemID == i.ID)
                    .OrderByDescending(ip => ip.ID)
                    .Select(ip => (decimal?)ip.Price)
                    .FirstOrDefault(),
                Description = _db.ItemsPrices
                    .Where(ip => ip.ItemID == i.ID)
                    .OrderByDescending(ip => ip.ID)
                    .Select(ip => ip.Remarks)
                    .FirstOrDefault() ?? string.Empty
            })
            .ToListAsync();

        return DataSourceLoader.Load(rows, loadOptions);
    }

    [HttpPut("item-prices")]
    public async Task<IActionResult> PutItemPrice([FromForm] int key, [FromForm] string values)
    {
        var item = await _db.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ID == key && i.IsActive);

        if (item is null)
            return NotFound();

        var latestItemPrice = await _db.ItemsPrices
            .AsNoTracking()
            .Where(ip => ip.ItemID == key)
            .OrderByDescending(ip => ip.ID)
            .FirstOrDefaultAsync();

        var payload = ParseItemPriceValues(values);
        var resolvedPrice = payload.Price ?? latestItemPrice?.Price;
        var resolvedDescription = payload.DescriptionProvided
            ? payload.Description
            : latestItemPrice?.Remarks ?? string.Empty;

        if (resolvedPrice is null)
            ModelState.AddModelError(nameof(ItemPrice.Price), "Price is required.");
        else if (resolvedPrice < 0)
            ModelState.AddModelError(nameof(ItemPrice.Price), "Price cannot be less than zero.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(CurrentUserId))
            return Unauthorized();

        var entity = new ItemPrice
        {
            ItemID = key,
            Price = resolvedPrice!.Value,
            Remarks = resolvedDescription,
            CreationDate = DateTime.UtcNow,
            CreatedByUserId = CurrentUserId
        };

        _db.ItemsPrices.Add(entity);

        await _db.SaveChangesAsync();
        return Ok(new
        {
            ItemID = item.ID,
            ItemName = item.NativeName,
            entity.Price,
            Description = entity.Remarks
        });
    }

    [HttpDelete("item-prices")]
    public async Task<IActionResult> DeleteItemPrice(int key)
    {
        var itemExists = await _db.Items
            .AsNoTracking()
            .AnyAsync(i => i.ID == key && i.IsActive);

        if (!itemExists)
            return NotFound();

        var prices = await _db.ItemsPrices
            .Where(ip => ip.ItemID == key)
            .ToListAsync();

        if (prices.Count > 0)
        {
            _db.ItemsPrices.RemoveRange(prices);
            await _db.SaveChangesAsync();
        }

        return Ok();
    }

    private static (decimal? Price, string Description, bool DescriptionProvided) ParseItemPriceValues(string values)
    {
        decimal? price = null;
        var description = string.Empty;
        var descriptionProvided = false;

        if (string.IsNullOrWhiteSpace(values))
            return (price, description, descriptionProvided);

        var formValues = JsonSerializer.Deserialize<JsonElement>(values);
        if (formValues.ValueKind != JsonValueKind.Object)
            return (price, description, descriptionProvided);

        if (formValues.TryGetProperty(nameof(ItemPrice.Price), out var priceValue) &&
            priceValue.ValueKind != JsonValueKind.Null)
        {
            if (priceValue.TryGetDecimal(out var parsedPrice))
                price = parsedPrice;
        }

        if (formValues.TryGetProperty("Description", out var descriptionValue) &&
            descriptionValue.ValueKind != JsonValueKind.Null)
        {
            descriptionProvided = true;
            description = descriptionValue.GetString() ?? string.Empty;
        }

        return (price, description, descriptionProvided);
    }
}

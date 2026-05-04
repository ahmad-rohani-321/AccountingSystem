using AccountingSystem.Data;
using AccountingSystem.Models.Settings;
using AccountingSystem.ViewModels;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CurrencyExchangeController(ApplicationDbContext db) : ApiControllerBase
{
    private readonly ApplicationDbContext _db = db;

    [HttpGet]
    public async Task<object> Get(DataSourceLoadOptions loadOptions)
    {
        var activeCurrencies = await _db.Currencies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.ID)
            .Select(c => new
            {
                c.ID,
                c.CurrencyName
            })
            .ToListAsync();

        var exchangeHistory = await _db.CurrencyExchanges
            .AsNoTracking()
            .OrderByDescending(e => e.CreationDate)
            .ToListAsync();

        var latestExchangeByCurrency = exchangeHistory
            .GroupBy(e => e.SubCurrencyID)
            .ToDictionary(g => g.Key, g => g.First());

        var rows = activeCurrencies.Select(currency =>
        {
            latestExchangeByCurrency.TryGetValue(currency.ID, out var exchange);

            return new CurrencyExchangeVM
            {
                SubCurrencyID = currency.ID,
                SubCurrencyName = currency.CurrencyName,
                SubCurrencyAmount = exchange?.SubCurrencyAmount ?? 0m,
                MainCurrencyAmount = exchange?.MainCurrencyAmount ?? 0m,
                ExchangeRate = exchange?.CurrencyExchangeRate ?? 0m
            };
        }).ToList();

        return DataSourceLoader.Load(rows, loadOptions);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] IEnumerable<CurrencyExchangeVM> payload)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var mainCurrency = await _db.Currencies.FirstOrDefaultAsync(c => c.IsMainCurrency);
        if (mainCurrency is null)
            return BadRequest(new { Message = "Main currency was not found." });

        var newEntries = payload.Select(item => new CurrencyExchange
        {
            MainCurrencyID = mainCurrency.ID,
            SubCurrencyID = item.SubCurrencyID,
            MainCurrencyAmount = item.MainCurrencyAmount,
            SubCurrencyAmount = item.SubCurrencyAmount,
            CurrencyExchangeRate = item.MainCurrencyAmount != 0 ? item.SubCurrencyAmount / item.MainCurrencyAmount : 0,
            CreationDate = DateTime.UtcNow,
            CreatedByUserId = userId
        }).ToList();

        await _db.CurrencyExchanges.AddRangeAsync(newEntries);
        await _db.SaveChangesAsync();

        return Ok();
    }
}

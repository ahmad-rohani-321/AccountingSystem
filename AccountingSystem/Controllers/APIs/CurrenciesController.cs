using AccountingSystem.Data;
using AccountingSystem.Models.Settings;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CurrenciesController(ApplicationDbContext db) : ApiControllerBase
{
    private readonly ApplicationDbContext _db = db;

    [HttpGet]
    public async Task<object> Get(DataSourceLoadOptions loadOptions)
    {
        var data = await _db.Currencies
            .Include(c => c.CreatedByUser)
            .AsNoTracking()
            .ToListAsync();

        return DataSourceLoader.Load(data, loadOptions);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] string values)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var entity = new Currency
        {
            CreationDate = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        ApplyValues(entity, values);
        await ClearOtherMainCurrenciesAsync(entity.IsMainCurrency);

        _db.Currencies.Add(entity);

        if (!TryValidateModel(entity))
            return BadRequest(ModelState);

        await _db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
    {
        var entity = await _db.Currencies.FirstOrDefaultAsync(c => c.ID == key);
        if (entity is null)
            return NotFound();

        ApplyValues(entity, values);
        await ClearOtherMainCurrenciesAsync(entity.IsMainCurrency, key);

        if (!TryValidateModel(entity))
            return BadRequest(ModelState);

        await _db.SaveChangesAsync();
        return Ok(entity);
    }

    private async Task ClearOtherMainCurrenciesAsync(bool isMainCurrency, int? currentCurrencyId = null)
    {
        if (!isMainCurrency)
            return;

        var query = _db.Currencies.Where(c => c.IsMainCurrency);
        if (currentCurrencyId.HasValue)
            query = query.Where(c => c.ID != currentCurrencyId.Value);

        var otherMains = await query.ToListAsync();
        foreach (var currency in otherMains)
            currency.IsMainCurrency = false;
    }

    private static void ApplyValues(Currency entity, string values)
    {
        DevExtremeFormValueMapper.Apply(
            values,
            FormValueSetter.String(nameof(Currency.CurrencyName), value => entity.CurrencyName = value, trim: true),
            FormValueSetter.String(nameof(Currency.CurrencySymbole), value => entity.CurrencySymbole = value, trim: true),
            FormValueSetter.Boolean(nameof(Currency.IsMainCurrency), value => entity.IsMainCurrency = value),
            FormValueSetter.Boolean(nameof(Currency.IsActive), value => entity.IsActive = value));
    }
}

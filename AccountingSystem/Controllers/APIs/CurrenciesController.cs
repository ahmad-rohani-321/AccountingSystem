using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models.Settings;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CurrenciesController(IHttpContextAccessor accessor, ApplicationDbContext db) : ControllerBase
    {
        private readonly IHttpContextAccessor _accessor = accessor;
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
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var entity = new Currency
            {
                CreationDate = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            ApplyValues(entity, values);

            if (entity.IsMainCurrency)
            {
                var otherMains = await _db.Currencies.Where(c => c.IsMainCurrency).ToListAsync();
                foreach (var c in otherMains)
                    c.IsMainCurrency = false;
            }

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

            if (entity.IsMainCurrency)
            {
                var otherMains = await _db.Currencies.Where(c => c.ID != key && c.IsMainCurrency).ToListAsync();
                foreach (var c in otherMains)
                    c.IsMainCurrency = false;
            }

            if (!TryValidateModel(entity))
                return BadRequest(ModelState);

            await _db.SaveChangesAsync();
            return Ok(entity);
        }

        private string GetUserId()
        {
            var principal = _accessor.HttpContext?.User ?? User;
            return principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private static void ApplyValues(Currency entity, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return;

            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(values);
            if (dict is null || dict.Count == 0)
                return;

            if (dict.TryGetValue(nameof(Currency.CurrencyName), out var nameEl) && nameEl.ValueKind != JsonValueKind.Null)
                entity.CurrencyName = (nameEl.GetString() ?? string.Empty).Trim();

            if (dict.TryGetValue(nameof(Currency.CurrencySymbole), out var symEl) && symEl.ValueKind != JsonValueKind.Null)
                entity.CurrencySymbole = (symEl.GetString() ?? string.Empty).Trim();

            if (dict.TryGetValue(nameof(Currency.IsMainCurrency), out var mainEl) && mainEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
                entity.IsMainCurrency = mainEl.GetBoolean();

            if (dict.TryGetValue(nameof(Currency.IsActive), out var activeEl) && activeEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
                entity.IsActive = activeEl.GetBoolean();
        }
    }
}

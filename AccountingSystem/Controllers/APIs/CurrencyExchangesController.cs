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
    public class CurrencyExchangesController(IHttpContextAccessor accessor, ApplicationDbContext db) : ControllerBase
    {
        private readonly IHttpContextAccessor _accessor = accessor;
        private readonly ApplicationDbContext _db = db;

        [HttpGet]
        public async Task<object> Get(DataSourceLoadOptions loadOptions)
        {
            var data = await _db.CurrencyExchanges
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

            var entity = new CurrencyExchange
            {
                CreationDate = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            ApplyValues(entity, values);

            if (entity.MainCurrencyID <= 0 || entity.SubCurrencyID <= 0)
                return BadRequest("اصلي اسعار او فرعي اسعار ضروری دي.");

            if (entity.MainCurrencyID == entity.SubCurrencyID)
                return BadRequest("اصلي اسعار او فرعي اسعار باید یو شان نه وي.");

            var currencyExists = await _db.Currencies.AnyAsync(c => c.ID == entity.MainCurrencyID)
                && await _db.Currencies.AnyAsync(c => c.ID == entity.SubCurrencyID);
            if (!currencyExists)
                return BadRequest("یوه یا زیاتې اسعار ونه موندل شوې.");

            // The UI edits exchange rate only. Keep amounts consistent and always create a new record.
            if (entity.MainCurrencyAmount <= 0)
                entity.MainCurrencyAmount = 1m;

            if (entity.CurrencyExchangeRate > 0 && entity.SubCurrencyAmount <= 0)
                entity.SubCurrencyAmount = entity.CurrencyExchangeRate * entity.MainCurrencyAmount;

            if (entity.SubCurrencyAmount > 0 && entity.CurrencyExchangeRate <= 0)
                entity.CurrencyExchangeRate = entity.SubCurrencyAmount / entity.MainCurrencyAmount;

            if (entity.CurrencyExchangeRate <= 0)
                return BadRequest("نرخ باید له 0 څخه زیات وي.");

            _db.CurrencyExchanges.Add(entity);

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

        private static void ApplyValues(CurrencyExchange entity, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return;

            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(values);
            if (dict is null || dict.Count == 0)
                return;

            if (dict.TryGetValue(nameof(CurrencyExchange.MainCurrencyID), out var mainIdEl) && mainIdEl.ValueKind != JsonValueKind.Null)
            {
                if (mainIdEl.TryGetInt32(out var mainId))
                    entity.MainCurrencyID = mainId;
            }

            if (dict.TryGetValue(nameof(CurrencyExchange.SubCurrencyID), out var subIdEl) && subIdEl.ValueKind != JsonValueKind.Null)
            {
                if (subIdEl.TryGetInt32(out var subId))
                    entity.SubCurrencyID = subId;
            }

            if (dict.TryGetValue(nameof(CurrencyExchange.MainCurrencyAmount), out var mainAmtEl) && mainAmtEl.ValueKind != JsonValueKind.Null)
            {
                if (mainAmtEl.TryGetDecimal(out var mainAmt))
                    entity.MainCurrencyAmount = mainAmt;
            }

            if (dict.TryGetValue(nameof(CurrencyExchange.SubCurrencyAmount), out var subAmtEl) && subAmtEl.ValueKind != JsonValueKind.Null)
            {
                if (subAmtEl.TryGetDecimal(out var subAmt))
                    entity.SubCurrencyAmount = subAmt;
            }

            if (dict.TryGetValue(nameof(CurrencyExchange.CurrencyExchangeRate), out var rateEl) && rateEl.ValueKind != JsonValueKind.Null)
            {
                if (rateEl.TryGetDecimal(out var rate))
                    entity.CurrencyExchangeRate = rate;
            }
        }
    }
}

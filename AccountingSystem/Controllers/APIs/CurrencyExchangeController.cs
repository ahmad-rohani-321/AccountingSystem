using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
    public class CurrencyExchangeController(IHttpContextAccessor accessor, ApplicationDbContext db) : ControllerBase
    {
        private readonly IHttpContextAccessor _accessor = accessor;
        private readonly ApplicationDbContext _db = db;

        [HttpGet]
        public async Task<object> Get(DataSourceLoadOptions loadOptions)
        {

            var newQuery = _db.Currencies.ToArray()
                .Select(c => new CurrencyExchangeVM
                {
                    SubCurrencyID = c.ID,
                    SubCurrencyName = c.CurrencyName,
                    SubCurrencyAmount = 0m,
                    MainCurrencyAmount = 0m,
                    ExchangeRate = 0m
                })
                .ToList();
            foreach (var currency in newQuery)
            {
                var exchange = await _db.CurrencyExchanges
                    .Where(e => e.SubCurrencyID == currency.SubCurrencyID)
                    .OrderByDescending(e => e.CreationDate)
                    .FirstOrDefaultAsync();

                if (exchange != null)
                {
                    currency.MainCurrencyAmount = exchange.MainCurrencyAmount;
                    currency.SubCurrencyAmount = exchange.SubCurrencyAmount;
                    currency.ExchangeRate = exchange.CurrencyExchangeRate;
                }
            }
            return DataSourceLoader.Load(newQuery, loadOptions);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] IEnumerable<CurrencyExchangeVM> payload)
        {
            var userId = GetUserId();
            var mainCurrency = await _db.Currencies.FirstOrDefaultAsync(c => c.IsMainCurrency);

            var newEntry = payload.Select(item => new CurrencyExchange
            {
                MainCurrencyID = mainCurrency.ID,
                SubCurrencyID = item.SubCurrencyID,
                MainCurrencyAmount = item.MainCurrencyAmount,
                SubCurrencyAmount = item.SubCurrencyAmount,
                CurrencyExchangeRate = item.MainCurrencyAmount != 0 ? (item.SubCurrencyAmount / item.MainCurrencyAmount) : 0,
                CreationDate = DateTime.UtcNow,
                CreatedByUserId = userId
            }).ToList();

            await _db.CurrencyExchanges.AddRangeAsync(newEntry);
            await _db.SaveChangesAsync();

            return Ok();
        }

        private string GetUserId()
        {
            var principal = _accessor.HttpContext?.User ?? User;
            return principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}

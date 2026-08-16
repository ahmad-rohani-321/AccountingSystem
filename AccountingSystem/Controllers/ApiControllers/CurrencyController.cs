using AccountingSystem.Data;
using AccountingSystem.Models.Inventory;
using AccountingSystem.Models.Settings;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Security.Claims;

namespace AccountingSystem.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CurrencyController(ApplicationDbContext context, IHttpContextAccessor accessor) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IHttpContextAccessor _contextAccessor = accessor;

        [HttpGet("Currencies")]
        public async Task<ActionResult<List<CurrencyDetailedViewModel>>> Currencies()
        {
            var currencies = (await _context.Currencies.ToArrayAsync()).Select(x => new CurrencyDetailedViewModel()
            {
                CurrencyId = x.ID,
                CurrencyName = x.CurrencyName,
                CurrencySymbole = x.CurrencySymbole,
                IsMainCurrency = x.IsMainCurrency,
                IsActive = x.IsActive
            }).ToList();
            return Ok(currencies);
        }

        [HttpGet("Currencies/Simple")]
        public async Task<ActionResult<List<CurrencyViewModel>>> CurrencySimpleList()
        {
            var currencies = (await _context.Currencies.Where(x => x.IsActive).OrderByDescending(z => z.IsMainCurrency).ToArrayAsync())
                    .Select(x => new CurrencyViewModel()
                    {
                        Id = x.ID,
                        Name = x.CurrencyName,
                        Symbole = x.CurrencySymbole
                    }).ToList();
            return Ok(currencies);
        }

        [HttpPost("Create")]
        public async Task<ActionResult> Add(CurrencyViewModel currency)
        {
            string user = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (currency.Name == null || currency.Name.Length == 0)
            {
                return BadRequest("د اسعارو نوم ولیکئ.");
            }
            else if(currency.Symbole == null || currency.Symbole.Length == 0)
            {
                return BadRequest("د اسعارو نښه ولیکئ.");
            }
            else if (_context.Currencies.Any(x => x.CurrencyName == currency.Name))
            {
                return BadRequest("دغه اسعار موجود دي.");
            }
            else if (_context.Currencies.Any(x => x.CurrencySymbole == currency.Symbole))
            {
                return BadRequest("دغه د اسعارو نښه موجود ده.");
            }
            else
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        await _context.Currencies.AddAsync(new Models.Settings.Currency()
                        {
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            CurrencyName = currency.Name,
                            CurrencySymbole = currency.Symbole,
                            IsActive = true,
                            IsMainCurrency = false
                        });

                        await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                        {
                            CreatedByUserId = user,
                            ModelName = "اسعار",
                            Details = $"په {currency.Name} نوم اسعار ثبت سوه.",
                            CreationDate = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Created();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(ex.Message);
                    }
                }
            }
        }

        [HttpPut("ChangeActivation/{id}")]
        public async Task<ActionResult> ChangeActivation(int id)
        {
            string user = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var currency = await _context.Currencies.FirstOrDefaultAsync(x => x.ID == id);
            if (currency == null)
            {
                return BadRequest("دغه اسعار موجود نه دی.");
            }
            else
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        currency.IsActive = !currency.IsActive;
                        _context.Currencies.Update(currency);
                        await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                        {
                            CreatedByUserId = user,
                            ModelName = "اسعار",
                            Details = $"د {currency.CurrencyName} اسعار حالت بدل سو.",
                            CreationDate = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Ok();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(ex.Message);
                    }
                }
            }
        }

        [HttpPut("Update")]
        public async Task<ActionResult> Update(CurrencyViewModel currency)
        {
            string user = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (currency.Id == 0)
            {
                return BadRequest("انتخاب سوي اسعار ته دي موجود.");
            }
            else if (currency.Name == null || currency.Name.Length == 0)
            {
                return BadRequest("د اسعارو نوم ولیکئ.");
            }
            else if (currency.Symbole == null || currency.Symbole.Length == 0)
            {
                return BadRequest("د اسعارو نښه ولیکئ.");
            }
            else if (_context.Currencies.Any(x => x.CurrencyName == currency.Name && x.ID != currency.Id))
            {
                return BadRequest("دغه اسعار موجود دي.");
            }
            else if (_context.Currencies.Any(x => x.CurrencySymbole == currency.Symbole && x.ID != currency.Id))
            {
                return BadRequest("دغه د اسعارو نښه موجود ده.");
            }
            else
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var getCurrency = await _context.Currencies.FindAsync(currency.Id);
                        getCurrency.CurrencyName = currency.Name;
                        getCurrency.CurrencySymbole = currency.Symbole;

                        await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                        {
                            CreatedByUserId = user,
                            ModelName = "اسعار",
                            Details = $"په {currency.Name} نوم اسعار تغیر سوه.",
                            CreationDate = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Ok();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(ex.Message);
                    }
                }
            }
        }

        [HttpPut("ChangeToMain/{id}")]
        public async Task<ActionResult> ChangeToMain(int id)
        {
            string user = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var currency = await _context.Currencies.FindAsync(id);
            if (currency == null)
            {
                return BadRequest("انتخاب سوي اسعار نه دي موجود.");
            }
            else if (!currency.IsActive)
            {
                return BadRequest("انتخاب سوي اسعار غیر فعال دي.");
            }
            else
            {
                using(var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var currencies = _context.Currencies.Where(c => c.IsActive);
                        foreach (var item in currencies)
                        {
                            item.IsMainCurrency = false;
                        }
                        currency.IsMainCurrency = true;
                        await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                        {
                            CreatedByUserId = user,
                            ModelName = "اسعار",
                            Details = $"په {currency.CurrencyName} نوم اسعار عمومي سوه.",
                            CreationDate = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Ok();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(ex.Message);
                    }
                }
            }
        }

        // convert prices
        [HttpGet("CurrencyConversion/Load")]
        public async Task<ActionResult<CurrencyConversionViewModel>> GetCurrencies()
        {
            var currencies = await _context.Currencies.Where(x => x.IsActive).ToArrayAsync();
            List<CurrencyConversionViewModel> listCurrencies = new();
            foreach(var currency in currencies)
            {
                var currencyPrice = await _context.CurrencyExchanges.OrderByDescending(z => z.ID).FirstOrDefaultAsync(x => x.SubCurrencyID == currency.ID);
                listCurrencies.Add(
                    new CurrencyConversionViewModel()
                    {
                        CurrencyId = currency.ID,
                        MainCurrencyPrice = currencyPrice == null ? 1 : currencyPrice.MainCurrencyAmount,
                        SubCurrencyPrice = currencyPrice == null ? 1 : currencyPrice.SubCurrencyAmount,
                        SubCurrencyName = currency.CurrencyName
                    }
                );
            }

            return Ok(listCurrencies);
        }


        // convert prices
        [HttpGet("GetSingleCurrencyConversion/{currencyId}")]
        public async Task<ActionResult> GetSingleCurrencyConversion(int currencyId)
        {
            var currencyPrice = await _context.CurrencyExchanges.OrderByDescending(z => z.ID).FirstOrDefaultAsync(x => x.SubCurrencyID == currencyId);
            var conversion = new CurrencyConversionViewModel()
            {
                CurrencyId = currencyPrice == null ? 0 : currencyPrice.ID,
                MainCurrencyPrice = currencyPrice == null ? 1 : currencyPrice.MainCurrencyAmount,
                SubCurrencyPrice = currencyPrice == null ? 1 : currencyPrice.SubCurrencyAmount,
                ExchangedAmount = currencyPrice == null ? 1 : currencyPrice.CurrencyExchangeRate
            };

            return Ok(conversion);
        }


        [HttpPost("CurrencyConversion/Create")]
        public async Task<ActionResult> SaveExchanges(List<CurrencyConversionViewModel> currencyConversions)
        {            
            string user = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (currencyConversions.Any(a => a.MainCurrencyPrice == 0 || a.SubCurrencyPrice == 0))
            {
                return BadRequest("د اسعارو قیمتونه باید له ضفر څخه پورته وي.");
            }
            else if (currencyConversions.Any(a => a.CurrencyId == 0))
            {
                return BadRequest("بیا ځلي کوښښ وکړئ.");
            }
            else if (currencyConversions == null || currencyConversions.Count == 0)
            {
                return BadRequest("هیله ده لومړی اسعار ثبت کړئ.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var mainCurrency = await _context.Currencies.FirstOrDefaultAsync(x => x.IsMainCurrency);
                    var conversions = currencyConversions.Select(c => new CurrencyExchange()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        SubCurrencyID = c.CurrencyId,
                        MainCurrencyID = mainCurrency.ID,
                        MainCurrencyAmount = c.MainCurrencyPrice,
                        SubCurrencyAmount = c.SubCurrencyPrice,
                        CurrencyExchangeRate = c.SubCurrencyPrice / c.MainCurrencyPrice
                    }).ToList();
                    await _context.CurrencyExchanges.AddRangeAsync(conversions);

                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        ModelName = "د اسعارو تبادله",
                        Details = "د اسعارو نوي قیمتونه ثبت سوه.",
                        CreationDate = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Ok();
                }
                catch (System.Exception ex)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(ex.Message);
                }
            }
        }
    }
}

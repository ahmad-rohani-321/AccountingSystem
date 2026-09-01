using AccountingSystem.Data;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AccountingSystem.Controllers.ApiControllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class SaleController(ApplicationDbContext context, IHttpContextAccessor accessor) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly IHttpContextAccessor _accessor = accessor;



    [HttpGet("Next-No")]
    public async Task<IActionResult> GetNextNo()
    {
        var lastSaleNo = await _context.Sales
            .Select(s => (int?)s.SaleNo)
            .MaxAsync() ?? 0;

        return Ok(new { SaleNo = lastSaleNo + 1 });
    }

    [HttpPost("SaveNewSale")]
    public async Task<IActionResult> SaveNewSale(SaleViewModel request)
    {
        if (request == null || request.SaleDetails == null || request.SaleDetails.Count == 0)
        {
            return BadRequest("خالي فروش نه ثبت کیږي.");
        }
        else if (request.SaleNo <= 0 || request.SaleTotal < 0 || request.SaleRecieved < 0 ||
                 request.SaleRecieved > request.SaleTotal ||
                 request.SaleDetails.Any(x => x.ItemId <= 0 || x.UnitId <= 0 || x.StockId <= 0 ||
                                              x.Quantity <= 0 || x.PerPrice < 0 ||
                                              x.TotalPrice != x.PerPrice * x.Quantity))
        {
            return BadRequest("د فروش معلومات ناسم دي.");
        }
        else if (request.SaleRecieved > 0 && request.BankId == 0)
        {
            return BadRequest("د رسيد مبلغ لپاره بانک انتخاب کړئ.");
        }
        else if (request.BankId != 0 && !await _context.Accounts.AnyAsync(x => x.ID == request.BankId))
        {
            return BadRequest("ناسم بانک انتخاب سوی دی");
        }
        else if (!await _context.Accounts.AnyAsync(x => x.ID == request.PersonId))
        {
            return BadRequest("ناسم شخص انتخاب سوی دی");
        }
        else if (!await _context.Currencies.AnyAsync(x => x.ID == request.CurrencyId))
        {
            return BadRequest("ناسم اسعار انتخاب سوی دی");
        }
        else if (await _context.Sales.AnyAsync(x => x.SaleNo == request.SaleNo))
        {
            return BadRequest("ټاکل سوې د فروش شمېره تکراري ده");
        }
        else if (request.SaleTotal != request.SaleDetails.Sum(x => x.TotalPrice))
        {
            return BadRequest("د فروش مجموعه ناسم محاسبه سوې ده");
        }
        else if (await _context.Items.CountAsync(x => request.SaleDetails.Select(i => i.ItemId).Distinct().Contains(x.ID))
                 != request.SaleDetails.Select(i => i.ItemId).Distinct().Count())
        {
            return BadRequest("هیله ده د فروش اجناس اصلاح کړئ");
        }
        else if (await _context.UnitConversion.CountAsync(u =>
                            request.SaleDetails.Select(x => x.UnitId).Distinct().Contains(u.ID) &&
                            request.SaleDetails.Any(x => x.UnitId == u.ID && x.ItemId == u.ItemID))
                 != request.SaleDetails.Select(x => x.UnitId).Distinct().Count())
        {
            return BadRequest("هیله ده واحدونه اصلاح کړئ!");
        }
        else if (await _context.WareHouses.CountAsync(s => request.SaleDetails.Select(w => w.StockId).Distinct().Contains(s.ID))
                 != request.SaleDetails.Select(w => w.StockId).Distinct().Count())
        {
            return BadRequest("هیله ده ګدامونه اصلاح کړی!");
        }
        else
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                DateTime date = request.SaleDate == DateTime.Now.Date ? DateTime.Now : request.SaleDate;
                var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                string remarks = $"فروش نمبر: {request.SaleNo} | {request.Remarks}";
                var sale = await _context.Sales.AddAsync(new Models.Sales.Sales()
                {
                    AccountID = request.PersonId,
                    CanAffectStock = request.EffectsStock,
                    CreatedByUserId = user,
                    CreationDate = date,
                    CurrencyID = request.CurrencyId,
                    IsHolded = request.IsHolded,
                    IsRefunded = false,
                    SaleNo = request.SaleNo,
                    Remarks = request.Remarks,
                    TotalAmount = request.SaleTotal,
                    ReceivedAmount = request.SaleRecieved,
                    RemainingAmount = request.SaleTotal - request.SaleRecieved
                });
                await _context.SaveChangesAsync();
                if (!request.IsHolded)
                {
                    var personAccount = await _context.AccountBalances.FirstOrDefaultAsync(x => x.AccountID == request.PersonId && x.CurrencyID == request.CurrencyId);
                    if (personAccount == null)
                    {
                        var balance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance()
                        {
                            AccountID = request.PersonId,
                            CreatedByUserId = user,
                            CreationDate = date,
                            CurrencyID = request.CurrencyId,
                            Balance = 0
                        });
                        await _context.SaveChangesAsync();
                        personAccount = balance.Entity;
                    }
                    personAccount.Balance += request.SaleTotal;
                    await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                    {
                        AccountBalanceID = personAccount.ID,
                        Balance = personAccount.Balance,
                        Credit = request.SaleTotal,
                        CreatedByUserId = user,
                        CreationDate = date,
                        Remarks = remarks,
                        TransactionTypeID = 5
                    });
                    if (request.SaleRecieved > 0)
                    {
                        personAccount.Balance -= request.SaleRecieved;
                        await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                        {
                            AccountBalanceID = personAccount.ID,
                            Balance = personAccount.Balance,
                            Debit = request.SaleRecieved,
                            CreatedByUserId = user,
                            CreationDate = date,
                        Remarks = remarks,
                            TransactionTypeID = 5
                        });

                        var treasureAccount = await _context.AccountBalances.FirstOrDefaultAsync(x => x.AccountID == request.BankId && x.CurrencyID == request.CurrencyId);
                        if (treasureAccount == null)
                        {
                            var account = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance()
                            {
                                AccountID = request.BankId,
                                Balance = 0,
                                CreatedByUserId = user,
                                CreationDate = date,
                                CurrencyID = request.CurrencyId
                            });
                            await _context.SaveChangesAsync();
                            treasureAccount = account.Entity;
                        }
                        treasureAccount.Balance += request.SaleRecieved;
                        await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                        {
                            AccountBalanceID = treasureAccount.ID,
                            Balance = treasureAccount.Balance,
                            Credit = request.SaleRecieved,
                            CreatedByUserId = user,
                            CreationDate = date,
                        Remarks = remarks,
                            TransactionTypeID = 5
                        });
                    }
                    await _context.SaveChangesAsync();
                }
                foreach (var item in request.SaleDetails)
                {
                    await _context.SalesDetails.AddAsync(new Models.Sales.SaleDetails()
                    {
                        CreatedByUserId = user,
                        CreationDate = date,
                        ItemID = item.ItemId,
                        PerPrice = item.PerPrice,
                        SaleID = sale.Entity.ID,
                        Quantity = item.Quantity,
                        TotalPrice = item.TotalPrice,
                        UnitConversionID = item.UnitId,
                        WarehouseID = item.StockId, 
                    });
                    if (!request.IsHolded && request.EffectsStock)
                    {
                        var stock = await _context.StockBalances.FirstOrDefaultAsync(x => x.ItemID == item.ItemId && x.WarehouseID == item.StockId);
                        var unitExchange = await _context.UnitConversion.FirstOrDefaultAsync(x => x.ID == item.UnitId);
                        if (unitExchange == null || unitExchange.ItemID != item.ItemId || unitExchange.ExchangedAmount <= 0)
                        {
                            return BadRequest("د واحد د تبدیل معلومات ناسم دي.");
                        }

                        decimal realStock = item.Quantity / unitExchange.ExchangedAmount;
                        if (stock == null || stock.Quantity < realStock)
                        {
                            return BadRequest("د فروش لپاره په ګدام کې کافي موجودي نسته.");
                        }

                        stock.Quantity -= realStock;
                        await _context.StockTransactions.AddAsync(new Models.Inventory.StockTransactions()
                        {
                            CreatedByUserId = user,
                            CreationDate = date,
                            Quantity = item.Quantity,
                            StockBalanceID = stock.ID,
                            TransactionID = 7,
                            Remarks = item.Remarks,
                            UnitID = item.UnitId
                        });
                        await _context.SaveChangesAsync();
                    }
                    await _context.SaveChangesAsync();
                }
                await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                {
                    CreatedByUserId = user,
                    CreationDate = DateTime.Now,
                    Details = $"په {request.SaleNo} شمېره نوی فروش ثبت سو.",
                    ModelName = "فروش"
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

    [HttpPost("EditSale")]
    public async Task<IActionResult> EditSale(SaleViewModel request)
    {
        return Ok();
    }

    [HttpGet("GetSaleById/{id}")]
    public async Task<ActionResult> GetSaleById(int? id)
    {
        if (id.HasValue && id.Value <= 0)
        {
            return BadRequest("ناسم فروش شمېره");
        }
        else if (!await _context.Sales.AnyAsync(x => x.ID == id.Value))
        {
            return NotFound("فروش ونه موندل سو");
        }
        else if (!(await _context.Sales.FindAsync(id.Value))?.IsHolded ?? false)
        {
            return NotFound("انتخاب سوی فروش نه دی ساتل سوی، د تغیر وړ نه دی");
        }
        else
        {
            var sale = await _context.Sales.FindAsync(id);
            var saleDetails = await _context.SalesDetails
                .Where(pd => pd.SaleID == id)
                .ToListAsync();
            var saleViewModel = new SaleViewModel
            {
                SaleId = sale.ID,
                SaleNo = sale.SaleNo,
                PersonId = sale.AccountID,
                CurrencyId = sale.CurrencyID,
                SaleTotal = sale.TotalAmount,
                SaleRecieved = sale.ReceivedAmount,
                SaleRemaining = sale.RemainingAmount,
                Remarks = sale.Remarks,
                SaleDate = sale.CreationDate,
                EffectsStock = sale.CanAffectStock,
                IsHolded = sale.IsHolded,
                SaleDetails = saleDetails.Select(pd => new SaleDetailsViewModel
                {
                    Id = pd.ID,
                    ItemId = pd.ItemID,
                    UnitId = pd.UnitConversionID,
                    StockId = pd.WarehouseID,
                    Quantity = pd.Quantity,
                    PerPrice = pd.PerPrice,
                    TotalPrice = pd.TotalPrice,
                    Remarks = pd.Remarks
                }).ToList()
            };
            return Ok(saleViewModel);
        }
    }

    [HttpDelete("DeleteSale/{id}")]
    public async Task<ActionResult> DeleteSale(int? id)
    {
        var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
        if (id.HasValue && id == 0)
        {
            return BadRequest("فروش نه دی انتخاب سوی");
        }
        else if (!await _context.Sales.AnyAsync(x => x.ID == id))
        {
            return BadRequest("صحیح فروش انتخاب سوی");
        }
        else if (!await _context.Sales.AnyAsync(x => x.ID == id && x.IsHolded && !x.CanAffectStock))
        {
            return BadRequest("فروش ساتل سوی او په ګدام یې تاثیر سوی دی");
        }
        else
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sale = await _context.Sales.FindAsync(id);
                var details = await _context.SalesDetails.Where(x => x.SaleID == id).ToListAsync();
                _context.SalesDetails.RemoveRange(details);
                _context.Sales.Remove(sale);
                await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                {
                    CreatedByUserId = user,
                    CreationDate = DateTime.Now,
                    Details = $"د {sale.SaleNo} شمېره فروش حذف سو.",
                    ModelName = "فروش"
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

    [HttpPost("SalePayment")]
    public async Task<ActionResult> SalePayment(PaymentRequest request)
    {
        if (request == null)
        {
            return BadRequest("د تادیې معلومات اړین دي.");
        }
        else if (!await _context.Sales.AnyAsync(x => x.ID == request.Id))
        {
            return BadRequest("فروش انتخاب کړئ");
        }
        else if (request.RecieveAmount <= 0)
        {
            return BadRequest("رسید مبلغ باید له صفر څخه لوړ وي");
        }
        else if (!await _context.Accounts.AnyAsync(x => x.ID == request.FeesSource && x.IsActive))
        {
            return BadRequest("صحیح دخل/بانک انتخاب کړئ");
        }
        else
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sale = await _context.Sales.FirstAsync(x => x.ID == request.Id);
                var newReceivedAmount = sale.ReceivedAmount + request.RecieveAmount;
                if (newReceivedAmount > sale.TotalAmount)
                {
                    return BadRequest("رسید مبلغ باید د فروش د مجموعې څخه لوړ نه وي");
                }

                var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                var salerBalance = await _context.AccountBalances
                    .FirstOrDefaultAsync(x => x.AccountID == sale.AccountID && x.CurrencyID == sale.CurrencyID);
                if (salerBalance == null)
                {
                    var balance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance
                    {
                        AccountID = sale.AccountID,
                        CurrencyID = sale.CurrencyID,
                        Balance = 0,
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now
                    });
                    salerBalance = balance.Entity;
                }

                var feesSourceBalance = await _context.AccountBalances
                    .FirstOrDefaultAsync(x => x.AccountID == request.FeesSource && x.CurrencyID == sale.CurrencyID);
                if (feesSourceBalance == null)
                {
                    var balance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance
                    {
                        AccountID = request.FeesSource,
                        CurrencyID = sale.CurrencyID,
                        Balance = 0,
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now
                    });
                    feesSourceBalance = balance.Entity;
                }

                await _context.SaveChangesAsync();

                feesSourceBalance.Balance += request.RecieveAmount;
                salerBalance.Balance -= request.RecieveAmount;
                sale.ReceivedAmount = newReceivedAmount;
                sale.RemainingAmount = sale.TotalAmount - newReceivedAmount;

                var remarks = $"فروش نمبر {sale.SaleNo} رسید| {request.Description}";
                await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry
                {
                    AccountBalanceID = feesSourceBalance.ID,
                    Balance = feesSourceBalance.Balance,
                    Credit = request.RecieveAmount,
                    CreatedByUserId = user,
                    CreationDate = DateTime.Now,
                    Remarks = remarks,
                    TransactionTypeID = 5
                });
                await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry
                {
                    AccountBalanceID = salerBalance.ID,
                    Balance = salerBalance.Balance,
                    Debit = request.RecieveAmount,
                    CreatedByUserId = user,
                    CreationDate = DateTime.Now,
                    Remarks = remarks,
                    TransactionTypeID = 5
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

    [HttpPost("SaleRefund")]
    public async Task<ActionResult> SaleRefund([FromBody] PaymentRequest request)
    {
        if (request == null)
        {
            return BadRequest("خالي فروش واپسي نه کیږي");
        }
        else if (!await _context.Sales.AnyAsync(x => x.ID == request.Id))
        {
            return BadRequest("ټاکل سوی فروش موجود نه دی.");
        }
        else if (!await _context.Accounts.AnyAsync(x => x.ID == request.FeesSource && x.IsActive) && request.RecieveAmount > 0)
        {
            return BadRequest("یو صحیح فعال بانک/نغدي حساب انتخاب کړئ.");
        }
        else if (!await _context.Sales.AnyAsync(x => x.ID == request.Id && !x.IsRefunded))
        {
            return BadRequest("دا فروش مخکي واپس سوی دی.");
        }
        else if (!await _context.Sales.AnyAsync(x => x.ID == request.Id && x.ReceivedAmount >= request.RecieveAmount))
        {
            return BadRequest("د واپسۍ لپاره د ورکړل سوي مبلغ باید د فروش د رسید مبلغ څخه زیات نه وي.");
        }
        else
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sale = await _context.Sales.FirstOrDefaultAsync(x => x.ID == request.Id);

                var remarks = $"فروش نمبر {sale.SaleNo} | {request.Description}";

                if (request.RecieveAmount > sale.ReceivedAmount)
                {
                    return BadRequest("د واپسۍ مبلغ د ورکړل سوي مبلغ څخه زیاتېدلای نه سي.");
                }
                if (sale.IsRefunded)
                {
                    return BadRequest("دا فروش مخکي واپس سوی دی.");
                }

                var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

                if (!sale.IsHolded)
                {
                    var salerBalance = await _context.AccountBalances
                    .FirstOrDefaultAsync(x => x.AccountID == sale.AccountID && x.CurrencyID == sale.CurrencyID);

                    if (salerBalance == null)
                    {
                        var balance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance
                        {
                            AccountID = sale.AccountID,
                            CurrencyID = sale.CurrencyID,
                            Balance = 0,
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now
                        });

                        await _context.SaveChangesAsync();

                        salerBalance = balance.Entity;
                    }

                    var feesSourceBalance = await _context.AccountBalances
                        .FirstOrDefaultAsync(x => x.AccountID == request.FeesSource && x.CurrencyID == sale.CurrencyID);

                    if (feesSourceBalance == null && request.RecieveAmount > 0)
                    {
                        var balance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance
                        {
                            AccountID = request.FeesSource,
                            CurrencyID = sale.CurrencyID,
                            Balance = 0,
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now
                        });

                        await _context.SaveChangesAsync();

                        feesSourceBalance = balance.Entity;
                    }

                    salerBalance.Balance -= sale.TotalAmount;

                    await _context.SaveChangesAsync();

                    await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry
                    {
                        AccountBalanceID = salerBalance.ID,
                        Balance = salerBalance.Balance,
                        Debit = sale.TotalAmount,
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Remarks = remarks,
                        TransactionTypeID = 9
                    });

                    await _context.SaveChangesAsync();

                    if (request.RecieveAmount > 0)
                    {
                        salerBalance.Balance += request.RecieveAmount;

                        await _context.SaveChangesAsync();

                        await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry
                        {
                            AccountBalanceID = salerBalance.ID,
                            Balance = salerBalance.Balance,
                            Credit = request.RecieveAmount,
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            Remarks = remarks,
                            TransactionTypeID = 9
                        });

                        await _context.SaveChangesAsync();

                        if (feesSourceBalance != null)
                        {
                            feesSourceBalance.Balance -= request.RecieveAmount;

                            await _context.SaveChangesAsync();

                            await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry
                            {
                                AccountBalanceID = feesSourceBalance.ID,
                                Balance = feesSourceBalance.Balance,
                                Debit = request.RecieveAmount,
                                CreatedByUserId = user,
                                CreationDate = DateTime.Now,
                                Remarks = remarks,
                                TransactionTypeID = 9
                            });

                            await _context.SaveChangesAsync();
                        }
                    }
                    sale.RemainingAmount = sale.TotalAmount - sale.ReceivedAmount;

                    sale.IsRefunded = true;

                    await _context.SaveChangesAsync();

                }

                if (sale.CanAffectStock)
                {
                    var saleDetails = await _context.SalesDetails
                        .Where(x => x.SaleID == sale.ID)
                        .ToListAsync();

                    foreach (var detail in saleDetails)
                    {
                        var unit = await _context.UnitConversion
                            .FirstOrDefaultAsync(x => x.ID == detail.UnitConversionID);
                        if (unit == null || unit.ExchangedAmount == 0)
                        {
                            throw new InvalidOperationException("د فروش د واحد د تبدیل معلومات ناسم دي.");
                        }

                        var stock = await _context.StockBalances.FirstOrDefaultAsync(x =>
                            x.ItemID == detail.ItemID && x.WarehouseID == detail.WarehouseID);
                        if (stock == null)
                        {
                            throw new InvalidOperationException("د فروش اړوند د ګدام موجودي ونه موندل سوه.");
                        }

                        stock.Quantity += detail.Quantity / unit.ExchangedAmount;
                        await _context.StockTransactions.AddAsync(new Models.Inventory.StockTransactions
                        {
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            Quantity = detail.Quantity,
                            StockBalanceID = stock.ID,
                            TransactionID = 8,
                            Remarks = remarks,
                            UnitID = detail.UnitConversionID
                        });
                    }
                }

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

    [HttpGet("GetSaleList")]
    public async Task<ActionResult> GetSaleList(int? personId, int? currencyId, DateTime? startDate, DateTime? endDate)
    {
        var sales = _context.Sales.AsQueryable();

        if (personId.HasValue && personId.Value > 0)
            sales = sales.Where(x => x.AccountID == personId.Value);

        if (currencyId.HasValue && currencyId.Value > 0)
            sales = sales.Where(x => x.CurrencyID == currencyId.Value);

        if (startDate.HasValue)
            sales = sales.Where(x => x.CreationDate >= startDate.Value.Date);

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1);
            sales = sales.Where(x => x.CreationDate < endOfDay);
        }

        var result = await sales
            .OrderByDescending(x => x.CreationDate)
            .Select(x => new SaleViewModel
            {
                SaleId = x.ID,
                SaleNo = x.SaleNo,
                PersonId = x.AccountID,
                PersonName = x.Account.Name,
                CurrencyId = x.CurrencyID,
                CurrencyName = x.Currency.CurrencyName,
                SaleTotal = x.TotalAmount,
                SaleRecieved = x.ReceivedAmount,
                SaleRemaining = x.RemainingAmount,
                SaleItemsCount = _context.SalesDetails.Count(d => d.SaleID == x.ID),
                Remarks = x.Remarks,
                SaleDate = x.CreationDate,
                IsHolded = x.IsHolded,
                EffectsStock = x.CanAffectStock,
                IsRefunded = x.IsRefunded
            })
            .ToListAsync();

        return Ok(result);
    }
}

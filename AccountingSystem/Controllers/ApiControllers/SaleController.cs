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
                        TransactionTypeID = 5,
                        ChequePhoto = "default.png"
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
                            TransactionTypeID = 5,
                            ChequePhoto = "default.png"
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
                            TransactionTypeID = 5,
                            ChequePhoto = "default.png"
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
                            return BadRequest("د فروش لپاره په ګدام کې کافي موجودي نشته.");
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
        if (request?.SaleDetails == null || request.SaleId <= 0 || request.SaleNo <= 0 || request.SaleRecieved < 0 || request.SaleRecieved > request.SaleTotal || request.SaleTotal != request.SaleDetails.Sum(x => x.TotalPrice)) return BadRequest("د فروش معلومات ناسم دي.");
        var sale = await _context.Sales.FirstOrDefaultAsync(x => x.ID == request.SaleId);
        if (sale == null) return NotFound("ناسم فروش شمېره");
        if (!sale.IsHolded || !request.IsHolded || sale.IsRefunded) return BadRequest("یوازې ساتل سوی فروش د تغیر وړ دی.");
        if (await _context.Sales.AnyAsync(x => x.ID != sale.ID && x.SaleNo == request.SaleNo)) return BadRequest("ټاکل سوې د فروش شمېره تکراري ده.");
        if (!await _context.Accounts.AnyAsync(x => x.ID == request.PersonId) || !await _context.Currencies.AnyAsync(x => x.ID == request.CurrencyId) || (request.SaleRecieved > 0 && (request.BankId == 0 || !await _context.Accounts.AnyAsync(x => x.ID == request.BankId)))) return BadRequest("د حساب معلومات ناسم دي.");
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var date = request.SaleDate == DateTime.Now.Date ? DateTime.Now : request.SaleDate;
            var user = _accessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var oldDetails = await _context.SalesDetails.Where(x => x.SaleID == sale.ID).ToListAsync();
            sale.AccountID = request.PersonId; sale.CurrencyID = request.CurrencyId; sale.SaleNo = request.SaleNo; sale.Remarks = request.Remarks; sale.TotalAmount = request.SaleTotal; sale.ReceivedAmount = request.SaleRecieved; sale.RemainingAmount = request.SaleTotal - request.SaleRecieved; sale.IsHolded = request.IsHolded; sale.CanAffectStock = request.EffectsStock; sale.CreationDate = date;
            _context.SalesDetails.RemoveRange(oldDetails);
            foreach (var item in request.SaleDetails)
            {
                await _context.SalesDetails.AddAsync(new Models.Sales.SaleDetails { SaleID = sale.ID, ItemID = item.ItemId, WarehouseID = item.StockId, UnitConversionID = item.UnitId, Quantity = item.Quantity, PerPrice = item.PerPrice, TotalPrice = item.TotalPrice, Remarks = item.Remarks, CreatedByUserId = user, CreationDate = date });
            }
            await _context.UserHistories.AddAsync(new Models.Identity.UserHistory { CreatedByUserId = user, CreationDate = date, Details = $"د {request.SaleNo} شمېره فروش تغیر سو.", ModelName = "فروش" });
            await _context.SaveChangesAsync(); await transaction.CommitAsync(); return Ok();
        }
        catch (Exception ex) { await transaction.RollbackAsync(); return BadRequest(ex.Message); }
    }

    [HttpGet("GetPurchaseById/{id}")]
    public async Task<ActionResult> GetPurchaseById(int? id)
    {
        if (id.HasValue && id.Value <= 0)
        {
            return BadRequest("ناسم خرید شمېره");
        }
        else if (!await _context.Purchases.AnyAsync(x => x.ID == id.Value))
        {
            return NotFound("خرید ونه موندل سو");
        }
        else if (!(await _context.Purchases.FirstOrDefaultAsync(x => x.ID == id.Value)).IsHolded)
        {
            return NotFound("انتخاب سوی خرید نه دی ساتل سوی، د تغیر وړ نه دی");
        }
        else
        {
            var purchase = await _context.Purchases.FirstOrDefaultAsync(p => p.ID == id);
            var purchaseDetails = await _context.PurchaseDetails
                .Where(pd => pd.PurchaseID == id)
                .ToListAsync();
            var purchaseViewModel = new PurchaseViewModel
            {
                PurchaseId = purchase.ID,
                PurchaseNo = purchase.PurchaseNo,
                PersonId = purchase.AccountID,
                CurrencyId = purchase.CurrencyID,
                PurchaseTotal = purchase.TotalAmount,
                PurchaseRecieved = purchase.ReceivedAmount,
                PurchaseRemaining = purchase.RemainingAmount,
                Remarks = purchase.Remarks,
                PurchaseDate = purchase.CreationDate,
                EffectsStock = purchase.CanAffectStock,
                IsHolded = purchase.IsHolded,
                PurchaseDetails = purchaseDetails.Select(pd => new PurchaseDetailsViewModel
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
            return Ok(purchaseViewModel);
        }
    }

    [HttpDelete("DeletePurchase/{id}")]
    public async Task<ActionResult> DeletePurchase(int? id)
    {
        var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
        if (id.HasValue && id == 0)
        {
            return BadRequest("خرید نه دی انتخاب سوی");
        }
        else if (!await _context.Purchases.AnyAsync(x => x.ID == id))
        {
            return BadRequest("صحیح خرید انتخاب سوی");
        }
        else if (!await _context.Purchases.AnyAsync(x => x.ID == id && x.IsHolded && !x.CanAffectStock))
        {
            return BadRequest("خرید ساتل سوی او په ګدام یې تاثیر سوی دی");
        }
        else
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var purchase = await _context.Purchases.FindAsync(id);
                var details = await _context.PurchaseDetails.Where(x => x.PurchaseID == id).ToListAsync();
                _context.PurchaseDetails.RemoveRange(details);
                _context.Purchases.Remove(purchase);
                await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                {
                    CreatedByUserId = user,
                    CreationDate = DateTime.Now,
                    Details = $"د {purchase.PurchaseNo} شمېره خرید حذف سو.",
                    ModelName = "نقدي معاملات"
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

    [HttpPost("PurchasePayment")]
    public async Task<ActionResult> PurchasePayment(PurchasePaymentRequest request)
    {
        if (request == null)
        {
            return BadRequest("د تادیې معلومات اړین دي.");
        }

        var purchaseId = request.PurchaseId;
        var recieveAmount = request.RecieveAmount;
        var description = request.Description;
        var feesSource = request.FeesSource;

        if (!await _context.Purchases.AnyAsync(x => x.ID == purchaseId))
        {
            return BadRequest("خرید انتخاب کړئ");
        }
        else if (recieveAmount <= 0)
        {
            return BadRequest("رسید مبلغ باید له صفر څخه لوړ وي");
        }
        else if (!await _context.Accounts.AnyAsync(x => x.ID == feesSource && x.IsActive))
        {
            return BadRequest("صحیح دخل/بانک انتخاب کړئ");
        }
        else
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var purchase = await _context.Purchases.FirstAsync(x => x.ID == purchaseId);
                var newReceivedAmount = purchase.ReceivedAmount + recieveAmount;
                if (newReceivedAmount > purchase.TotalAmount)
                {
                    return BadRequest("رسید مبلغ نشي کولی ډیر وی ځانګړي خرید موندلو ته.");
                }

                var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                var purchaserBalance = await _context.AccountBalances
                    .FirstOrDefaultAsync(x => x.AccountID == purchase.AccountID && x.CurrencyID == purchase.CurrencyID);
                if (purchaserBalance == null)
                {
                    var balance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance
                    {
                        AccountID = purchase.AccountID,
                        CurrencyID = purchase.CurrencyID,
                        Balance = 0,
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now
                    });
                    purchaserBalance = balance.Entity;
                }

                var feesSourceBalance = await _context.AccountBalances
                    .FirstOrDefaultAsync(x => x.AccountID == feesSource && x.CurrencyID == purchase.CurrencyID);
                if (feesSourceBalance == null)
                {
                    var balance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance
                    {
                        AccountID = feesSource,
                        CurrencyID = purchase.CurrencyID,
                        Balance = 0,
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now
                    });
                    feesSourceBalance = balance.Entity;
                }

                await _context.SaveChangesAsync();

                feesSourceBalance.Balance -= recieveAmount;
                purchaserBalance.Balance += recieveAmount;
                purchase.ReceivedAmount = newReceivedAmount;
                purchase.RemainingAmount = purchase.TotalAmount - newReceivedAmount;

                var remarks = $"خرید نمبر {purchase.PurchaseNo} رسید| {description}";
                await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry
                {
                    AccountBalanceID = feesSourceBalance.ID,
                    Balance = feesSourceBalance.Balance,
                    Debit = recieveAmount,
                    Credit = 0,
                    CreatedByUserId = user,
                    CreationDate = DateTime.Now,
                    Remarks = remarks,
                    TransactionTypeID = 6
                });
                await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry
                {
                    AccountBalanceID = purchaserBalance.ID,
                    Balance = purchaserBalance.Balance,
                    Debit = 0,
                    Credit = recieveAmount,
                    CreatedByUserId = user,
                    CreationDate = DateTime.Now,
                    Remarks = remarks,
                    TransactionTypeID = 6
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

    [HttpPost("PurchaseRefund")]
    public async Task<ActionResult> PurchaseRefund([FromBody] PurchasePaymentRequest request)
    {
        if (request == null)
        {
            return BadRequest("خالي خرید واپسي نه کیږي");
        }
        else if (!await _context.Purchases.AnyAsync(x => x.ID == request.PurchaseId))
        {
            return BadRequest("ټاکل سوی خرید موجود نه دی.");
        }
        else if (!await _context.Accounts.AnyAsync(x => x.ID == request.FeesSource && x.IsActive) && request.RecieveAmount > 0)
        {
            return BadRequest("یو صحیح فعال بانک/نغدي حساب انتخاب کړئ.");
        }
        else if (!await _context.Purchases.AnyAsync(x => x.ID == request.PurchaseId && !x.IsRefunded))
        {
            return BadRequest("دا خرید مخکي واپس سوی دی.");
        }
        else if (!await _context.Purchases.AnyAsync(x => x.ID == request.PurchaseId && x.ReceivedAmount >= request.RecieveAmount))
        {
            return BadRequest("د واپسۍ لپاره د ورکړل سوي مبلغ باید د خرید د رسید مبلغ څخه زیات نه وي.");
        }
        else
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var purchase = await _context.Purchases.FirstOrDefaultAsync(x => x.ID == request.PurchaseId);

                var remarks = $"خرید نمبر {purchase.PurchaseNo} | {request.Description}";

                if (request.RecieveAmount > purchase.ReceivedAmount)
                {
                    return BadRequest("د واپسۍ مبلغ د ورکړل سوي مبلغ څخه زیاتېدلای نه سي.");
                }
                if (purchase.IsRefunded)
                {
                    return BadRequest("دا خرید مخکي واپس سوی دی.");
                }

                var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

                if (!purchase.IsHolded)
                {
                    var purchaserBalance = await _context.AccountBalances
                    .FirstOrDefaultAsync(x => x.AccountID == purchase.AccountID && x.CurrencyID == purchase.CurrencyID);

                    if (purchaserBalance == null)
                    {
                        var balance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance
                        {
                            AccountID = purchase.AccountID,
                            CurrencyID = purchase.CurrencyID,
                            Balance = 0,
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now
                        });

                        await _context.SaveChangesAsync();

                        purchaserBalance = balance.Entity;
                    }

                    var feesSourceBalance = await _context.AccountBalances
                        .FirstOrDefaultAsync(x => x.AccountID == request.FeesSource && x.CurrencyID == purchase.CurrencyID);

                    if (feesSourceBalance == null && request.RecieveAmount > 0)
                    {
                        var balance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance
                        {
                            AccountID = request.FeesSource,
                            CurrencyID = purchase.CurrencyID,
                            Balance = 0,
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now
                        });

                        await _context.SaveChangesAsync();

                        feesSourceBalance = balance.Entity;
                    }

                    purchaserBalance.Balance += purchase.TotalAmount;

                    await _context.SaveChangesAsync();

                    await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry
                    {
                        AccountBalanceID = purchaserBalance.ID,
                        Balance = purchaserBalance.Balance,
                        Debit = 0,
                        Credit = purchase.TotalAmount,
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Remarks = remarks,
                        TransactionTypeID = 10
                    });

                    await _context.SaveChangesAsync();

                    if (request.RecieveAmount > 0)
                    {
                        purchaserBalance.Balance -= request.RecieveAmount;

                        await _context.SaveChangesAsync();

                        await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry
                        {
                            AccountBalanceID = purchaserBalance.ID,
                            Balance = purchaserBalance.Balance,
                            Debit = request.RecieveAmount,
                            Credit = 0,
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            Remarks = remarks,
                            TransactionTypeID = 10
                        });

                        await _context.SaveChangesAsync();

                        if (feesSourceBalance != null)
                        {
                            feesSourceBalance.Balance += request.RecieveAmount;

                            await _context.SaveChangesAsync();

                            await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry
                            {
                                AccountBalanceID = feesSourceBalance.ID,
                                Balance = feesSourceBalance.Balance,
                                Debit = 0,
                                Credit = request.RecieveAmount,
                                CreatedByUserId = user,
                                CreationDate = DateTime.Now,
                                Remarks = remarks,
                                TransactionTypeID = 10
                            });

                            await _context.SaveChangesAsync();
                        }
                    }
                    purchase.RemainingAmount = purchase.TotalAmount - purchase.ReceivedAmount;

                    purchase.IsRefunded = true;

                    await _context.SaveChangesAsync();

                }

                if (purchase.CanAffectStock)
                {
                    var purchaseDetails = await _context.PurchaseDetails
                        .Where(x => x.PurchaseID == purchase.ID)
                        .ToListAsync();

                    foreach (var detail in purchaseDetails)
                    {
                        var unit = await _context.UnitConversion
                            .FirstOrDefaultAsync(x => x.ID == detail.UnitConversionID);
                        if (unit == null || unit.ExchangedAmount == 0)
                        {
                            throw new InvalidOperationException("د خرید د واحد د تبدیل معلومات ناسم دي.");
                        }

                        var stock = await _context.StockBalances.FirstOrDefaultAsync(x =>
                            x.ItemID == detail.ItemID && x.WarehouseID == detail.WarehouseID);
                        if (stock == null)
                        {
                            throw new InvalidOperationException("د خرید اړوند د ګدام موجودي ونه موندل سوه.");
                        }

                        stock.Quantity -= detail.Quantity / unit.ExchangedAmount;
                        await _context.StockTransactions.AddAsync(new Models.Inventory.StockTransactions
                        {
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            Quantity = detail.Quantity,
                            StockBalanceID = stock.ID,
                            TransactionID = 10,
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

    [HttpGet("GetPurchaseList")]
    public async Task<ActionResult> GetPurchaseList(int? personId, int? currencyId, DateTime? startDate, DateTime? endDate)
    {
        var purchases = _context.Purchases.AsQueryable();

        if (personId.HasValue && personId.Value > 0)
            purchases = purchases.Where(x => x.AccountID == personId.Value);

        if (currencyId.HasValue && currencyId.Value > 0)
            purchases = purchases.Where(x => x.CurrencyID == currencyId.Value);

        if (startDate.HasValue)
            purchases = purchases.Where(x => x.CreationDate >= startDate.Value.Date);

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1);
            purchases = purchases.Where(x => x.CreationDate < endOfDay);
        }

        var result = await purchases
            .OrderByDescending(x => x.CreationDate)
            .Select(x => new PurchaseViewModel
            {
                PurchaseId = x.ID,
                PurchaseNo = x.PurchaseNo,
                PersonId = x.AccountID,
                PersonName = x.Account.Name,
                CurrencyId = x.CurrencyID,
                CurrencyName = x.Currency.CurrencyName,
                PurchaseTotal = x.TotalAmount,
                PurchaseRecieved = x.ReceivedAmount,
                PurchaseRemaining = x.RemainingAmount,
                PurchaseItemsCount = _context.PurchaseDetails.Count(d => d.PurchaseID == x.ID),
                Remarks = x.Remarks,
                PurchaseDate = x.CreationDate,
                IsHolded = x.IsHolded,
                EffectsStock = x.CanAffectStock,
                IsRefunded = x.IsRefunded
            })
            .ToListAsync();

        return Ok(result);
    }
    [HttpPost("SalePayment")]
    public async Task<ActionResult> SalePayment(PurchasePaymentRequest request)
    {
        if (request == null || request.PurchaseId <= 0 || request.RecieveAmount <= 0 || request.FeesSource <= 0) return BadRequest("د رسید معلومات ناسم دي.");
        var sale = await _context.Sales.FirstOrDefaultAsync(x => x.ID == request.PurchaseId);
        if (sale == null || sale.IsHolded || sale.IsRefunded) return BadRequest("د فروش معلومات ناسم دي.");
        if (request.RecieveAmount > sale.RemainingAmount || !await _context.Accounts.AnyAsync(x => x.ID == request.FeesSource && x.IsActive)) return BadRequest("د رسید مبلغ يا بانک ناسم دی.");
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var user = _accessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!; var date = DateTime.Now;
            var customer = await _context.AccountBalances.FirstOrDefaultAsync(x => x.AccountID == sale.AccountID && x.CurrencyID == sale.CurrencyID);
            var bank = await _context.AccountBalances.FirstOrDefaultAsync(x => x.AccountID == request.FeesSource && x.CurrencyID == sale.CurrencyID);
            if (customer == null || bank == null) throw new InvalidOperationException("د فروش اړوند حساب بیلانس ونه موندل سو.");
            customer.Balance -= request.RecieveAmount; bank.Balance += request.RecieveAmount; sale.ReceivedAmount += request.RecieveAmount; sale.RemainingAmount -= request.RecieveAmount;
            var remarks = $"فروش نمبر: {sale.SaleNo} | {request.Description}";
            await _context.JournalEntries.AddRangeAsync(
                new Models.Accounting.JournalEntry { AccountBalanceID = customer.ID, Balance = customer.Balance, Debit = request.RecieveAmount, TransactionTypeID = 5, Remarks = remarks, ChequePhoto = "default.png", CreatedByUserId = user, CreationDate = date },
                new Models.Accounting.JournalEntry { AccountBalanceID = bank.ID, Balance = bank.Balance, Credit = request.RecieveAmount, TransactionTypeID = 5, Remarks = remarks, ChequePhoto = "default.png", CreatedByUserId = user, CreationDate = date });
            await _context.SaveChangesAsync(); await transaction.CommitAsync(); return Ok();
        }
        catch (Exception ex) { await transaction.RollbackAsync(); return BadRequest(ex.Message); }
    }

    [HttpGet("GetSaleList")]
    public async Task<ActionResult> GetSaleList(int? personId, int? currencyId, DateTime? startDate, DateTime? endDate)
    {
        var sales = _context.Sales.AsQueryable();
        if (personId > 0) sales = sales.Where(x => x.AccountID == personId);
        if (currencyId > 0) sales = sales.Where(x => x.CurrencyID == currencyId);
        if (startDate.HasValue) sales = sales.Where(x => x.CreationDate >= startDate.Value.Date);
        if (endDate.HasValue) sales = sales.Where(x => x.CreationDate < endDate.Value.Date.AddDays(1));
        return Ok(await sales.OrderByDescending(x => x.CreationDate).Select(x => new SaleViewModel { SaleId=x.ID, SaleNo=x.SaleNo, PersonId=x.AccountID, PersonName=x.Account.Name, CurrencyId=x.CurrencyID, CurrencyName=x.Currency.CurrencyName, SaleTotal=x.TotalAmount, SaleRecieved=x.ReceivedAmount, SaleRemaining=x.RemainingAmount, SaleDate=x.CreationDate, Remarks=x.Remarks, IsHolded=x.IsHolded, EffectsStock=x.CanAffectStock, IsRefunded=x.IsRefunded, SaleItemsCount=_context.SalesDetails.Count(d => d.SaleID==x.ID) }).ToListAsync());
    }
}

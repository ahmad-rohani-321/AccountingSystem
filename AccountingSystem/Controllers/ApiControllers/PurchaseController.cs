using System.Security.Claims;
using AccountingSystem.Data;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace AccountingSystem.Controllers.ApiControllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PurchaseController(ApplicationDbContext context, IHttpContextAccessor accessor) : ControllerBase
{      
    private readonly ApplicationDbContext _context = context;
    private readonly IHttpContextAccessor _accessor = accessor;


    [HttpGet("Next-No")]
    public async Task<IActionResult> GetNextNo()
    {
        var lastPurchaseNo = await _context.Purchases
            .Select(p => (int?)p.PurchaseNo)
            .MaxAsync() ?? 0;

        return Ok(new { PurchaseNo = lastPurchaseNo + 1 });
    }

    [HttpPost("SaveNewPurchase")]
    public  async Task<IActionResult> SaveNewPurchase(PurchaseViewModel request)
    {
        if(request == null)
        {
            return BadRequest("خالي خرید نه ثبت کیږي.");
        }
        else if(request.BankId != 0 && !await _context.Accounts.AnyAsync(x => x.ID == request.BankId))
        {
            return BadRequest("ناسم بانک انتخاب سوی دی");
        }
        else if(!await _context.Accounts.AnyAsync(x => x.ID == request.PersonId))
        {
            return BadRequest("ناسم شخص انتخاب سوی دی");
        }
        else if(!await _context.Currencies.AnyAsync(x => x.ID == request.CurrencyId))
        {
            return BadRequest("ناسم اسعار انتخاب سوی دی");
        }
        else if(await _context.Purchases.AnyAsync(x => x.PurchaseNo == request.PurchaseId))
        {
            return BadRequest("ټاکل سوې د خرید شمېره تکراري ده");
        }
        else if(request.PurchaseTotal != request.PurchaseDetails.Sum(x => x.TotalPrice))
        {
            return BadRequest("د خرید مجموعه ناسم محاسبه سوې ده");
        }
        else if(request.PurchaseRecieved > request.PurchaseTotal)
        {
            return BadRequest("رسید مبلغ له مجموعه مبلغ څخه لوړ دی");
        }
        else if (!await _context.Items.AnyAsync(x => request.PurchaseDetails.Select(i => i.ItemId).Contains(x.ID)))
        {
            return BadRequest("هیله ده د خرید اجناس اصلاح کړئ");
        }
        else if(!await _context.UnitConversion
                            .AnyAsync(u => 
                            request.PurchaseDetails.Select(x => x.UnitId).Contains(u.ID) && 
                            request.PurchaseDetails.Select(x => x.ItemId).Contains(u.ItemID)))
        {
            return BadRequest("هیله ده واحدونه اصلاح کړئ!");
        }
        else if (!await _context.WareHouses.AnyAsync(s => request.PurchaseDetails.Select(w => w.StockId).Contains(s.ID)))
        {
            return BadRequest("هیله ده ګدامونه اصلاح کړی!");
        }
        else
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                DateTime date = request.PurchaseDate == DateTime.Now.Date ? DateTime.Now : request.PurchaseDate;
                var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                string remarks = $"خرید نمبر: {request.PurchaseId} | {request.Remarks}";
                var purchase = await _context.Purchases.AddAsync(new Models.Purchase.Purchase()
                {
                    AccountID = request.PersonId,
                    CanAffectStock = request.EffectsStock,
                    CreatedByUserId = user,
                    CreationDate = date,
                    CurrencyID = request.CurrencyId,
                    IsHolded = request.IsHolded,
                    IsRefunded = false,
                    PurchaseNo = request.PurchaseId,
                    Remarks = request.Remarks,
                    TotalAmount = request.PurchaseTotal,
                    ReceivedAmount = request.PurchaseRecieved,
                    RemainingAmount = request.PurchaseTotal - request.PurchaseRecieved
                });
                await _context.SaveChangesAsync();
                if (!request.IsHolded)
                {
                    var personAccount = await _context.AccountBalances.FirstOrDefaultAsync(x => x.AccountID == request.PersonId && x.CurrencyID == request.CurrencyId);
                    if(personAccount == null)
                    {
                        var balance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance()
                        {
                            AccountID = request.PersonId,
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            CurrencyID = request.CurrencyId,
                            Balance = 0
                        });
                        await _context.SaveChangesAsync();
                        personAccount = balance.Entity;
                    }
                    personAccount.Balance -= request.PurchaseTotal;
                    await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                    {
                        AccountBalanceID = personAccount.ID,
                        Balance = personAccount.Balance,
                        Debit = request.PurchaseTotal,
                        CreatedByUserId = user,
                        CreationDate = date,
                        Remarks = remarks,
                        TransactionTypeID = 6,
                        ChequePhoto = "default.png"
                    });
                    if(request.PurchaseRecieved > 0)
                    {
                        personAccount.Balance += request.PurchaseRecieved;
                        await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                        {
                            AccountBalanceID = personAccount.ID,
                            Balance = personAccount.Balance,
                            Credit = request.PurchaseRecieved,
                            CreatedByUserId = user,
                            CreationDate = date,
                            Remarks = remarks,
                            TransactionTypeID = 6,
                            ChequePhoto = "default.png"
                        });
                    }
                    await _context.SaveChangesAsync();
                }
                foreach (var item in request.PurchaseDetails)
                {
                    await _context.PurchaseDetails.AddAsync(new Models.Purchase.PurchaseDetails()
                    {
                        CreatedByUserId = user,
                        CreationDate = date,
                        ItemID = item.ItemId,
                        PerPrice = item.PerPrice,
                        PurchaseID = purchase.Entity.ID,
                        Quantity = item.Quantity,
                        TotalPrice = item.TotalPrice,
                        UnitConversionID = item.UnitId,
                        WarehouseID = item.StockId
                    });
                    if (!request.IsHolded && request.EffectsStock)
                    {
                        var stock = await _context.StockBalances.FirstOrDefaultAsync(x => x.ItemID == item.ItemId && x.WarehouseID == item.StockId);
                        var unitExchange = await _context.UnitConversion.FirstOrDefaultAsync(x => x.ID == item.UnitId);
                        decimal realStock = item.Quantity / unitExchange.ExchangedAmount;
                        if(stock == null)
                        {
                            var stockEntry = await _context.StockBalances.AddAsync(new Models.Inventory.StockBalance()
                            {
                                CreatedByUserId = user,
                                CreationDate = DateTime.Now,
                                ItemID = item.ItemId,
                                WarehouseID = item.StockId,
                                Remarks = item.Remarks,
                                Quantity = realStock
                            });
                            await _context.SaveChangesAsync();
                            stock = stockEntry.Entity;
                        }
                        await _context.StockTransactions.AddAsync(new Models.Inventory.StockTransactions()
                        {
                            CreatedByUserId = user,
                            CreationDate = date,
                            Quantity = item.Quantity,
                            StockBalanceID = stock.ID,
                            TransactionID = 5,
                            Remarks = item.Remarks,
                            UnitID = item.UnitId
                        });
                        await _context.SaveChangesAsync();
                    }
                    await _context.SaveChangesAsync();
                }
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

    [HttpPost("EditPurchase")]
    public  async Task<IActionResult> EditPurchase(PurchaseViewModel request)
    {
        if(request == null)
        {
            return BadRequest("خالي خرید نه ثبت کیږي.");
        }
        else if(request.BankId != 0 && !await _context.Accounts.AnyAsync(x => x.ID == request.BankId))
        {
            return BadRequest("ناسم بانک انتخاب سوی دی");
        }
        else if(!await _context.Accounts.AnyAsync(x => x.ID == request.PersonId))
        {
            return BadRequest("ناسم شخص انتخاب سوی دی");
        }
        else if(!await _context.Currencies.AnyAsync(x => x.ID == request.CurrencyId))
        {
            return BadRequest("ناسم اسعار انتخاب سوی دی");
        }
        else if(!await _context.Purchases.AnyAsync(x => x.ID == request.PurchaseId))
        {
            return NotFound("ناسم خرید شمېره");
        }
        else if(await _context.Purchases.AnyAsync(x => x.ID != request.PurchaseId && x.PurchaseNo == request.PurchaseNo))
        {
            return BadRequest("ټاکل سوې د خرید شمېره تکراري ده");
        }
        else if(request.PurchaseTotal != request.PurchaseDetails.Sum(x => x.TotalPrice))
        {
            return BadRequest("د خرید مجموعه ناسم محاسبه سوې ده");
        }
        else if(request.PurchaseRecieved > request.PurchaseTotal)
        {
            return BadRequest("رسید مبلغ له مجموعه مبلغ څخه لوړ دی");
        }
        else if (!await _context.Items.AnyAsync(x => request.PurchaseDetails.Select(i => i.ItemId).Contains(x.ID)))
        {
            return BadRequest("هیله ده د خرید اجناس اصلاح کړئ");
        }
        else if(!await _context.UnitConversion
                            .AnyAsync(u => 
                            request.PurchaseDetails.Select(x => x.UnitId).Contains(u.ID) && 
                            request.PurchaseDetails.Select(x => x.ItemId).Contains(u.ItemID)))
        {
            return BadRequest("هیله ده واحدونه اصلاح کړئ!");
        }
        else if (!await _context.WareHouses.AnyAsync(s => request.PurchaseDetails.Select(w => w.StockId).Contains(s.ID)))
        {
            return BadRequest("هیله ده ګدامونه اصلاح کړی!");
        }
        else
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                DateTime date = request.PurchaseDate == DateTime.Now.Date ? DateTime.Now : request.PurchaseDate;
                var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                string remarks = $"خرید نمبر: {request.PurchaseNo} | {request.Remarks}";
                var purchase = await _context.Purchases.FirstOrDefaultAsync(x => x.ID == request.PurchaseId);
                var oldPurchaseDetails = await _context.PurchaseDetails
                    .Include(x => x.UnitConversion)
                    .Where(x => x.PurchaseID == purchase.ID)
                    .ToListAsync();
                var oldDetailsById = oldPurchaseDetails.ToDictionary(x => x.ID);
                var submittedDetailIds = request.PurchaseDetails
                    .Where(x => x.Id != 0)
                    .Select(x => x.Id)
                    .ToHashSet();
                if (submittedDetailIds.Count != request.PurchaseDetails.Count(x => x.Id != 0))
                {
                    return BadRequest("A purchase detail was submitted more than once.");
                }
                var oldAffectsStock = !purchase.IsHolded && purchase.CanAffectStock;
                var newAffectsStock = !request.IsHolded && request.EffectsStock;

                async Task ApplyStockAdjustment(
                    int itemId,
                    int warehouseId,
                    int unitId,
                    decimal quantity,
                    bool addToStock,
                    string itemRemarks)
                {
                    if (quantity == 0)
                    {
                        return;
                    }

                    var unit = await _context.UnitConversion.FirstOrDefaultAsync(x => x.ID == unitId);
                    if (unit == null || unit.ExchangedAmount == 0)
                    {
                        throw new InvalidOperationException("The unit conversion is invalid.");
                    }

                    var stock = await _context.StockBalances.FirstOrDefaultAsync(x =>
                        x.ItemID == itemId && x.WarehouseID == warehouseId);

                    if (stock == null)
                    {
                        if (!addToStock)
                        {
                            throw new InvalidOperationException("Purchase stock balance was not found.");
                        }

                        stock = new Models.Inventory.StockBalance()
                        {
                            CreatedByUserId = user,
                            CreationDate = date,
                            ItemID = itemId,
                            WarehouseID = warehouseId,
                            Remarks = itemRemarks,
                            Quantity = 0
                        };
                        await _context.StockBalances.AddAsync(stock);
                    }

                    var baseQuantity = quantity / unit.ExchangedAmount;
                    stock.Quantity += addToStock ? baseQuantity : -baseQuantity;

                    await _context.StockTransactions.AddAsync(new Models.Inventory.StockTransactions()
                    {
                        CreatedByUserId = user,
                        CreationDate = date,
                        Quantity = quantity,
                        StockBalance = stock,
                        TransactionID = addToStock ? 6 : 13,
                        Remarks = itemRemarks,
                        UnitID = unitId
                    });
                }

                purchase.AccountID = request.PersonId;
                purchase.CanAffectStock = request.EffectsStock;
                purchase.CreatedByUserId = user;
                purchase.CreationDate = date;
                purchase.CurrencyID = request.CurrencyId;
                purchase.IsHolded = request.IsHolded;
                purchase.IsRefunded = false;
                purchase.PurchaseNo = request.PurchaseNo;
                purchase.Remarks = request.Remarks;
                purchase.TotalAmount = request.PurchaseTotal;
                purchase.ReceivedAmount = request.PurchaseRecieved;
                purchase.RemainingAmount = request.PurchaseTotal - request.PurchaseRecieved;

                await _context.SaveChangesAsync();
                if (!request.IsHolded)
                {
                    var personAccount = await _context.AccountBalances.FirstOrDefaultAsync(x => x.AccountID == request.PersonId && x.CurrencyID == request.CurrencyId);
                    if(personAccount == null)
                    {
                        var balance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance()
                        {
                            AccountID = request.PersonId,
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            CurrencyID = request.CurrencyId,
                            Balance = 0
                        });
                        await _context.SaveChangesAsync();
                        personAccount = balance.Entity;
                    }
                    personAccount.Balance -= request.PurchaseTotal;
                    await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                    {
                        AccountBalanceID = personAccount.ID,
                        Balance = personAccount.Balance,
                        Debit = request.PurchaseTotal,
                        CreatedByUserId = user,
                        CreationDate = date,
                        Remarks = remarks,
                        TransactionTypeID = 6,
                        ChequePhoto = "default.png"
                    });
                    if(request.PurchaseRecieved > 0)
                    {
                        personAccount.Balance += request.PurchaseRecieved;
                        await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                        {
                            AccountBalanceID = personAccount.ID,
                            Balance = personAccount.Balance,
                            Credit = request.PurchaseRecieved,
                            CreatedByUserId = user,
                            CreationDate = date,
                            Remarks = remarks,
                            TransactionTypeID = 6,
                            ChequePhoto = "default.png"
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                foreach (var item in request.PurchaseDetails)
                {
                    Models.Purchase.PurchaseDetails existingDetail = null;
                    if (item.Id != 0 && !oldDetailsById.TryGetValue(item.Id, out existingDetail))
                    {
                        return BadRequest("A purchase detail does not belong to this purchase.");
                    }

                    if (item.Id == 0)
                    {
                        await _context.PurchaseDetails.AddAsync(new Models.Purchase.PurchaseDetails()
                        {
                            CreatedByUserId = user,
                            CreationDate = date,
                            ItemID = item.ItemId,
                            PerPrice = item.PerPrice,
                            PurchaseID = purchase.ID,
                            Quantity = item.Quantity,
                            TotalPrice = item.TotalPrice,
                            UnitConversionID = item.UnitId,
                            WarehouseID = item.StockId,
                            Remarks = item.Remarks
                        });

                        if (newAffectsStock)
                        {
                            await ApplyStockAdjustment(item.ItemId, item.StockId, item.UnitId, item.Quantity, true, item.Remarks);
                        }
                        continue;
                    }

                    if (oldAffectsStock && newAffectsStock &&
                        existingDetail.ItemID == item.ItemId &&
                        existingDetail.WarehouseID == item.StockId &&
                        existingDetail.UnitConversionID == item.UnitId)
                    {
                        var quantityDifference = item.Quantity - existingDetail.Quantity;
                        if (quantityDifference > 0)
                        {
                            await ApplyStockAdjustment(item.ItemId, item.StockId, item.UnitId, quantityDifference, true, item.Remarks);
                        }
                        else if (quantityDifference < 0)
                        {
                            await ApplyStockAdjustment(item.ItemId, item.StockId, item.UnitId, -quantityDifference, false, item.Remarks);
                        }
                    }
                    else
                    {
                        if (oldAffectsStock)
                        {
                            await ApplyStockAdjustment(existingDetail.ItemID, existingDetail.WarehouseID,
                                existingDetail.UnitConversionID, existingDetail.Quantity, false, existingDetail.Remarks);
                        }
                        if (newAffectsStock)
                        {
                            await ApplyStockAdjustment(item.ItemId, item.StockId, item.UnitId, item.Quantity, true, item.Remarks);
                        }
                    }

                    existingDetail.ItemID = item.ItemId;
                    existingDetail.UnitConversionID = item.UnitId;
                    existingDetail.WarehouseID = item.StockId;
                    existingDetail.Quantity = item.Quantity;
                    existingDetail.PerPrice = item.PerPrice;
                    existingDetail.TotalPrice = item.TotalPrice;
                    existingDetail.Remarks = item.Remarks;
                }

                foreach (var removedDetail in oldPurchaseDetails.Where(x => !submittedDetailIds.Contains(x.ID)))
                {
                    if (oldAffectsStock)
                    {
                        await ApplyStockAdjustment(removedDetail.ItemID, removedDetail.WarehouseID,
                            removedDetail.UnitConversionID, removedDetail.Quantity, false, removedDetail.Remarks);
                    }
                    _context.PurchaseDetails.Remove(removedDetail);
                }

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

    [HttpGet("GetPurchaseById/{id}")]
    public async Task<ActionResult> GetPurchaseById(int? id)
    {
        if(id.HasValue && id.Value <= 0)
        {
            return BadRequest("ناسم خرید شمېره");
        }
        else if(!await _context.Purchases.AnyAsync(x => x.ID == id.Value))
        {
            return NotFound("خرید ونه موندل سو");
        }
        else if(!(await _context.Purchases.FirstOrDefaultAsync(x => x.ID == id.Value)).IsHolded)
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
                EffectsStock = x.CanAffectStock
            })
            .ToListAsync();

        return Ok(result);
    }

}

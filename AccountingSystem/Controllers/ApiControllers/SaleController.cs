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

    [HttpGet("GetSalePrice/{itemId}")]
    public async Task<IActionResult> GetSalePrice(int itemId)
    {
        var itemprice = await _context.ItemsPrices.OrderByDescending(x => x.CreationDate).FirstOrDefaultAsync(x => x.ItemID == itemId);
        return Ok(itemprice != null ? itemprice.SalePrice : 0);
    }

    [HttpGet("Next-No")]
    public async Task<IActionResult> GetNextNo()
    {
        var lastSaleNo = await _context.Sales
            .Select(s => (int?)s.SaleNo)
            .MaxAsync() ?? 0;

        return Ok(new { SaleNo = lastSaleNo + 1 });
    }

    [HttpGet("HasStockQuantity")]
    public async Task<IActionResult> HasStockQuantity(int itemId, int unitId, int stockId, decimal quantity)
    {
        if(!await _context.Items.AnyAsync(x => x.ID == itemId))
        {
            return BadRequest("صحیح جنس انتخاب کړئ");
        }
        else if(!await _context.UnitConversion.AnyAsync(x => x.ID == unitId))
        {
            return BadRequest("صحیح واحد انتخاب کړئ");
        }
        else if(!await _context.WareHouses.AnyAsync(x => x.ID == stockId))
        {
            return BadRequest("صحیح ګدام انتخاب کړئ");
        }
        else if(quantity <= 0)
        {
            return BadRequest("صحیح مقدار داخل کړئ");
        }
        else
        {
            
            var unit = await _context.UnitConversion.FirstOrDefaultAsync(x => x.ID == unitId);
            if (unit == null || unit.ItemID != itemId || unit.ExchangedAmount <= 0)
            {
                return BadRequest("د جنس او واحد د تبدیل معلومات ناسم دي");
            }

            var stockBalance = await _context.StockBalances.Where(x => x.ItemID == itemId && x.WarehouseID == stockId).ToListAsync();

            var baseQuantity = quantity / unit.ExchangedAmount;
            var hasStock = stockBalance.Any(s => s.Quantity >= baseQuantity);

            return Ok(new { HasStock = hasStock });
        }
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

                var unitIds = request.SaleDetails.Select(x => x.UnitId).Distinct().ToArray();
                var units = await _context.UnitConversion
                    .Where(x => unitIds.Contains(x.ID))
                    .ToDictionaryAsync(x => x.ID);

                if (units.Count != unitIds.Length || request.SaleDetails.Any(x =>
                    !units.TryGetValue(x.UnitId, out var unit) ||
                    unit.ItemID != x.ItemId ||
                    unit.ExchangedAmount <= 0))
                {
                    return BadRequest("د واحد د تبدیل معلومات ناسم دي.");
                }

                var itemIds = request.SaleDetails.Select(x => x.ItemId).Distinct().ToArray();
                var warehouseIds = request.SaleDetails.Select(x => x.StockId).Distinct().ToArray();
                var stockBalances = await _context.StockBalances
                    .Where(x => itemIds.Contains(x.ItemID) && warehouseIds.Contains(x.WarehouseID))
                    .ToListAsync();

                var stocksByItemAndWarehouse = stockBalances
                    .GroupBy(x => (x.ItemID, x.WarehouseID))
                    .ToDictionary(x => x.Key, x => x.OrderBy(x => x.CreationDate).ThenBy(x => x.ID).ToList());
                var availableStockQuantity = stockBalances.ToDictionary(x => x.ID, x => x.Quantity);

                var requiredStock = request.SaleDetails
                    .GroupBy(x => (x.ItemId, x.StockId))
                    .Select(x => new
                    {
                        x.Key,
                        Quantity = x.Sum(line => line.Quantity / units[line.UnitId].ExchangedAmount)
                    })
                    .ToList();

                foreach (var requirement in requiredStock)
                {
                    if (!stocksByItemAndWarehouse.TryGetValue(requirement.Key, out var stocks) ||
                        stocks.Sum(x => availableStockQuantity[x.ID]) < requirement.Quantity)
                    {
                        return BadRequest("د فروش لپاره په ګدام کې کافي موجودي نسته.");
                    }
                }

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
                    var remainingQuantity = item.Quantity / units[item.UnitId].ExchangedAmount;
                    var stocks = stocksByItemAndWarehouse[(item.ItemId, item.StockId)];

                    foreach (var stock in stocks)
                    {
                        if (remainingQuantity <= 0)
                        {
                            break;
                        }

                        var takenBaseQuantity = Math.Min(availableStockQuantity[stock.ID], remainingQuantity);
                        if (takenBaseQuantity <= 0)
                        {
                            continue;
                        }

                        var takenQuantity = takenBaseQuantity * units[item.UnitId].ExchangedAmount;
                        var purchasePrice = stock.PurchaseBaseCurrencyPrice * units[item.UnitId].ExchangedAmount;

                        await _context.SalesDetails.AddAsync(new Models.Sales.SaleDetails()
                        {
                            CreatedByUserId = user,
                            CreationDate = date,
                            ItemID = item.ItemId,
                            PerPrice = item.PerPrice,
                            SaleID = sale.Entity.ID,
                            Quantity = takenQuantity,
                            TotalPrice = item.PerPrice * takenQuantity,
                            Profit = (item.PerPrice - purchasePrice) * takenQuantity,
                            UnitConversionID = item.UnitId,
                            WarehouseID = item.StockId,
                            StockItemId = stock.ID,
                            Remarks = item.Remarks
                        });

                        availableStockQuantity[stock.ID] -= takenBaseQuantity;
                        remainingQuantity -= takenBaseQuantity;

                        if (request.EffectsStock)
                        {
                            stock.Quantity -= takenBaseQuantity;
                            await _context.StockTransactions.AddAsync(new Models.Inventory.StockTransactions()
                            {
                                CreatedByUserId = user,
                                CreationDate = date,
                                Quantity = takenQuantity,
                                StockBalanceID = stock.ID,
                                TransactionID = 7,
                                Remarks = item.Remarks,
                                UnitID = item.UnitId
                            });
                        }
                    }
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
        if (request == null || request.SaleDetails == null || request.SaleDetails.Count == 0)
        {
            return BadRequest("خالي فروش نه ثبت کیږي.");
        }
        else if (request.SaleId <= 0 || request.SaleNo <= 0 || request.SaleTotal < 0 || request.SaleRecieved < 0 ||
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
        else if (!await _context.Sales.AnyAsync(x => x.ID == request.SaleId))
        {
            return NotFound("ناسم فروش شمېره");
        }
        else if (await _context.Sales.AnyAsync(x => x.ID != request.SaleId && x.SaleNo == request.SaleNo))
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
        else if (await _context.WareHouses.CountAsync(x => request.SaleDetails.Select(i => i.StockId).Distinct().Contains(x.ID))
                 != request.SaleDetails.Select(i => i.StockId).Distinct().Count())
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
                var sale = await _context.Sales.FirstAsync(x => x.ID == request.SaleId);

                if (sale.IsRefunded)
                {
                    return BadRequest("واپس سوی فروش د تغیر وړ نه دی.");
                }

                var oldSaleDetails = await _context.SalesDetails
                    .Where(x => x.SaleID == sale.ID)
                    .ToListAsync();
                var oldDetailsById = oldSaleDetails.ToDictionary(x => x.ID);
                var submittedDetailIds = request.SaleDetails.Where(x => x.Id != 0).Select(x => x.Id).ToHashSet();

                if (submittedDetailIds.Count != request.SaleDetails.Count(x => x.Id != 0))
                {
                    return BadRequest("د فروش یو جز له یو ځل څخه زیات ثبت سوی دی.");
                }
                else if (request.SaleDetails.Any(x => x.Id != 0 && !oldDetailsById.ContainsKey(x.Id)))
                {
                    return BadRequest("د فروش دا جز له دې فروش سره تړاو نه لري.");
                }

                var unitIds = request.SaleDetails.Select(x => x.UnitId)
                    .Concat(oldSaleDetails.Select(x => x.UnitConversionID))
                    .Distinct()
                    .ToArray();
                var units = await _context.UnitConversion
                    .Where(x => unitIds.Contains(x.ID))
                    .ToDictionaryAsync(x => x.ID);

                if (units.Count != unitIds.Length || request.SaleDetails.Any(x =>
                    !units.TryGetValue(x.UnitId, out var unit) ||
                    unit.ItemID != x.ItemId || unit.ExchangedAmount <= 0))
                {
                    return BadRequest("د واحد د تبدیل معلومات ناسم دي.");
                }

                var oldAffectsStock = sale.CanAffectStock;
                var newAffectsStock = request.EffectsStock;
                var stockKeys = oldSaleDetails.Select(x => (x.ItemID, x.WarehouseID))
                    .Concat(request.SaleDetails.Select(x => (x.ItemId, x.StockId)))
                    .Distinct()
                    .ToArray();
                var itemIds = stockKeys.Select(x => x.Item1).Distinct().ToArray();
                var warehouseIds = stockKeys.Select(x => x.Item2).Distinct().ToArray();
                var oldStockIds = oldSaleDetails.Select(x => x.StockItemId).Distinct().ToArray();
                var stocks = await _context.StockBalances
                    .Where(x => (itemIds.Contains(x.ItemID) && warehouseIds.Contains(x.WarehouseID)) || oldStockIds.Contains(x.ID))
                    .ToListAsync();
                var stocksByItemAndWarehouse = stocks
                    .GroupBy(x => (x.ItemID, x.WarehouseID))
                    .ToDictionary(x => x.Key, x => x.OrderBy(x => x.CreationDate).ThenBy(x => x.ID).ToList());
                var stockById = stocks.ToDictionary(x => x.ID);

                if ((oldAffectsStock || newAffectsStock) && stockKeys.Any(x => !stocksByItemAndWarehouse.ContainsKey(x)))
                {
                    return BadRequest("د فروش لپاره په ګدام کې کافي موجودي نسته.");
                }

                if (oldAffectsStock)
                {
                    foreach (var oldDetail in oldSaleDetails)
                    {
                        if (!stockById.TryGetValue(oldDetail.StockItemId, out var stock))
                        {
                            return BadRequest("د فروش اړوند موجودي ونه موندل سوه.");
                        }
                        stock.Quantity += oldDetail.Quantity / units[oldDetail.UnitConversionID].ExchangedAmount;

                        await _context.StockTransactions.AddAsync(new Models.Inventory.StockTransactions()
                        {
                            CreatedByUserId = user,
                            CreationDate = date,
                            Quantity = oldDetail.Quantity,
                            StockBalanceID = stock.ID,
                            TransactionID = 8,
                            Remarks = $"فروش نمبر: {sale.SaleNo} تغیر پخوانی مقدار واپس سو | {oldDetail.Remarks}",
                            UnitID = oldDetail.UnitConversionID
                        });
                    }
                }

                var availableStockQuantity = stocks.ToDictionary(x => x.ID, x => x.Quantity);

                var requiredStock = request.SaleDetails
                    .GroupBy(x => (x.ItemId, x.StockId))
                    .Select(x => new
                    {
                        x.Key,
                        Quantity = x.Sum(line => line.Quantity / units[line.UnitId].ExchangedAmount)
                    })
                    .ToList();

                if (newAffectsStock)
                {
                    foreach (var requirement in requiredStock)
                    {
                        if (!stocksByItemAndWarehouse.TryGetValue(requirement.Key, out var availableStocks) ||
                            availableStocks.Sum(x => availableStockQuantity[x.ID]) < requirement.Quantity)
                        {
                            return BadRequest("د فروش لپاره په ګدام کې کافي موجودي نسته.");
                        }
                    }
                }

                var oldJournalRemarks = $"فروش نمبر: {sale.SaleNo} | {sale.Remarks}";
                var editJournalPrefix = $"فروش تغیر شمېره: {sale.ID} |";
                var previousJournalEntries = await _context.JournalEntries
                    .Where(x => (x.TransactionTypeID == 5 && x.Remarks == oldJournalRemarks) ||
                                (x.TransactionTypeID == 8 && x.Remarks.StartsWith(editJournalPrefix)))
                    .ToListAsync();

                // Reverse only the original sale posting (or the last edit posting).
                // Payment and refund entries have different remarks and remain untouched.
                foreach (var previousJournalEntry in previousJournalEntries)
                {
                    var balance = await _context.AccountBalances.FindAsync(previousJournalEntry.AccountBalanceID);
                    if (balance == null)
                    {
                        throw new InvalidOperationException("د فروش اړوند حساب موجود نه دی.");
                    }

                    balance.Balance += previousJournalEntry.Debit - previousJournalEntry.Credit;
                    await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                    {
                        AccountBalanceID = balance.ID,
                        Balance = balance.Balance,
                        Debit = previousJournalEntry.Credit,
                        Credit = previousJournalEntry.Debit,
                        CreatedByUserId = user,
                        CreationDate = date,
                        Remarks = $"فروش تغیر شمېره: {sale.ID} - عكس پخوانی ثبت",
                        TransactionTypeID = 8
                    });
                }

                async Task<Models.Accounts.AccountBalance> GetAccountBalance(int accountId, int currencyId)
                {
                    var accountBalance = await _context.AccountBalances
                        .FirstOrDefaultAsync(x => x.AccountID == accountId && x.CurrencyID == currencyId);
                    if (accountBalance != null)
                    {
                        return accountBalance;
                    }

                    var newAccountBalance = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance()
                    {
                        AccountID = accountId,
                        CurrencyID = currencyId,
                        Balance = 0,
                        CreatedByUserId = user,
                        CreationDate = date
                    });
                    await _context.SaveChangesAsync();
                    return newAccountBalance.Entity;
                }

                string remarks = $"{editJournalPrefix} {request.Remarks}";
                if (!request.IsHolded)
                {
                    var personAccount = await GetAccountBalance(request.PersonId, request.CurrencyId);
                    personAccount.Balance += request.SaleTotal;
                    await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                    {
                        AccountBalanceID = personAccount.ID,
                        Balance = personAccount.Balance,
                        Credit = request.SaleTotal,
                        CreatedByUserId = user,
                        CreationDate = date,
                        Remarks = remarks,
                        TransactionTypeID = 8
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
                            TransactionTypeID = 8
                        });

                        var treasureAccount = await GetAccountBalance(request.BankId, request.CurrencyId);
                        treasureAccount.Balance += request.SaleRecieved;
                        await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                        {
                            AccountBalanceID = treasureAccount.ID,
                            Balance = treasureAccount.Balance,
                            Credit = request.SaleRecieved,
                            CreatedByUserId = user,
                            CreationDate = date,
                            Remarks = remarks,
                            TransactionTypeID = 8
                        });
                    }
                }

                sale.AccountID = request.PersonId;
                sale.CanAffectStock = request.EffectsStock;
                sale.CreatedByUserId = user;
                sale.CreationDate = date;
                sale.CurrencyID = request.CurrencyId;
                sale.IsHolded = request.IsHolded;
                sale.IsRefunded = false;
                sale.SaleNo = request.SaleNo;
                sale.Remarks = request.Remarks;
                sale.TotalAmount = request.SaleTotal;
                sale.ReceivedAmount = request.SaleRecieved;
                sale.RemainingAmount = request.SaleTotal - request.SaleRecieved;

                _context.SalesDetails.RemoveRange(oldSaleDetails);

                foreach (var item in request.SaleDetails)
                {
                    var remainingQuantity = item.Quantity / units[item.UnitId].ExchangedAmount;
                    var stocksForItem = stocksByItemAndWarehouse[(item.ItemId, item.StockId)];

                    foreach (var stock in stocksForItem)
                    {
                        if (remainingQuantity <= 0)
                        {
                            break;
                        }

                        var takenBaseQuantity = Math.Min(availableStockQuantity[stock.ID], remainingQuantity);
                        if (takenBaseQuantity <= 0)
                        {
                            continue;
                        }

                        var takenQuantity = takenBaseQuantity * units[item.UnitId].ExchangedAmount;
                        var purchasePrice = stock.PurchaseBaseCurrencyPrice * units[item.UnitId].ExchangedAmount;

                        await _context.SalesDetails.AddAsync(new Models.Sales.SaleDetails()
                        {
                            CreatedByUserId = user,
                            CreationDate = date,
                            ItemID = item.ItemId,
                            PerPrice = item.PerPrice,
                            SaleID = sale.ID,
                            Quantity = takenQuantity,
                            TotalPrice = item.PerPrice * takenQuantity,
                            Profit = (item.PerPrice - purchasePrice) * takenQuantity,
                            UnitConversionID = item.UnitId,
                            WarehouseID = item.StockId,
                            StockItemId = stock.ID,
                            Remarks = item.Remarks
                        });

                        availableStockQuantity[stock.ID] -= takenBaseQuantity;
                        remainingQuantity -= takenBaseQuantity;

                        if (newAffectsStock)
                        {
                            stock.Quantity -= takenBaseQuantity;
                            await _context.StockTransactions.AddAsync(new Models.Inventory.StockTransactions()
                            {
                                CreatedByUserId = user,
                                CreationDate = date,
                                Quantity = takenQuantity,
                                StockBalanceID = stock.ID,
                                TransactionID = 11,
                                Remarks = $"فروش نمبر: {request.SaleNo} تغیر | {item.Remarks}",
                                UnitID = item.UnitId
                            });
                        }
                    }
                }

                await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                {
                    CreatedByUserId = user,
                    CreationDate = DateTime.Now,
                    Details = $"د {request.SaleId} فروش تغیر سو.",
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

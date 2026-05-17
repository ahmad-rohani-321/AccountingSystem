using AccountingSystem.Data;
using AccountingSystem.Models.Accounting;
using AccountingSystem.Models.Accounts;
using AccountingSystem.Models.Inventory;
using AccountingSystem.Models.Sales;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.Arm;

namespace AccountingSystem.Controllers.APIs;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SalesController(ApplicationDbContext db) : ApiControllerBase
{
    private readonly ApplicationDbContext _db = db;
    private const int SaleTransactionTypeId = 5;
    private const int SaleChangeTransactionTypeId = 8;
    private const int SaleStockTransactionTypeId = 7;
    private const int SaleRefundTransactionTypeId = 9;
    private const int SaleRefundStockTransactionTypeId = 8;
    private const int StrictFullPaymentAccountTypeId = 10;
    private const string DefaultChequePhotoPath = "/images/journalentry/default.png";

    private IQueryable<Sales> BuildSaleGridQuery()
    {
        return _db.Sales
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.Currency);
    }

    private static IQueryable<Sales> ApplyCreatedTodayFilter(IQueryable<Sales> query)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return query.Where(x => x.CreationDate >= today && x.CreationDate < tomorrow);
    }

    private IQueryable<SaleGridRow> ProjectSaleGridRows(IQueryable<Sales> query)
    {
        return query
            .OrderByDescending(x => x.CreationDate)
            .Select(x => new SaleGridRow
            {
                ID = x.ID,
                SaleNo = x.SaleNo,
                IsHolded = x.IsHolded,
                IsRefunded = x.IsRefunded,
                AccountID = x.AccountID,
                CurrencyID = x.CurrencyID,
                AccountName = x.Account != null ? x.Account.Name : string.Empty,
                AccountCode = x.Account != null ? x.Account.Code : string.Empty,
                AccountTypeID = x.Account != null ? x.Account.AccountTypeID : 0,
                AccountTypeName = x.Account != null && x.Account.AccountType != null ? x.Account.AccountType.Name : string.Empty,
                CurrencyName = x.Currency != null ? x.Currency.CurrencyName : string.Empty,
                TotalAmount = x.TotalAmount,
                ReceivedAmount = x.ReceivedAmount,
                RemainingAmount = x.RemainingAmount,
                ItemCount = _db.SalesDetails.Count(d => d.SaleID == x.ID),
                Remarks = x.Remarks,
                CreationDate = x.CreationDate
            });
    }

    private static string BuildSaleJournalRemarks(int saleNo, string remarks)
    {
        return ("فروش نمبر " + saleNo + (remarks ?? string.Empty)).Trim();
    }

    private static string BuildSaleRefundJournalRemarks(int saleNo, string remarks)
    {
        return ("د فروش واپسي نمبر " + saleNo + (remarks ?? string.Empty)).Trim();
    }

    private static string BuildSaleReceiveJournalRemarks(int saleNo, string remarks)
    {
        return ("د فروش رسید نمبر " + saleNo + " " + (remarks ?? string.Empty)).Trim();
    }

    private async Task<int?> ResolveTreasureAccountIdAsync(Sales sale)
    {
        if (sale.ReceivedAmount <= 0)
            return null;

        var saleJournalRemarks = BuildSaleJournalRemarks(sale.SaleNo, sale.Remarks);
        var receiveJournalRemarks = BuildSaleReceiveJournalRemarks(sale.SaleNo, sale.Remarks);

        return await _db.JournalEntries
            .AsNoTracking()
            .Where(x =>
                (x.TransactionTypeID == SaleTransactionTypeId ||
                 x.TransactionTypeID == SaleChangeTransactionTypeId) &&
                (x.Remarks == saleJournalRemarks || x.Remarks == receiveJournalRemarks) &&
                x.Credit > 0 &&
                x.Debit == 0 &&
                x.AccountBalance.AccountID != sale.AccountID &&
                x.AccountBalance.CurrencyID == sale.CurrencyID)
            .OrderByDescending(x => x.ID)
            .Select(x => (int?)x.AccountBalance.AccountID)
            .FirstOrDefaultAsync();
    }

    private async Task<decimal?> GetLatestItemSalePriceAsync(int itemId)
    {
        return await _db.ItemsPrices
            .AsNoTracking()
            .Where(x => x.ItemID == itemId)
            .OrderByDescending(x => x.CreationDate)
            .ThenByDescending(x => x.ID)
            .Select(x => (decimal?)x.Price)
            .FirstOrDefaultAsync();
    }

    private async Task<decimal?> GetUnitExchangeAmountAsync(int itemId, int? unitConversionId)
    {
        if (unitConversionId is null or <= 0)
            return 1m;

        var unitInfo = await _db.UnitConversion
            .AsNoTracking()
            .Where(x => x.ID == unitConversionId.Value && x.ItemID == itemId)
            .Select(x => new
            {
                x.ExchangedAmount,
                x.MainAmount,
                x.SubAmount
            })
            .FirstOrDefaultAsync();

        if (unitInfo is null)
            return null;

        var exchangeAmount = unitInfo.ExchangedAmount;
        if (exchangeAmount <= 0 && unitInfo.MainAmount > 0 && unitInfo.SubAmount > 0)
            exchangeAmount = unitInfo.SubAmount / unitInfo.MainAmount;

        return exchangeAmount > 0 ? exchangeAmount : null;
    }

    private async Task<decimal?> GetLatestPurchaseUnitPriceAsync(int itemId, int? unitConversionId, DateTime? effectiveDate = null)
    {
        var purchaseDetailQuery = _db.PurchaseDetails
            .AsNoTracking()
            .Where(x => x.ItemID == itemId);

        if (effectiveDate.HasValue)
            purchaseDetailQuery = purchaseDetailQuery.Where(x => x.CreationDate <= effectiveDate.Value);

        var latestPurchaseDetail = await purchaseDetailQuery
            .OrderByDescending(x => x.CreationDate)
            .ThenByDescending(x => x.ID)
            .Select(x => new
            {
                x.PerPrice,
                PurchaseUnitConversionId = (int?)x.UnitConversionID
            })
            .FirstOrDefaultAsync();

        if (latestPurchaseDetail is null)
            return null;

        var sourceExchangeAmount = await GetUnitExchangeAmountAsync(itemId, latestPurchaseDetail.PurchaseUnitConversionId);
        var targetExchangeAmount = await GetUnitExchangeAmountAsync(itemId, unitConversionId);

        if (!sourceExchangeAmount.HasValue || !targetExchangeAmount.HasValue)
            return null;

        var mainUnitPrice = latestPurchaseDetail.PerPrice * sourceExchangeAmount.Value;
        return mainUnitPrice / targetExchangeAmount.Value;
    }

    private async Task<List<SaleDetailResponse>> BuildSaleDetailResponsesAsync(Sales sale)
    {
        var details = await _db.SalesDetails
            .AsNoTracking()
            .Where(x => x.SaleID == sale.ID)
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (details.Count == 0)
            return [];

        var itemIds = details.Select(x => x.ItemID).Distinct().ToArray();
        var itemsById = await _db.Items
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ID))
            .ToDictionaryAsync(x => x.ID);

        var storedUnitConversionIds = details
            .Where(x => x.UnitConversionID > 0)
            .Select(x => x.UnitConversionID)
            .Distinct()
            .ToArray();

        var unitConversions = await _db.UnitConversion
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemID) || storedUnitConversionIds.Contains(x.ID))
            .OrderBy(x => x.ID)
            .ToListAsync();

        var stockTransactions = await _db.StockTransactions
            .AsNoTracking()
            .Include(x => x.StockBalance)
            .Where(x =>
                x.TransactionID == SaleStockTransactionTypeId &&
                x.CreatedByUserId == sale.CreatedByUserId &&
                x.CreationDate >= sale.CreationDate.AddMinutes(-10) &&
                x.CreationDate <= sale.CreationDate.AddMinutes(10) &&
                x.StockBalance != null &&
                itemIds.Contains(x.StockBalance.ItemID))
            .OrderBy(x => x.ID)
            .ToListAsync();

        var usedStockTransactionIds = new HashSet<int>();
        var responses = new List<SaleDetailResponse>(details.Count);

        foreach (var detail in details)
        {
            var matchedStockTransaction = stockTransactions
                .Where(x =>
                    !usedStockTransactionIds.Contains(x.ID) &&
                    x.StockBalance != null &&
                    x.StockBalance.ItemID == detail.ItemID &&
                    x.StockBalance.WarehouseID == detail.WarehouseID &&
                    x.Quantity == detail.Quantity &&
                    (x.Remarks ?? string.Empty) == (detail.Remarks ?? string.Empty))
                .OrderBy(x => Math.Abs((x.CreationDate - detail.CreationDate).Ticks))
                .ThenBy(x => x.ID)
                .FirstOrDefault();

            if (matchedStockTransaction is null)
            {
                matchedStockTransaction = stockTransactions
                    .Where(x =>
                        !usedStockTransactionIds.Contains(x.ID) &&
                        x.StockBalance != null &&
                        x.StockBalance.ItemID == detail.ItemID &&
                        x.StockBalance.WarehouseID == detail.WarehouseID)
                    .OrderBy(x => Math.Abs((x.CreationDate - detail.CreationDate).Ticks))
                    .ThenBy(x => x.ID)
                    .FirstOrDefault();
            }

            if (matchedStockTransaction is not null)
                usedStockTransactionIds.Add(matchedStockTransaction.ID);

            int? unitConversionId = null;
            if (detail.UnitConversionID > 0 &&
                unitConversions.Any(x => x.ID == detail.UnitConversionID && x.ItemID == detail.ItemID))
            {
                unitConversionId = detail.UnitConversionID;
            }
            else if (matchedStockTransaction is not null)
            {
                unitConversionId = unitConversions
                    .Where(x => x.ItemID == detail.ItemID && x.SubUnitID == matchedStockTransaction.UnitID)
                    .Select(x => (int?)x.ID)
                    .FirstOrDefault();

                if (unitConversionId is null &&
                    itemsById.TryGetValue(detail.ItemID, out var item) &&
                    matchedStockTransaction.UnitID == item.UnitId)
                {
                    unitConversionId = 0;
                }
            }

            responses.Add(new SaleDetailResponse
            {
                ItemID = detail.ItemID,
                UnitConversionID = unitConversionId,
                Quantity = detail.Quantity,
                UnitPrice = detail.PerPrice,
                TotalPrice = detail.TotalPrice,
                WarehouseID = detail.WarehouseID,
                Remarks = detail.Remarks ?? string.Empty
            });
        }

        return responses;
    }

    [HttpGet("GetToday")]
    public async Task<object> GetToday(DateTime? fromDate, DateTime? toDate, int? accountId)
    {
        var query = BuildSaleGridQuery();
        var hasAccountFilter = accountId.HasValue && accountId.Value > 0;
        var hasDateRangeFilter = fromDate.HasValue && toDate.HasValue;

        if (hasAccountFilter)
        {
            query = query.Where(x => x.AccountID == accountId.Value);
        }

        if (hasDateRangeFilter)
        {
            var from = fromDate.Value.Date;
            var toExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreationDate >= from && x.CreationDate < toExclusive);
        }
        else if (!hasAccountFilter)
        {
            query = ApplyCreatedTodayFilter(query);
        }

        return await ProjectSaleGridRows(query).ToListAsync();
    }

    [HttpGet("GetCreatedToday")]
    public async Task<object> GetCreatedToday()
    {
        var query = ApplyCreatedTodayFilter(BuildSaleGridQuery());
        return await ProjectSaleGridRows(query).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var sale = await _db.Sales
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id);

        if (sale is null)
            return NotFound(new { Message = "فروش ونه موندل شو." });

        var accountInfo = await _db.Accounts
            .AsNoTracking()
            .Where(x => x.ID == sale.AccountID)
            .Select(x => new { x.Name, x.Code })
            .FirstOrDefaultAsync();

        var currencyInfo = await _db.Currencies
            .AsNoTracking()
            .Where(x => x.ID == sale.CurrencyID)
            .Select(x => x.CurrencyName)
            .FirstOrDefaultAsync();

        var details = await BuildSaleDetailResponsesAsync(sale);
        var treasureAccountId = await ResolveTreasureAccountIdAsync(sale);

        return Ok(new SaleResponse
        {
            SaleID = sale.ID,
            SaleNo = sale.SaleNo,
            AccountID = sale.AccountID,
            AccountName = accountInfo is null
                ? string.Empty
                : string.IsNullOrWhiteSpace(accountInfo.Code)
                    ? accountInfo.Name ?? string.Empty
                    : (accountInfo.Name ?? string.Empty) + " - " + accountInfo.Code,
            TreasureAccountID = treasureAccountId,
            IsHolded = sale.IsHolded,
            CurrencyID = sale.CurrencyID,
            CurrencyName = currencyInfo ?? string.Empty,
            SaleDate = sale.CreationDate,
            TotalAmount = sale.TotalAmount,
            ReceivedAmount = sale.ReceivedAmount,
            RemainingAmount = sale.RemainingAmount,
            Remarks = sale.Remarks ?? string.Empty,
            Details = details
        });
    }

    [HttpGet("item-pricing/{itemId:int}")]
    public async Task<IActionResult> GetItemPricing(int itemId, [FromQuery] int? unitConversionId, [FromQuery] DateTime? saleDate)
    {
        var itemExists = await _db.Items
            .AsNoTracking()
            .AnyAsync(x => x.ID == itemId && x.IsActive);

        if (!itemExists)
            return NotFound();

        var saleUnitPrice = await GetLatestItemSalePriceAsync(itemId);
        var purchaseUnitPrice = await GetLatestPurchaseUnitPriceAsync(itemId, unitConversionId, saleDate);

        return Ok(new
        {
            ItemID = itemId,
            UnitConversionID = unitConversionId,
            SaleUnitPrice = saleUnitPrice,
            PurchaseUnitPrice = purchaseUnitPrice
        });
    }

    [HttpGet("next-no")]
    public async Task<IActionResult> GetNextNo()
    {
        var lastSaleNo = await _db.Sales
            .Select(p => (int?)p.SaleNo)
            .MaxAsync() ?? 0;

        return Ok(new { SaleNo = lastSaleNo + 1 });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaleSaveRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Message = "سیسټم ته ننوزئ" });

        var validationMessage = ValidateRequest(request, requireTreasureRules: !request.IsHolded);
        if (!string.IsNullOrWhiteSpace(validationMessage))
            return BadRequest(new { Message = validationMessage });

        var configurationMessage = await ValidateSaleConfigurationAsync();
        if (!string.IsNullOrWhiteSpace(configurationMessage))
            return BadRequest(new { Message = configurationMessage });

        if (await _db.Sales.AnyAsync(p => p.SaleNo == request.SaleNo))
            return BadRequest(new { Message = "د فروش شمېره مخکې ثبت سوې ده." });

        var referenceValidation = await ValidateSaleReferencesAsync(request);
        if (!referenceValidation.Success)
            return BadRequest(new { Message = referenceValidation.ErrorMessage });

        SaleOrder selectedOrder = null;
        if (request.OrderID is > 0)
        {
            selectedOrder = await _db.SalesOrders.FirstOrDefaultAsync(x => x.ID == request.OrderID.Value);
            if (selectedOrder is null)
                return BadRequest(new { Message = "فروش آرډر ونه موندل شو." });
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        var preparedDetailsResult = await PrepareSaleDetailsAsync(request, userId);
        if (!preparedDetailsResult.Success)
            return BadRequest(new { Message = preparedDetailsResult.ErrorMessage });

        var sale = new Sales
        {
            SaleNo = request.SaleNo,
            AccountID = request.AccountID,
            CurrencyID = request.CurrencyID,
            IsHolded = request.IsHolded,
            Remarks = request.Remarks?.Trim() ?? string.Empty,
            TotalAmount = request.TotalAmount,
            ReceivedAmount = request.ReceivedAmount,
            RemainingAmount = request.RemainingAmount,
            CreatedByUserId = userId,
            CreationDate = request.SaleDate
        };

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();

        var applyResult = await ApplySaleEffectsAsync(
            sale,
            request,
            userId,
            referenceValidation.Account!,
            preparedDetailsResult.PreparedDetails!,
            applyFinancialEffects: !request.IsHolded);

        if (!applyResult.Success)
        {
            await tx.RollbackAsync();
            return BadRequest(new { Message = applyResult.ErrorMessage });
        }

        if (selectedOrder is not null)
            selectedOrder.IsCompleted = true;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            Message = "فروش په بریالیتوب ثبت شو.",
            SaleID = sale.ID,
            sale.SaleNo,
            AccountBalance = applyResult.AccountBalance,
            TreasureAccountBalance = applyResult.TreasureAccountBalance
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaleSaveRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Message = "سیسټم ته ننوزئ." });

        var validationMessage = ValidateRequest(request, requireTreasureRules: !request.IsHolded);
        if (!string.IsNullOrWhiteSpace(validationMessage))
            return BadRequest(new { Message = validationMessage });

        var configurationMessage = await ValidateSaleConfigurationAsync();
        if (!string.IsNullOrWhiteSpace(configurationMessage))
            return BadRequest(new { Message = configurationMessage });

        var sale = await _db.Sales.FirstOrDefaultAsync(x => x.ID == id);
        if (sale is null)
            return NotFound(new { Message = "فروش ونه موندل شو." });

        if (!sale.IsHolded)
            return BadRequest(new { Message = "دا فروش هولډ سوی نه دی." });

        if (sale.IsRefunded)
            return BadRequest(new { Message = "واپسي سوی فروش سمېدای نه سي." });

        if (await _db.Sales.AnyAsync(p => p.ID != id && p.SaleNo == request.SaleNo))
            return BadRequest(new { Message = "د فروش شمېره مخکې ثبت سوې ده." });

        var referenceValidation = await ValidateSaleReferencesAsync(request);
        if (!referenceValidation.Success)
            return BadRequest(new { Message = referenceValidation.ErrorMessage });

        if (!request.IsHolded && request.ReceivedAmount > 0 && request.TreasureAccountID is not > 0)
            return BadRequest(new { Message = "که رسید له صفر څخه زیات وي، تجري/بانک حساب لا هم ضروري دی." });

        var saleDetails = await _db.SalesDetails
            .Where(x => x.SaleID == sale.ID)
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (saleDetails.Count == 0)
            return BadRequest(new { Message = "د فروش تفصیلات ونه موندل شول." });

        var existingEffectsResult = await LoadExistingSaleEffectsAsync(
            sale,
            saleDetails,
            includeJournalEntries: !sale.IsHolded);
        if (!existingEffectsResult.Success)
            return BadRequest(new { Message = existingEffectsResult.ErrorMessage });

        await using var tx = await _db.Database.BeginTransactionAsync();
        var preparedDetailsResult = await PrepareSaleDetailsAsync(request, userId);
        if (!preparedDetailsResult.Success)
            return BadRequest(new { Message = preparedDetailsResult.ErrorMessage });

        var reverseResult = await ReverseSaleEffectsAsync(
            existingEffectsResult.StockEffects!,
            existingEffectsResult.JournalEntries!,
            reverseFinancialEffects: !sale.IsHolded);

        if (!reverseResult.Success)
        {
            await tx.RollbackAsync();
            return BadRequest(new { Message = reverseResult.ErrorMessage });
        }

        _db.SalesDetails.RemoveRange(saleDetails);
        _db.StockTransactions.RemoveRange(existingEffectsResult.StockTransactions!);
        if (existingEffectsResult.JournalEntries!.Count > 0)
            _db.JournalEntries.RemoveRange(existingEffectsResult.JournalEntries!);

        sale.SaleNo = request.SaleNo;
        sale.AccountID = request.AccountID;
        sale.CurrencyID = request.CurrencyID;
        sale.IsHolded = request.IsHolded;
        sale.Remarks = request.Remarks?.Trim() ?? string.Empty;
        sale.TotalAmount = request.TotalAmount;
        sale.ReceivedAmount = request.ReceivedAmount;
        sale.RemainingAmount = request.RemainingAmount;
        sale.CreationDate = request.SaleDate;

        var applyResult = await ApplySaleEffectsAsync(
            sale,
            request,
            userId,
            referenceValidation.Account!,
            preparedDetailsResult.PreparedDetails!,
            applyFinancialEffects: !request.IsHolded);

        if (!applyResult.Success)
        {
            await tx.RollbackAsync();
            return BadRequest(new { Message = applyResult.ErrorMessage });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            Message = "فروش په بریالیتوب سم شو.",
            SaleID = sale.ID,
            sale.SaleNo,
            AccountBalance = applyResult.AccountBalance,
            TreasureAccountBalance = applyResult.TreasureAccountBalance
        });
    }

    [HttpPost("{id:int}/refund")]
    public async Task<IActionResult> Refund(int id, [FromBody] SaleRefundRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Message = "مهرباني وکړئ بیا سیسټم ته ننوزئ." });

        if (request is null)
            return BadRequest(new { Message = "د واپسي غوښتنه ضروري ده." });

        if (request.RefundAmount < 0)
            return BadRequest(new { Message = "د واپسي مبلغ منفي کېدای نه شي." });

        if (request.RefundAmount > 0 && request.TreasureAccountID is not > 0)
            return BadRequest(new { Message = "کله چې د واپسي مبلغ له صفر څخه زیات وي، تجري/بانک حساب ضروري دی." });

        if (request.RefundAmount <= 0 && request.TreasureAccountID is > 0)
            return BadRequest(new { Message = "کله چې تجري/بانک حساب انتخاب وي، د واپسي مبلغ باید له صفر څخه زیات وي." });

        var configurationMessage = await ValidateSaleRefundConfigurationAsync();
        if (!string.IsNullOrWhiteSpace(configurationMessage))
            return BadRequest(new { Message = configurationMessage });

        var sale = await _db.Sales.FirstOrDefaultAsync(x => x.ID == id);
        if (sale is null)
            return NotFound(new { Message = "فروش ونه موندل شو." });

        if (sale.IsRefunded)
            return BadRequest(new { Message = "د دې فروش واپسي مخکې لا شوې ده." });

        if (sale.IsHolded && request.RefundAmount > 0)
            return BadRequest(new { Message = "د هولډ شوي فروش لپاره د واپسي نغدي قیدونه نه شي ثبتېدای." });

        if (!sale.IsHolded && request.RefundAmount > sale.ReceivedAmount)
            return BadRequest(new { Message = "د واپسي مبلغ د فروش له رسید شوې اندازې څخه زیات کېدای نه شي." });

        if (request.TreasureAccountID is > 0 &&
            !await _db.Accounts.AnyAsync(a => a.ID == request.TreasureAccountID.Value))
            return BadRequest(new { Message = "تجري/بانک حساب ونه موندل شو." });

        var saleDetails = await _db.SalesDetails
            .Where(x => x.SaleID == sale.ID)
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (saleDetails.Count == 0)
            return BadRequest(new { Message = "د فروش تفصیلات ونه موندل شول." });

        var existingEffectsResult = await LoadExistingSaleEffectsAsync(
            sale,
            saleDetails,
            includeJournalEntries: false);

        if (!existingEffectsResult.Success)
            return BadRequest(new { Message = existingEffectsResult.ErrorMessage });

        await using var tx = await _db.Database.BeginTransactionAsync();

        foreach (var stockEffect in existingEffectsResult.StockEffects!)
        {

            stockEffect.StockBalance.Quantity += stockEffect.MainStockQuantity;

            _db.StockTransactions.Add(new StockTransactions
            {
                StockBalanceID = stockEffect.StockBalance.ID,
                Quantity = stockEffect.StockTransaction.Quantity,
                Remarks = stockEffect.StockTransaction.Remarks ?? string.Empty,
                UnitID = stockEffect.StockTransaction.UnitID,
                TransactionID = SaleRefundStockTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = DateTime.Now
            });
        }

        decimal? accountBalanceValue = null;
        decimal? treasureBalanceValue = null;

        if (!sale.IsHolded)
        {
            var refundDate = DateTime.Now;
            var accountBalance = await GetOrCreateAccountBalanceAsync(sale.AccountID, sale.CurrencyID, userId, refundDate);
            var refundRemarks = BuildSaleRefundJournalRemarks(sale.SaleNo, sale.Remarks);

            accountBalance.Balance -= sale.TotalAmount;
            _db.JournalEntries.Add(new JournalEntry
            {
                AccountBalanceID = accountBalance.ID,
                Debit = sale.TotalAmount,
                Credit = 0,
                Balance = accountBalance.Balance,
                Remarks = refundRemarks,
                ChequePhoto = DefaultChequePhotoPath,
                TransactionTypeID = SaleRefundTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = refundDate
            });

            if (request.RefundAmount > 0)
            {
                accountBalance.Balance += request.RefundAmount;
                _db.JournalEntries.Add(new JournalEntry
                {
                    AccountBalanceID = accountBalance.ID,
                    Debit = 0,
                    Credit = request.RefundAmount,
                    Balance = accountBalance.Balance,
                    Remarks = refundRemarks,
                    ChequePhoto = DefaultChequePhotoPath,
                    TransactionTypeID = SaleRefundTransactionTypeId,
                    CreatedByUserId = userId,
                    CreationDate = refundDate
                });

                var treasureBalance = await GetOrCreateAccountBalanceAsync(
                    request.TreasureAccountID!.Value,
                    sale.CurrencyID,
                    userId,
                    refundDate);

                treasureBalance.Balance -= request.RefundAmount;
                _db.JournalEntries.Add(new JournalEntry
                {
                    AccountBalanceID = treasureBalance.ID,
                    Debit = request.RefundAmount,
                    Credit = 0,
                    Balance = treasureBalance.Balance,
                    Remarks = refundRemarks,
                    ChequePhoto = DefaultChequePhotoPath,
                    TransactionTypeID = SaleRefundTransactionTypeId,
                    CreatedByUserId = userId,
                    CreationDate = refundDate
                });

                treasureBalanceValue = treasureBalance.Balance;
            }

            accountBalanceValue = accountBalance.Balance;
        }

        sale.IsRefunded = true;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            Message = "فروش واپس کړل سو.",
            SaleID = sale.ID,
            sale.SaleNo,
            AccountBalance = accountBalanceValue,
            TreasureAccountBalance = treasureBalanceValue
        });
    }

    [HttpPost("{id:int}/receive")]
    public async Task<IActionResult> Receive(int id, [FromBody] SaleReceiveRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Message = "Ù…Ù‡Ø±Ø¨Ø§Ù†ÙŠ ÙˆÚ©Ú“Ø¦ Ø¨ÛŒØ§ Ø³ÛŒØ³Ù¼Ù… ØªÙ‡ Ù†Ù†ÙˆØ²Ø¦." });

        if (request is null)
            return BadRequest(new { Message = "Ø¯ Ø±Ø³ÛŒØ¯ ØºÙˆÚšØªÙ†Ù‡ Ø¶Ø±ÙˆØ±ÙŠ Ø¯Ù‡." });

        if (request.ReceiveAmount <= 0)
            return BadRequest(new { Message = "Ø¯ Ø±Ø³ÛŒØ¯ Ù…Ø¨Ù„Øº Ø¨Ø§ÛŒØ¯ Ù„Ù‡ ØµÙØ± Ú…Ø®Ù‡ Ø²ÛŒØ§Øª ÙˆÙŠ." });

        if (request.TreasureAccountID is not > 0)
            return BadRequest(new { Message = "ØªØ¬Ø±ÙŠ/Ø¨Ø§Ù†Ú© Ø­Ø³Ø§Ø¨ Ø¶Ø±ÙˆØ±ÙŠ Ø¯ÛŒ." });

        var sale = await _db.Sales.FirstOrDefaultAsync(x => x.ID == id);
        if (sale is null)
            return NotFound(new { Message = "Ø®Ø±ÛŒØ¯ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ." });

        if (sale.IsRefunded)
            return BadRequest(new { Message = "Ø¯ ÙˆØ§Ù¾Ø³ Ø´ÙˆÙŠ Ø®Ø±ÛŒØ¯ Ù„Ù¾Ø§Ø±Ù‡ Ø±Ø³ÛŒØ¯ Ù†Ù‡ Ø´ÙŠ Ø«Ø¨ØªÛØ¯Ø§ÛŒ." });

        if (sale.IsHolded)
            return BadRequest(new { Message = "Ø¯ Ù‡ÙˆÙ„Ú‰ Ø´ÙˆÙŠ Ø®Ø±ÛŒØ¯ Ù„Ù¾Ø§Ø±Ù‡ Ø±Ø³ÛŒØ¯ Ù†Ù‡ Ø´ÙŠ Ø«Ø¨ØªÛØ¯Ø§ÛŒ." });

        if (sale.ReceivedAmount >= sale.TotalAmount && sale.TotalAmount > 0)
            return BadRequest(new { Message = "د دې فروش ټول مبلغ لا مخکې بشپړ رسید شوی دی." });

        if (sale.RemainingAmount <= 0)
            return BadRequest(new { Message = "Ø¯ Ø¯Û Ø®Ø±ÛŒØ¯ Ø¨Ø§Ù‚ÙŠ Ù…Ø¨Ù„Øº ØµÙØ± Ø¯ÛŒ." });

        if (request.ReceiveAmount > sale.RemainingAmount)
            return BadRequest(new { Message = "Ø¯ Ø±Ø³ÛŒØ¯ Ù…Ø¨Ù„Øº Ø¯ Ø®Ø±ÛŒØ¯ Ù„Ù‡ Ø¨Ø§Ù‚ÙŠ Ø§Ù†Ø¯Ø§Ø²Û Ú…Ø®Ù‡ Ø²ÛŒØ§Øª Ú©ÛØ¯Ø§ÛŒ Ù†Ù‡ Ø´ÙŠ." });

        if (!await _db.Accounts.AnyAsync(a => a.ID == request.TreasureAccountID.Value))
            return BadRequest(new { Message = "ØªØ¬Ø±ÙŠ/Ø¨Ø§Ù†Ú© Ø­Ø³Ø§Ø¨ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ." });

        var configurationMessage = await ValidateSaleConfigurationAsync();
        if (!string.IsNullOrWhiteSpace(configurationMessage))
            return BadRequest(new { Message = configurationMessage });

        var effectiveDate = DateTime.Now;
        var accountBalance = await GetOrCreateAccountBalanceAsync(sale.AccountID, sale.CurrencyID, userId, effectiveDate);
        var treasureBalance = await GetOrCreateAccountBalanceAsync(
            request.TreasureAccountID.Value,
            sale.CurrencyID,
            userId,
            effectiveDate);


        var journalRemarks = BuildSaleReceiveJournalRemarks(sale.SaleNo, sale.Remarks);
        await using var tx = await _db.Database.BeginTransactionAsync();

        accountBalance.Balance -= request.ReceiveAmount;
        treasureBalance.Balance += request.ReceiveAmount;

        _db.JournalEntries.Add(new JournalEntry
        {
            AccountBalanceID = accountBalance.ID,
            Debit = request.ReceiveAmount,
            Credit = 0,
            Balance = accountBalance.Balance,
            Remarks = journalRemarks,
            ChequePhoto = DefaultChequePhotoPath,
            TransactionTypeID = SaleChangeTransactionTypeId,
            CreatedByUserId = userId,
            CreationDate = effectiveDate
        });

        _db.JournalEntries.Add(new JournalEntry
        {
            AccountBalanceID = treasureBalance.ID,
            Debit = 0,
            Credit = request.ReceiveAmount,
            Balance = treasureBalance.Balance,
            Remarks = journalRemarks,
            ChequePhoto = DefaultChequePhotoPath,
            TransactionTypeID = SaleChangeTransactionTypeId,
            CreatedByUserId = userId,
            CreationDate = effectiveDate
        });

        sale.ReceivedAmount += request.ReceiveAmount;
        sale.RemainingAmount -= request.ReceiveAmount;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            Message = "Ø¯ Ø®Ø±ÛŒØ¯ Ø±Ø³ÛŒØ¯ Ø«Ø¨Øª Ø´Ùˆ.",
            SaleID = sale.ID,
            sale.SaleNo,
            sale.ReceivedAmount,
            sale.RemainingAmount,
            AccountBalance = accountBalance.Balance,
            TreasureAccountBalance = treasureBalance.Balance
        });
    }

    private async Task<string> ValidateSaleConfigurationAsync()
    {
        if (!await _db.JournalEntryTransactionTypes.AnyAsync(x => x.ID == SaleTransactionTypeId))
            return $"اړین د ورځني معاملاتو ډول {SaleTransactionTypeId} ونه موندل سو.";

        if (!await _db.JournalEntryTransactionTypes.AnyAsync(x => x.ID == SaleChangeTransactionTypeId))
            return $"اړین د ورځني معاملاتو ډول {SaleChangeTransactionTypeId} ونه موندل سو.";

        if (!await _db.StockTransactionTypes.AnyAsync(x => x.ID == SaleStockTransactionTypeId))
            return $"اړین د سټاک معاملې ډول {SaleStockTransactionTypeId} ونه موندل سو.";

        return string.Empty;
    }

    private async Task<string> ValidateSaleRefundConfigurationAsync()
    {
        if (!await _db.JournalEntryTransactionTypes.AnyAsync(x => x.ID == SaleRefundTransactionTypeId))
            return $"اړین د ورځني معاملاتو ډول {SaleRefundTransactionTypeId} ونه موندل سو.";

        if (!await _db.StockTransactionTypes.AnyAsync(x => x.ID == SaleRefundStockTransactionTypeId))
            return $"اړین د سټاک معاملې ډول {SaleRefundStockTransactionTypeId} ونه موندل سو.";

        return string.Empty;
    }

    private async Task<(bool Success, string ErrorMessage, Account Account)> ValidateSaleReferencesAsync(SaleSaveRequest request)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .Include(a => a.AccountType)
            .FirstOrDefaultAsync(a => a.ID == request.AccountID);
        if (account is null)
            return (false, "حساب ونه موندل سو.", null);

        if (account.AccountTypeID is not 3 and not 5 and not StrictFullPaymentAccountTypeId)
            return (false, "Ø¯ ÙØ±ÙˆØ´ Ù„Ù¾Ø§Ø±Ù‡ Ù¼Ø§Ú©Ù„ Ø´ÙˆÛŒ Ø­Ø³Ø§Ø¨ Ù†Ø§Ø³Ù… Ø¯ÛŒ.", null);

        if (!await _db.Currencies.AnyAsync(c => c.ID == request.CurrencyID))
            return (false, "اسعار ونه موندل سو.", null);

        if (request.TreasureAccountID is > 0 &&
            !await _db.Accounts.AnyAsync(a => a.ID == request.TreasureAccountID.Value))
            return (false, "تجرئ/بانک ونه موندل سو.", null);

        if (!request.IsHolded &&
            account.AccountTypeID == StrictFullPaymentAccountTypeId &&
            request.ReceivedAmount != request.TotalAmount)
            return (false, "د حساب ډول 10 لپاره رسید مبلغ باید له مجموعې سره مساوي وي.", null);

        return (true, string.Empty, account);
    }

    private async Task<(bool Success, string ErrorMessage, List<PreparedSaleDetail> PreparedDetails)> PrepareSaleDetailsAsync(SaleSaveRequest request, string userId)
    {
        var itemIds = request.Details.Select(d => d.ItemID).Distinct().ToArray();
        var warehouseIds = request.Details.Select(d => d.WarehouseID).Distinct().ToArray();

        if (await _db.Items.CountAsync(i => itemIds.Contains(i.ID)) != itemIds.Length)
            return (false, "په جنسونو کې ناسم انتخاب شته.", null);

        if (await _db.WareHouses.CountAsync(w => warehouseIds.Contains(w.ID)) != warehouseIds.Length)
            return (false, "په ګدامونو کې ناسم انتخاب شته.", null);

        var itemsById = await _db.Items
            .Where(i => itemIds.Contains(i.ID))
            .ToDictionaryAsync(i => i.ID);

        var preparedDetails = new List<PreparedSaleDetail>(request.Details.Count);
        foreach (var detail in request.Details)
        {
            if (!itemsById.TryGetValue(detail.ItemID, out var item))
                return (false, "په جنسونو کې ناسم انتخاب شته.", null);

            var (mainStockQuantity, unitId, unitConversion, unitError) =
                await ResolveSaleUnitAsync(detail.UnitConversionID!.Value, detail.Quantity, item, userId);
            if (!string.IsNullOrWhiteSpace(unitError))
                return (false, unitError, null);

            preparedDetails.Add(new PreparedSaleDetail
            {
                Detail = detail,
                MainStockQuantity = mainStockQuantity,
                UnitId = unitId,
                UnitConversion = unitConversion,
                Remarks = (detail.Remarks ?? string.Empty).Trim()
            });
        }

        return (true, string.Empty, preparedDetails);
    }

    private async Task<(bool Success, string ErrorMessage, List<ExistingSaleStockEffect> StockEffects, List<StockTransactions> StockTransactions, List<JournalEntry> JournalEntries)> LoadExistingSaleEffectsAsync(
        Sales sale,
        List<SaleDetails> saleDetails,
        bool includeJournalEntries)
    {
        var itemIds = saleDetails.Select(x => x.ItemID).Distinct().ToArray();
        var itemsById = await _db.Items
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ID))
            .ToDictionaryAsync(x => x.ID);

        var stockTransactions = await _db.StockTransactions
            .Include(x => x.StockBalance)
            .Where(x =>
                x.TransactionID == SaleStockTransactionTypeId &&
                x.CreatedByUserId == sale.CreatedByUserId &&
                x.CreationDate >= sale.CreationDate.AddMinutes(-10) &&
                x.CreationDate <= sale.CreationDate.AddMinutes(10) &&
                x.StockBalance != null &&
                itemIds.Contains(x.StockBalance.ItemID))
            .OrderBy(x => x.ID)
            .ToListAsync();
        var unitConversionIds = saleDetails
            .Where(x => x.UnitConversionID > 0)
            .Select(x => x.UnitConversionID)
            .Distinct()
            .ToArray();
        var unitSubUnitByConversionId = await _db.UnitConversion
            .AsNoTracking()
            .Where(x => unitConversionIds.Contains(x.ID))
            .ToDictionaryAsync(x => x.ID, x => x.SubUnitID);


        var usedStockTransactionIds = new HashSet<int>();
        var stockEffects = new List<ExistingSaleStockEffect>(saleDetails.Count);

        foreach (var detail in saleDetails)
        {
            if (!itemsById.TryGetValue(detail.ItemID, out var item))
                return (false, "په جنسونو کې ناسم انتخاب شته.", null, null, null);

            var expectedUnitId = item.UnitId;
            if (detail.UnitConversionID > 0)
            {
                if (!unitSubUnitByConversionId.TryGetValue(detail.UnitConversionID, out expectedUnitId))
                    return (false, "د زوړ فروش د واحد تبادله ونه موندل شو.", null, null, null);
            }

            var matchedStockTransaction = stockTransactions
                .Where(x =>
                    !usedStockTransactionIds.Contains(x.ID) &&
                    x.StockBalance != null &&
                    x.StockBalance.ItemID == detail.ItemID &&
                    x.StockBalance.WarehouseID == detail.WarehouseID &&
                    x.UnitID == expectedUnitId &&
                    x.Quantity == detail.Quantity &&
                    (x.Remarks ?? string.Empty) == (detail.Remarks ?? string.Empty))
                .OrderBy(x => Math.Abs((x.CreationDate - detail.CreationDate).Ticks))
                .ThenBy(x => x.ID)
                .FirstOrDefault();

            if (matchedStockTransaction is null)
            {
                matchedStockTransaction = stockTransactions
                    .Where(x =>
                        !usedStockTransactionIds.Contains(x.ID) &&
                        x.StockBalance != null &&
                        x.StockBalance.ItemID == detail.ItemID &&
                        x.StockBalance.WarehouseID == detail.WarehouseID &&
                        x.UnitID == expectedUnitId)
                    .OrderBy(x => Math.Abs((x.CreationDate - detail.CreationDate).Ticks))
                    .ThenBy(x => x.ID)
                    .FirstOrDefault();
            }

            if (matchedStockTransaction is null || matchedStockTransaction.StockBalance is null)
                return (false, "د زوړ فروش د سټاک بدلونونه ونه موندل شو.", null, null, null);

            usedStockTransactionIds.Add(matchedStockTransaction.ID);

            var mainQuantityResult = await ResolveStoredTransactionMainQuantityAsync(item, matchedStockTransaction.Quantity, matchedStockTransaction.UnitID);
            if (!mainQuantityResult.Success)
                return (false, mainQuantityResult.ErrorMessage, null, null, null);

            stockEffects.Add(new ExistingSaleStockEffect
            {
                StockBalance = matchedStockTransaction.StockBalance,
                StockTransaction = matchedStockTransaction,
                MainStockQuantity = mainQuantityResult.MainStockQuantity
            });
        }

        var journalEntries = new List<JournalEntry>();
        if (includeJournalEntries)
        {
            var oldJournalRemarks = BuildSaleJournalRemarks(sale.SaleNo, sale.Remarks);
            journalEntries = await _db.JournalEntries
            .Include(x => x.AccountBalance)
            .Where(x =>
                x.TransactionTypeID == SaleTransactionTypeId &&
                x.CreatedByUserId == sale.CreatedByUserId &&
                x.Remarks == oldJournalRemarks &&
                x.CreationDate >= sale.CreationDate.AddMinutes(-10) &&
                x.CreationDate <= sale.CreationDate.AddMinutes(10))
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (journalEntries.Count == 0)
            return (false, "د زوړ فروش جرنل قیدونه ونه موندل شو.", null, null, null);

        }

        return (true, string.Empty, stockEffects, stockEffects.Select(x => x.StockTransaction).ToList(), journalEntries);
    }

    private async Task<(bool Success, string ErrorMessage)> ReverseSaleEffectsAsync(
        List<ExistingSaleStockEffect> stockEffects,
        List<JournalEntry> journalEntries,
        bool reverseFinancialEffects)
    {
        foreach (var stockEffect in stockEffects)
        {

            stockEffect.StockBalance.Quantity += stockEffect.MainStockQuantity;
        }

        if (!reverseFinancialEffects)
            return (true, string.Empty);

        foreach (var journalEntry in journalEntries)
        {
            if (journalEntry.AccountBalance is null)
                return (false, "د زوړ فروش د حساب بلانس قید ونه موندل شو.");

            journalEntry.AccountBalance.Balance += journalEntry.Debit - journalEntry.Credit;
        }

        return (true, string.Empty);
    }

    private async Task<(bool Success, string ErrorMessage, decimal? AccountBalance, decimal? TreasureAccountBalance)> ApplySaleEffectsAsync(
        Sales sale,
        SaleSaveRequest request,
        string userId,
        Account account,
        List<PreparedSaleDetail> preparedDetails,
        bool applyFinancialEffects)
    {
        var effectiveDate = request.SaleDate;

        foreach (var preparedDetail in preparedDetails)
        {
            var stockBalance = await _db.StockBalances.FirstOrDefaultAsync(sb =>
                sb.ItemID == preparedDetail.Detail.ItemID &&
                sb.WarehouseID == preparedDetail.Detail.WarehouseID);

            if (stockBalance is null || stockBalance.Quantity < preparedDetail.MainStockQuantity)
                return (false, "د فروش لپاره کافي سټاک نشته.", null, null);

            stockBalance.Quantity -= preparedDetail.MainStockQuantity;
            if (!string.IsNullOrWhiteSpace(preparedDetail.Remarks))
                stockBalance.Remarks = preparedDetail.Remarks;

            var purchaseUnitPrice = await GetLatestPurchaseUnitPriceAsync(
                preparedDetail.Detail.ItemID,
                preparedDetail.Detail.UnitConversionID,
                effectiveDate);
            var profit = purchaseUnitPrice.HasValue
                ? (preparedDetail.Detail.UnitPrice - purchaseUnitPrice.Value) * preparedDetail.Detail.Quantity
                : 0;

            _db.SalesDetails.Add(new SaleDetails
            {
                SaleID = sale.ID,
                ItemID = preparedDetail.Detail.ItemID,
                UnitConversion = preparedDetail.UnitConversion,
                Quantity = preparedDetail.Detail.Quantity,
                PerPrice = preparedDetail.Detail.UnitPrice,
                TotalPrice = preparedDetail.Detail.TotalPrice,
                Profit = profit,
                WarehouseID = preparedDetail.Detail.WarehouseID,
                Remarks = preparedDetail.Remarks,
                CreatedByUserId = userId,
                CreationDate = effectiveDate
            });

            _db.StockTransactions.Add(new StockTransactions
            {
                StockBalanceID = stockBalance.ID,
                Quantity = preparedDetail.Detail.Quantity,
                Remarks = preparedDetail.Remarks,
                UnitID = preparedDetail.UnitId,
                TransactionID = SaleStockTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = effectiveDate
            });
        }

        if (!applyFinancialEffects)
            return (true, string.Empty, null, null);

        var accountBalance = await GetOrCreateAccountBalanceAsync(request.AccountID, request.CurrencyID, userId, effectiveDate);
        var journalRemarks = BuildSaleJournalRemarks(request.SaleNo, request.Remarks);

        // An active sale should always write the full debit/credit trail so
        // JournalEntry balances and AccountBalances stay in sync even when the
        // selected account type requires full payment.
        accountBalance.Balance += request.TotalAmount;

        _db.JournalEntries.Add(new JournalEntry
        {
            AccountBalanceID = accountBalance.ID,
            Debit = 0,
            Credit = request.TotalAmount,
            Balance = accountBalance.Balance,
            Remarks = journalRemarks,
            ChequePhoto = DefaultChequePhotoPath,
            TransactionTypeID = SaleTransactionTypeId,
            CreatedByUserId = userId,
            CreationDate = effectiveDate
        });

        AccountBalance treasureAccountBalance = null;
        if (request.ReceivedAmount > 0)
        {
            treasureAccountBalance = await GetOrCreateAccountBalanceAsync(
                request.TreasureAccountID!.Value,
                request.CurrencyID,
                userId,
                effectiveDate);

            accountBalance.Balance -= request.ReceivedAmount;

            _db.JournalEntries.Add(new JournalEntry
            {
                AccountBalanceID = accountBalance.ID,
                Debit = request.ReceivedAmount,
                Credit = 0,
                Balance = accountBalance.Balance,
                Remarks = journalRemarks,
                ChequePhoto = DefaultChequePhotoPath,
                TransactionTypeID = SaleTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = effectiveDate
            });

            treasureAccountBalance.Balance += request.ReceivedAmount;
            _db.JournalEntries.Add(new JournalEntry
            {
                AccountBalanceID = treasureAccountBalance.ID,
                Debit = 0,
                Credit = request.ReceivedAmount,
                Balance = treasureAccountBalance.Balance,
                Remarks = journalRemarks,
                ChequePhoto = DefaultChequePhotoPath,
                TransactionTypeID = SaleTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = effectiveDate
            });
        }

        return (true, string.Empty, accountBalance.Balance, treasureAccountBalance?.Balance);
    }

    private async Task<AccountBalance> GetOrCreateAccountBalanceAsync(int accountId, int currencyId, string userId, DateTime effectiveDate)
    {
        var accountBalance = await _db.AccountBalances.FirstOrDefaultAsync(ab =>
            ab.AccountID == accountId &&
            ab.CurrencyID == currencyId);

        if (accountBalance is null)
        {
            accountBalance = new AccountBalance
            {
                AccountID = accountId,
                CurrencyID = currencyId,
                Balance = 0,
                CreatedByUserId = userId,
                CreationDate = effectiveDate
            };
            _db.AccountBalances.Add(accountBalance);
            await _db.SaveChangesAsync();
        }

        return accountBalance;
    }

    private async Task<(bool Success, string ErrorMessage, decimal MainStockQuantity)> ResolveStoredTransactionMainQuantityAsync(Item item, decimal quantity, int unitId)
    {
        if (unitId == item.UnitId)
            return (true, string.Empty, quantity);

        var unitConversion = await _db.UnitConversion
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ItemID == item.ID && u.SubUnitID == unitId);

        if (unitConversion is null)
            return (false, "د زوړ فروش د واحد تبادله ونه موندل شو.", 0);

        var exchangedAmount = unitConversion.ExchangedAmount;
        if (exchangedAmount <= 0 && unitConversion.MainAmount > 0 && unitConversion.SubAmount > 0)
            exchangedAmount = unitConversion.SubAmount / unitConversion.MainAmount;

        if (exchangedAmount <= 0)
            return (false, "د زوړ فروش د واحد تبادله ناسمه ده.", 0);

        return (true, string.Empty, quantity / exchangedAmount);
    }

    private static string ValidateRequest(SaleSaveRequest request, bool requireTreasureRules)
    {
        if (request is null) return "معلومات ندي رسېدلي.";
        if (request.SaleNo <= 0) return "د فروش شمېره ضروری ده.";
        if (request.AccountID <= 0) return "حساب انتخاب کړئ.";
        if (request.CurrencyID <= 0) return "اسعار انتخاب کړئ.";
        if (request.SaleDate == default) return "نېټه انتخاب کړئ.";
        if (request.Details is null || request.Details.Count == 0) return "لږ تر لږه یو قطار اضافه کړئ.";
        if (request.TotalAmount <= 0) return "مجموعه باید له صفر څخه زیاته وي.";
        if (request.ReceivedAmount < 0) return "رسید باید منفي نه وي.";
        if (request.ReceivedAmount > request.TotalAmount) return "رسید باید له مجموعې څخه زیات نه وي.";
        if (requireTreasureRules && request.TreasureAccountID is > 0 && request.ReceivedAmount <= 0) return "کله چې تجرئ/بانک انتخاب وي، رسید باید له صفر څخه زیات وي.";
        if (requireTreasureRules && (request.TreasureAccountID is null or <= 0) && request.ReceivedAmount > 0) return "کله چې رسید داخل وي، تجرئ/بانک انتخاب کړئ.";

        var calculatedTotal = 0m;
        foreach (var row in request.Details)
        {
            if (row.ItemID <= 0) return "په ټولو قطارونو کې جنس ضروری دی.";
            if (row.UnitConversionID is null || row.UnitConversionID < 0) return "په ټولو قطارونو کې واحد ضروری دی.";
            if (row.Quantity <= 0) return "په ټولو قطارونو کې مقدار باید له صفر څخه زیات وي.";
            if (row.UnitPrice < 0) return "په ټولو قطارونو کې قیمت ضروری دی.";
            if (row.WarehouseID <= 0) return "په ټولو قطارونو کې ګدام ضروری دی.";

            var expectedRowTotal = row.Quantity * row.UnitPrice;
            if (row.TotalPrice != expectedRowTotal) row.TotalPrice = expectedRowTotal;
            calculatedTotal += row.TotalPrice;
        }

        if (calculatedTotal != request.TotalAmount) request.TotalAmount = calculatedTotal;
        request.RemainingAmount = request.TotalAmount - request.ReceivedAmount;

        return string.Empty;
    }

    private async Task<(decimal mainStockQuantity, int unitId, UnitConversion unitConversion, string error)> ResolveSaleUnitAsync(
        int unitConversionId,
        decimal quantity,
        Item item,
        string userId)
    {
        UnitConversion unitConversion;
        if (unitConversionId == 0)
        {
            unitConversion = await _db.UnitConversion
                .FirstOrDefaultAsync(u => u.ItemID == item.ID && u.SubUnitID == item.UnitId);

            if (unitConversion is null)
            {
                unitConversion = new UnitConversion
                {
                    ItemID = item.ID,
                    MainUnitId = item.UnitId,
                    SubUnitID = item.UnitId,
                    MainAmount = 1,
                    SubAmount = 1,
                    ExchangedAmount = 1,
                    Remarks = string.Empty,
                    CreationDate = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
                _db.UnitConversion.Add(unitConversion);
            }
        }
        else
        {
            if (unitConversionId < 0)
                return (0, 0, null, "په ټولو قطارونو کې واحد ضروری دی.");

            unitConversion = await _db.UnitConversion
                .FirstOrDefaultAsync(u => u.ID == unitConversionId && u.ItemID == item.ID);
        }

        if (unitConversion is null)
            return (0, 0, null, "د دې جنس لپاره واحد ناسم دی.");

        var exchangedAmount = unitConversion.ExchangedAmount;
        if (exchangedAmount <= 0 && unitConversion.MainAmount > 0 && unitConversion.SubAmount > 0)
        {
            exchangedAmount = unitConversion.SubAmount / unitConversion.MainAmount;
            if (exchangedAmount > 0)
                unitConversion.ExchangedAmount = exchangedAmount;
        }

        if (exchangedAmount <= 0)
            return (0, 0, null, "د واحد تبادله ناسمه ده.");

        return (quantity / exchangedAmount, unitConversion.SubUnitID, unitConversion, string.Empty);
    }

    private sealed class PreparedSaleDetail
    {
        public required SaleSaveDetailRequest Detail { get; init; }
        public required decimal MainStockQuantity { get; init; }
        public required int UnitId { get; init; }
        public required UnitConversion UnitConversion { get; init; }
        public required string Remarks { get; init; }
    }

    private sealed class ExistingSaleStockEffect
    {
        public required StockBalance StockBalance { get; init; }
        public required StockTransactions StockTransaction { get; init; }
        public required decimal MainStockQuantity { get; init; }
    }
}


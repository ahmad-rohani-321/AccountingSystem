using AccountingSystem.Data;
using AccountingSystem.Models.Accounting;
using AccountingSystem.Models.Accounts;
using AccountingSystem.Models.Inventory;
using AccountingSystem.Models.Purchase;
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
public class PurchasesController(ApplicationDbContext db) : ApiControllerBase
{
    private readonly ApplicationDbContext _db = db;
    private const int PurchaseTransactionTypeId = 6;
    private const int PurchaseChangeTransactionTypeId = 7;
    private const int PurchaseStockTransactionTypeId = 5;
    private const int PurchaseRefundTransactionTypeId = 10;
    private const int PurchaseRefundStockTransactionTypeId = 6;
    private const int StrictFullPaymentAccountTypeId = 10;
    private const string DefaultChequePhotoPath = "/images/journalentry/default.png";

    private IQueryable<Purchase> BuildPurchaseGridQuery()
    {
        return _db.Purchases
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.Currency);
    }

    private static IQueryable<Purchase> ApplyCreatedTodayFilter(IQueryable<Purchase> query)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return query.Where(x => x.CreationDate >= today && x.CreationDate < tomorrow);
    }

    private IQueryable<PurchaseGridRow> ProjectPurchaseGridRows(IQueryable<Purchase> query)
    {
        return query
            .OrderByDescending(x => x.CreationDate)
            .Select(x => new PurchaseGridRow
            {
                ID = x.ID,
                PurchaseNo = x.PurchaseNo,
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
                ItemCount = _db.PurchaseDetails.Count(d => d.PurchaseID == x.ID),
                Remarks = x.Remarks,
                CreationDate = x.CreationDate
            });
    }

    private static string BuildPurchaseJournalRemarks(int purchaseNo, string remarks)
    {
        return ("خرید نمبر " + purchaseNo + (remarks ?? string.Empty)).Trim();
    }

    private static string BuildPurchaseRefundJournalRemarks(int purchaseNo, string remarks)
    {
        return ("د خرید واپسي نمبر " + purchaseNo + (remarks ?? string.Empty)).Trim();
    }

    private static string BuildPurchaseReceiveJournalRemarks(int purchaseNo, string remarks)
    {
        return ("د خرید رسید نمبر " + purchaseNo + " " + (remarks ?? string.Empty)).Trim();
    }

    private async Task<int?> ResolveTreasureAccountIdAsync(Purchase purchase)
    {
        if (purchase.ReceivedAmount <= 0)
            return null;

        var purchaseJournalRemarks = BuildPurchaseJournalRemarks(purchase.PurchaseNo, purchase.Remarks);
        var receiveJournalRemarks = BuildPurchaseReceiveJournalRemarks(purchase.PurchaseNo, purchase.Remarks);

        return await _db.JournalEntries
            .AsNoTracking()
            .Where(x =>
                (x.TransactionTypeID == PurchaseTransactionTypeId ||
                 x.TransactionTypeID == PurchaseChangeTransactionTypeId) &&
                (x.Remarks == purchaseJournalRemarks || x.Remarks == receiveJournalRemarks) &&
                x.Debit > 0 &&
                x.Credit == 0 &&
                x.AccountBalance.AccountID != purchase.AccountID &&
                x.AccountBalance.CurrencyID == purchase.CurrencyID)
            .OrderByDescending(x => x.ID)
            .Select(x => (int?)x.AccountBalance.AccountID)
            .FirstOrDefaultAsync();
    }

    private async Task<List<PurchaseDetailResponse>> BuildPurchaseDetailResponsesAsync(Purchase purchase)
    {
        var details = await _db.PurchaseDetails
            .AsNoTracking()
            .Where(x => x.PurchaseID == purchase.ID)
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
                x.TransactionID == PurchaseStockTransactionTypeId &&
                x.CreatedByUserId == purchase.CreatedByUserId &&
                x.CreationDate >= purchase.CreationDate.AddMinutes(-10) &&
                x.CreationDate <= purchase.CreationDate.AddMinutes(10) &&
                x.StockBalance != null &&
                itemIds.Contains(x.StockBalance.ItemID))
            .OrderBy(x => x.ID)
            .ToListAsync();

        var usedStockTransactionIds = new HashSet<int>();
        var responses = new List<PurchaseDetailResponse>(details.Count);

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

            responses.Add(new PurchaseDetailResponse
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
        var query = BuildPurchaseGridQuery();
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

        return await ProjectPurchaseGridRows(query).ToListAsync();
    }

    [HttpGet("GetCreatedToday")]
    public async Task<object> GetCreatedToday()
    {
        var query = ApplyCreatedTodayFilter(BuildPurchaseGridQuery());
        return await ProjectPurchaseGridRows(query).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var purchase = await _db.Purchases
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id);

        if (purchase is null)
            return NotFound(new { Message = "خرید ونه موندل شو." });

        var accountInfo = await _db.Accounts
            .AsNoTracking()
            .Where(x => x.ID == purchase.AccountID)
            .Select(x => new { x.Name, x.Code })
            .FirstOrDefaultAsync();

        var currencyInfo = await _db.Currencies
            .AsNoTracking()
            .Where(x => x.ID == purchase.CurrencyID)
            .Select(x => x.CurrencyName)
            .FirstOrDefaultAsync();

        var details = await BuildPurchaseDetailResponsesAsync(purchase);
        var treasureAccountId = await ResolveTreasureAccountIdAsync(purchase);

        return Ok(new PurchaseResponse
        {
            PurchaseID = purchase.ID,
            PurchaseNo = purchase.PurchaseNo,
            AccountID = purchase.AccountID,
            AccountName = accountInfo is null
                ? string.Empty
                : string.IsNullOrWhiteSpace(accountInfo.Code)
                    ? accountInfo.Name ?? string.Empty
                    : (accountInfo.Name ?? string.Empty) + " - " + accountInfo.Code,
            TreasureAccountID = treasureAccountId,
            IsHolded = purchase.IsHolded,
            CurrencyID = purchase.CurrencyID,
            CurrencyName = currencyInfo ?? string.Empty,
            PurchaseDate = purchase.CreationDate,
            TotalAmount = purchase.TotalAmount,
            ReceivedAmount = purchase.ReceivedAmount,
            RemainingAmount = purchase.RemainingAmount,
            Remarks = purchase.Remarks ?? string.Empty,
            Details = details
        });
    }

    [HttpGet("next-no")]
    public async Task<IActionResult> GetNextNo()
    {
        var lastPurchaseNo = await _db.Purchases
            .Select(p => (int?)p.PurchaseNo)
            .MaxAsync() ?? 0;

        return Ok(new { PurchaseNo = lastPurchaseNo + 1 });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] PurchaseSaveRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Message = "سیسټم ته ننوزئ." });

        var validationMessage = ValidateRequest(request, requireTreasureRules: !request.IsHolded);
        if (!string.IsNullOrWhiteSpace(validationMessage))
            return BadRequest(new { Message = validationMessage });

        var configurationMessage = await ValidatePurchaseConfigurationAsync();
        if (!string.IsNullOrWhiteSpace(configurationMessage))
            return BadRequest(new { Message = configurationMessage });

        if (await _db.Purchases.AnyAsync(p => p.PurchaseNo == request.PurchaseNo))
            return BadRequest(new { Message = "د خرید شمېره مخکې ثبت سوې ده." });

        var referenceValidation = await ValidatePurchaseReferencesAsync(request);
        if (!referenceValidation.Success)
            return BadRequest(new { Message = referenceValidation.ErrorMessage });

        PurchaseOrder selectedOrder = null;
        if (request.OrderID is > 0)
        {
            selectedOrder = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.ID == request.OrderID.Value);
            if (selectedOrder is null)
                return BadRequest(new { Message = "خرید آرډر ونه موندل شو." });
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        var preparedDetailsResult = await PreparePurchaseDetailsAsync(request, userId);
        if (!preparedDetailsResult.Success)
            return BadRequest(new { Message = preparedDetailsResult.ErrorMessage });

        var purchase = new Purchase
        {
            PurchaseNo = request.PurchaseNo,
            AccountID = request.AccountID,
            CurrencyID = request.CurrencyID,
            IsHolded = request.IsHolded,
            Remarks = request.Remarks?.Trim() ?? string.Empty,
            TotalAmount = request.TotalAmount,
            ReceivedAmount = request.ReceivedAmount,
            RemainingAmount = request.RemainingAmount,
            CreatedByUserId = userId,
            CreationDate = request.PurchaseDate
        };

        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync();

        var applyResult = await ApplyPurchaseEffectsAsync(
            purchase,
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
            Message = "خرید په بریالیتوب ثبت شو.",
            PurchaseID = purchase.ID,
            purchase.PurchaseNo,
            AccountBalance = applyResult.AccountBalance,
            TreasureAccountBalance = applyResult.TreasureAccountBalance
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PurchaseSaveRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Message = "سیسټم ته ننوزئ." });

        var validationMessage = ValidateRequest(request, requireTreasureRules: !request.IsHolded);
        if (!string.IsNullOrWhiteSpace(validationMessage))
            return BadRequest(new { Message = validationMessage });

        var configurationMessage = await ValidatePurchaseConfigurationAsync();
        if (!string.IsNullOrWhiteSpace(configurationMessage))
            return BadRequest(new { Message = configurationMessage });

        var purchase = await _db.Purchases.FirstOrDefaultAsync(x => x.ID == id);
        if (purchase is null)
            return NotFound(new { Message = "خرید ونه موندل شو." });

        if (!purchase.IsHolded)
            return BadRequest(new { Message = "دا خرید هولډ سوی نه دی." });

        if (purchase.IsRefunded)
            return BadRequest(new { Message = "واپسي سوی خرید سمېدای نه سي." });

        if (await _db.Purchases.AnyAsync(p => p.ID != id && p.PurchaseNo == request.PurchaseNo))
            return BadRequest(new { Message = "د خرید شمېره مخکې ثبت سوې ده." });

        var referenceValidation = await ValidatePurchaseReferencesAsync(request);
        if (!referenceValidation.Success)
            return BadRequest(new { Message = referenceValidation.ErrorMessage });

        if (!request.IsHolded && request.ReceivedAmount > 0 && request.TreasureAccountID is not > 0)
            return BadRequest(new { Message = "که رسید له صفر څخه زیات وي، تجرۍ/بانک حساب لا هم ضروري دی." });

        var purchaseDetails = await _db.PurchaseDetails
            .Where(x => x.PurchaseID == purchase.ID)
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (purchaseDetails.Count == 0)
            return BadRequest(new { Message = "د خرید تفصیلات ونه موندل شول." });

        var existingEffectsResult = await LoadExistingPurchaseEffectsAsync(
            purchase,
            purchaseDetails,
            includeJournalEntries: !purchase.IsHolded);
        if (!existingEffectsResult.Success)
            return BadRequest(new { Message = existingEffectsResult.ErrorMessage });

        await using var tx = await _db.Database.BeginTransactionAsync();
        var preparedDetailsResult = await PreparePurchaseDetailsAsync(request, userId);
        if (!preparedDetailsResult.Success)
            return BadRequest(new { Message = preparedDetailsResult.ErrorMessage });

        var reverseResult = await ReversePurchaseEffectsAsync(
            existingEffectsResult.StockEffects!,
            existingEffectsResult.JournalEntries!,
            reverseFinancialEffects: !purchase.IsHolded,
            userId: userId,
            effectiveDate: request.PurchaseDate);

        if (!reverseResult.Success)
        {
            await tx.RollbackAsync();
            return BadRequest(new { Message = reverseResult.ErrorMessage });
        }

        _db.PurchaseDetails.RemoveRange(purchaseDetails);
        if (existingEffectsResult.JournalEntries!.Count > 0)
            _db.JournalEntries.RemoveRange(existingEffectsResult.JournalEntries!);

        purchase.PurchaseNo = request.PurchaseNo;
        purchase.AccountID = request.AccountID;
        purchase.CurrencyID = request.CurrencyID;
        purchase.IsHolded = request.IsHolded;
        purchase.Remarks = request.Remarks?.Trim() ?? string.Empty;
        purchase.TotalAmount = request.TotalAmount;
        purchase.ReceivedAmount = request.ReceivedAmount;
        purchase.RemainingAmount = request.RemainingAmount;
        purchase.CreationDate = request.PurchaseDate;

        var applyResult = await ApplyPurchaseEffectsAsync(
            purchase,
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
            Message = "خرید په بریالیتوب سم شو.",
            PurchaseID = purchase.ID,
            purchase.PurchaseNo,
            AccountBalance = applyResult.AccountBalance,
            TreasureAccountBalance = applyResult.TreasureAccountBalance
        });
    }

    [HttpPost("{id:int}/refund")]
    public async Task<IActionResult> Refund(int id, [FromBody] PurchaseRefundRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Message = "مهرباني وکړئ بیا سیسټم ته ننوزئ." });

        if (request is null)
            return BadRequest(new { Message = "د واپسي غوښتنه ضروري ده." });

        if (request.RefundAmount < 0)
            return BadRequest(new { Message = "د واپسي مبلغ منفي کېدای نه شي." });

        if (request.RefundAmount > 0 && request.TreasureAccountID is not > 0)
            return BadRequest(new { Message = "کله چې د واپسي مبلغ له صفر څخه زیات وي، تجرۍ/بانک حساب ضروري دی." });

        if (request.RefundAmount <= 0 && request.TreasureAccountID is > 0)
            return BadRequest(new { Message = "کله چې تجرۍ/بانک حساب انتخاب وي، د واپسي مبلغ باید له صفر څخه زیات وي." });

        var configurationMessage = await ValidatePurchaseRefundConfigurationAsync();
        if (!string.IsNullOrWhiteSpace(configurationMessage))
            return BadRequest(new { Message = configurationMessage });

        var purchase = await _db.Purchases.FirstOrDefaultAsync(x => x.ID == id);
        if (purchase is null)
            return NotFound(new { Message = "خرید ونه موندل شو." });

        if (purchase.IsRefunded)
            return BadRequest(new { Message = "د دې خرید واپسي مخکې لا سوې ده." });

        if (purchase.IsHolded && request.RefundAmount > 0)
            return BadRequest(new { Message = "د هولډ شوي خرید لپاره د واپسي نغدي قیدونه نه شي ثبتېدای." });

        if (!purchase.IsHolded && request.RefundAmount > purchase.ReceivedAmount)
            return BadRequest(new { Message = "د واپسي مبلغ د خرید له رسید شوې اندازې څخه زیات کېدای نه شي." });

        if (request.TreasureAccountID is > 0 &&
            !await _db.Accounts.AnyAsync(a => a.ID == request.TreasureAccountID.Value))
            return BadRequest(new { Message = "تجرۍ/بانک حساب ونه موندل شو." });

        var purchaseDetails = await _db.PurchaseDetails
            .Where(x => x.PurchaseID == purchase.ID)
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (purchaseDetails.Count == 0)
            return BadRequest(new { Message = "د خرید تفصیلات ونه موندل شول." });

        var existingEffectsResult = await LoadExistingPurchaseEffectsAsync(
            purchase,
            purchaseDetails,
            includeJournalEntries: false);

        if (!existingEffectsResult.Success)
            return BadRequest(new { Message = existingEffectsResult.ErrorMessage });

        await using var tx = await _db.Database.BeginTransactionAsync();

        foreach (var stockEffect in existingEffectsResult.StockEffects!)
        {
            if (stockEffect.StockBalance.Quantity < stockEffect.MainStockQuantity)
            {
                await tx.RollbackAsync();
                return BadRequest(new { Message = "واپسي نه شي بشپړېدای، ځکه د یو یا څو جنسونو لپاره کافي موجودي نشته." });
            }

            stockEffect.StockBalance.Quantity -= stockEffect.MainStockQuantity;

            _db.StockTransactions.Add(new StockTransactions
            {
                StockBalanceID = stockEffect.StockBalance.ID,
                Quantity = stockEffect.StockTransaction.Quantity,
                Remarks = stockEffect.StockTransaction.Remarks ?? string.Empty,
                UnitID = stockEffect.StockTransaction.UnitID,
                TransactionID = PurchaseRefundStockTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = DateTime.Now
            });
        }

        decimal? accountBalanceValue = null;
        decimal? treasureBalanceValue = null;

        if (!purchase.IsHolded)
        {
            var refundDate = DateTime.Now;
            var accountBalance = await GetOrCreateAccountBalanceAsync(purchase.AccountID, purchase.CurrencyID, userId, refundDate);
            var refundRemarks = BuildPurchaseRefundJournalRemarks(purchase.PurchaseNo, purchase.Remarks);

            accountBalance.Balance += purchase.TotalAmount;
            _db.JournalEntries.Add(new JournalEntry
            {
                AccountBalanceID = accountBalance.ID,
                Debit = 0,
                Credit = purchase.TotalAmount,
                Balance = accountBalance.Balance,
                Remarks = refundRemarks,
                ChequePhoto = DefaultChequePhotoPath,
                TransactionTypeID = PurchaseRefundTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = refundDate
            });

            if (request.RefundAmount > 0)
            {
                accountBalance.Balance -= request.RefundAmount;
                _db.JournalEntries.Add(new JournalEntry
                {
                    AccountBalanceID = accountBalance.ID,
                    Debit = request.RefundAmount,
                    Credit = 0,
                    Balance = accountBalance.Balance,
                    Remarks = refundRemarks,
                    ChequePhoto = DefaultChequePhotoPath,
                    TransactionTypeID = PurchaseRefundTransactionTypeId,
                    CreatedByUserId = userId,
                    CreationDate = refundDate
                });

                var treasureBalance = await GetOrCreateAccountBalanceAsync(
                    request.TreasureAccountID!.Value,
                    purchase.CurrencyID,
                    userId,
                    refundDate);

                treasureBalance.Balance += request.RefundAmount;
                _db.JournalEntries.Add(new JournalEntry
                {
                    AccountBalanceID = treasureBalance.ID,
                    Debit = 0,
                    Credit = request.RefundAmount,
                    Balance = treasureBalance.Balance,
                    Remarks = refundRemarks,
                    ChequePhoto = DefaultChequePhotoPath,
                    TransactionTypeID = PurchaseRefundTransactionTypeId,
                    CreatedByUserId = userId,
                    CreationDate = refundDate
                });

                treasureBalanceValue = treasureBalance.Balance;
            }

            accountBalanceValue = accountBalance.Balance;
        }

        purchase.IsRefunded = true;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            Message = "خرید واپس کړل سو.",
            PurchaseID = purchase.ID,
            purchase.PurchaseNo,
            AccountBalance = accountBalanceValue,
            TreasureAccountBalance = treasureBalanceValue
        });
    }

    [HttpPost("{id:int}/receive")]
    public async Task<IActionResult> Receive(int id, [FromBody] PurchaseReceiveRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Message = "مهرباني وکړئ بیا سیسټم ته ننوزئ." });

        if (request is null)
            return BadRequest(new { Message = "د رسید غوښتنه ضروري ده." });

        if (request.ReceiveAmount <= 0)
            return BadRequest(new { Message = "د رسید مبلغ باید له صفر څخه زیات وي." });

        if (request.TreasureAccountID is not > 0)
            return BadRequest(new { Message = "تجرۍ/بانک حساب ضروري دی." });

        var purchase = await _db.Purchases.FirstOrDefaultAsync(x => x.ID == id);
        if (purchase is null)
            return NotFound(new { Message = "خرید ونه موندل شو." });

        if (purchase.IsRefunded)
            return BadRequest(new { Message = "د واپس شوي خرید لپاره رسید نه شي ثبتېدای." });

        if (purchase.IsHolded)
            return BadRequest(new { Message = "د هولډ شوي خرید لپاره رسید نه شي ثبتېدای." });

        if (purchase.ReceivedAmount >= purchase.TotalAmount && purchase.TotalAmount > 0)
            return BadRequest(new { Message = "د دې خرید ټول مبلغ لا مخکې بشپړ رسید شوی دی." });

        if (purchase.RemainingAmount <= 0)
            return BadRequest(new { Message = "د دې خرید باقي مبلغ صفر دی." });

        if (request.ReceiveAmount > purchase.RemainingAmount)
            return BadRequest(new { Message = "د رسید مبلغ د خرید له باقي اندازې څخه زیات کېدای نه شي." });

        if (!await _db.Accounts.AnyAsync(a => a.ID == request.TreasureAccountID.Value))
            return BadRequest(new { Message = "تجرۍ/بانک حساب ونه موندل شو." });

        var configurationMessage = await ValidatePurchaseConfigurationAsync();
        if (!string.IsNullOrWhiteSpace(configurationMessage))
            return BadRequest(new { Message = configurationMessage });

        var effectiveDate = DateTime.Now;
        var accountBalance = await GetOrCreateAccountBalanceAsync(purchase.AccountID, purchase.CurrencyID, userId, effectiveDate);
        var treasureBalance = await GetOrCreateAccountBalanceAsync(
            request.TreasureAccountID.Value,
            purchase.CurrencyID,
            userId,
            effectiveDate);

        if (request.ReceiveAmount > treasureBalance.Balance)
            return BadRequest(new { Message = "رسید مبلغ په دکان/بانک کې نه دی." });

        var journalRemarks = BuildPurchaseReceiveJournalRemarks(purchase.PurchaseNo, purchase.Remarks);
        await using var tx = await _db.Database.BeginTransactionAsync();

        accountBalance.Balance += request.ReceiveAmount;
        treasureBalance.Balance -= request.ReceiveAmount;

        _db.JournalEntries.Add(new JournalEntry
        {
            AccountBalanceID = accountBalance.ID,
            Debit = 0,
            Credit = request.ReceiveAmount,
            Balance = accountBalance.Balance,
            Remarks = journalRemarks,
            ChequePhoto = DefaultChequePhotoPath,
            TransactionTypeID = PurchaseChangeTransactionTypeId,
            CreatedByUserId = userId,
            CreationDate = effectiveDate
        });

        _db.JournalEntries.Add(new JournalEntry
        {
            AccountBalanceID = treasureBalance.ID,
            Debit = request.ReceiveAmount,
            Credit = 0,
            Balance = treasureBalance.Balance,
            Remarks = journalRemarks,
            ChequePhoto = DefaultChequePhotoPath,
            TransactionTypeID = PurchaseChangeTransactionTypeId,
            CreatedByUserId = userId,
            CreationDate = effectiveDate
        });

        purchase.ReceivedAmount += request.ReceiveAmount;
        purchase.RemainingAmount -= request.ReceiveAmount;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            Message = "د خرید رسید ثبت شو.",
            PurchaseID = purchase.ID,
            purchase.PurchaseNo,
            purchase.ReceivedAmount,
            purchase.RemainingAmount,
            AccountBalance = accountBalance.Balance,
            TreasureAccountBalance = treasureBalance.Balance
        });
    }

    private async Task<string> ValidatePurchaseConfigurationAsync()
    {
        if (!await _db.JournalEntryTransactionTypes.AnyAsync(x => x.ID == PurchaseTransactionTypeId))
            return $"اړین د ورځني معاملاتو ډول {PurchaseTransactionTypeId} ونه موندل سو.";

        if (!await _db.JournalEntryTransactionTypes.AnyAsync(x => x.ID == PurchaseChangeTransactionTypeId))
            return $"اړین د ورځني معاملاتو ډول {PurchaseChangeTransactionTypeId} ونه موندل سو.";

        if (!await _db.StockTransactionTypes.AnyAsync(x => x.ID == PurchaseStockTransactionTypeId))
            return $"اړین د سټاک معاملې ډول {PurchaseStockTransactionTypeId} ونه موندل سو.";

        return string.Empty;
    }

    private async Task<string> ValidatePurchaseRefundConfigurationAsync()
    {
        if (!await _db.JournalEntryTransactionTypes.AnyAsync(x => x.ID == PurchaseRefundTransactionTypeId))
            return $"اړین د ورځني معاملاتو ډول {PurchaseRefundTransactionTypeId} ونه موندل سو.";

        if (!await _db.StockTransactionTypes.AnyAsync(x => x.ID == PurchaseRefundStockTransactionTypeId))
            return $"اړین د سټاک معاملې ډول {PurchaseRefundStockTransactionTypeId} ونه موندل سو.";

        return string.Empty;
    }

    private async Task<(bool Success, string ErrorMessage, Account Account)> ValidatePurchaseReferencesAsync(PurchaseSaveRequest request)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .Include(a => a.AccountType)
            .FirstOrDefaultAsync(a => a.ID == request.AccountID);
        if (account is null)
            return (false, "حساب ونه موندل سو.", null);

        if (!await _db.Currencies.AnyAsync(c => c.ID == request.CurrencyID))
            return (false, "اسعار ونه موندل سول.", null);

        if (request.TreasureAccountID is > 0 &&
            !await _db.Accounts.AnyAsync(a => a.ID == request.TreasureAccountID.Value))
            return (false, "تجرۍ/بانک ونه موندل سو.", null);

        if (!request.IsHolded &&
            account.AccountTypeID == StrictFullPaymentAccountTypeId &&
            request.ReceivedAmount != request.TotalAmount)
            return (false, "د حساب ډول 10 لپاره رسید مبلغ باید له مجموعې سره مساوي وي.", null);

        return (true, string.Empty, account);
    }

    private async Task<(bool Success, string ErrorMessage, List<PreparedPurchaseDetail> PreparedDetails)> PreparePurchaseDetailsAsync(PurchaseSaveRequest request, string userId)
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

        var preparedDetails = new List<PreparedPurchaseDetail>(request.Details.Count);
        foreach (var detail in request.Details)
        {
            if (!itemsById.TryGetValue(detail.ItemID, out var item))
                return (false, "په جنسونو کې ناسم انتخاب شته.", null);

            var (mainStockQuantity, unitId, unitConversion, unitError) =
                await ResolvePurchaseUnitAsync(detail.UnitConversionID!.Value, detail.Quantity, item, userId);
            if (!string.IsNullOrWhiteSpace(unitError))
                return (false, unitError, null);

            preparedDetails.Add(new PreparedPurchaseDetail
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

    private async Task<(bool Success, string ErrorMessage, List<ExistingPurchaseStockEffect> StockEffects, List<StockTransactions> StockTransactions, List<JournalEntry> JournalEntries)> LoadExistingPurchaseEffectsAsync(
        Purchase purchase,
        List<PurchaseDetails> purchaseDetails,
        bool includeJournalEntries)
    {
        var itemIds = purchaseDetails.Select(x => x.ItemID).Distinct().ToArray();
        var itemsById = await _db.Items
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ID))
            .ToDictionaryAsync(x => x.ID);

        var stockTransactions = await _db.StockTransactions
            .Include(x => x.StockBalance)
            .Where(x =>
                x.TransactionID == PurchaseStockTransactionTypeId &&
                x.CreatedByUserId == purchase.CreatedByUserId &&
                x.CreationDate >= purchase.CreationDate.AddMinutes(-10) &&
                x.CreationDate <= purchase.CreationDate.AddMinutes(10) &&
                x.StockBalance != null &&
                itemIds.Contains(x.StockBalance.ItemID))
            .OrderBy(x => x.ID)
            .ToListAsync();
        var unitConversionIds = purchaseDetails
            .Where(x => x.UnitConversionID > 0)
            .Select(x => x.UnitConversionID)
            .Distinct()
            .ToArray();
        var unitSubUnitByConversionId = await _db.UnitConversion
            .AsNoTracking()
            .Where(x => unitConversionIds.Contains(x.ID))
            .ToDictionaryAsync(x => x.ID, x => x.SubUnitID);


        var usedStockTransactionIds = new HashSet<int>();
        var stockEffects = new List<ExistingPurchaseStockEffect>(purchaseDetails.Count);

        foreach (var detail in purchaseDetails)
        {
            if (!itemsById.TryGetValue(detail.ItemID, out var item))
                return (false, "په جنسونو کې ناسم انتخاب شته.", null, null, null);

            var expectedUnitId = item.UnitId;
            if (detail.UnitConversionID > 0)
            {
                if (!unitSubUnitByConversionId.TryGetValue(detail.UnitConversionID, out expectedUnitId))
                    return (false, "د زوړ خرید د واحد تبادله ونه موندل شو.", null, null, null);
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
                return (false, "د زوړ خرید د سټاک بدلونونه ونه موندل شو.", null, null, null);

            usedStockTransactionIds.Add(matchedStockTransaction.ID);

            var mainQuantityResult = await ResolveStoredTransactionMainQuantityAsync(item, matchedStockTransaction.Quantity, matchedStockTransaction.UnitID);
            if (!mainQuantityResult.Success)
                return (false, mainQuantityResult.ErrorMessage, null, null, null);

            stockEffects.Add(new ExistingPurchaseStockEffect
            {
                StockBalance = matchedStockTransaction.StockBalance,
                StockTransaction = matchedStockTransaction,
                MainStockQuantity = mainQuantityResult.MainStockQuantity
            });
        }

        var journalEntries = new List<JournalEntry>();
        if (includeJournalEntries)
        {
            var oldJournalRemarks = BuildPurchaseJournalRemarks(purchase.PurchaseNo, purchase.Remarks);
            journalEntries = await _db.JournalEntries
            .Include(x => x.AccountBalance)
            .Where(x =>
                x.TransactionTypeID == PurchaseTransactionTypeId &&
                x.CreatedByUserId == purchase.CreatedByUserId &&
                x.Remarks == oldJournalRemarks &&
                x.CreationDate >= purchase.CreationDate.AddMinutes(-10) &&
                x.CreationDate <= purchase.CreationDate.AddMinutes(10))
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (journalEntries.Count == 0)
            return (false, "د زوړ خرید جرنل قیدونه ونه موندل شو.", null, null, null);

        }

        return (true, string.Empty, stockEffects, stockEffects.Select(x => x.StockTransaction).ToList(), journalEntries);
    }

    private async Task<(bool Success, string ErrorMessage)> ReversePurchaseEffectsAsync(
        List<ExistingPurchaseStockEffect> stockEffects,
        List<JournalEntry> journalEntries,
        bool reverseFinancialEffects,
        string userId,
        DateTime effectiveDate)
    {
        foreach (var stockEffect in stockEffects)
        {
            if (stockEffect.StockBalance.Quantity < stockEffect.MainStockQuantity)
                return (false, "د زوړ خرید سمون نه شي کېدای، ځکه اوسنی سټاک کم دی.");

            stockEffect.StockBalance.Quantity -= stockEffect.MainStockQuantity;

            // Keep stock movement history intact by recording the reversal as its own transaction.
            _db.StockTransactions.Add(new StockTransactions
            {
                StockBalanceID = stockEffect.StockBalance.ID,
                Quantity = stockEffect.StockTransaction.Quantity,
                Remarks = stockEffect.StockTransaction.Remarks ?? string.Empty,
                UnitID = stockEffect.StockTransaction.UnitID,
                TransactionID = PurchaseRefundStockTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = effectiveDate
            });
        }

        if (!reverseFinancialEffects)
            return (true, string.Empty);

        foreach (var journalEntry in journalEntries)
        {
            if (journalEntry.AccountBalance is null)
                return (false, "د زوړ خرید د حساب بلانس قید ونه موندل شو.");

            journalEntry.AccountBalance.Balance += journalEntry.Debit - journalEntry.Credit;
        }

        return (true, string.Empty);
    }

    private async Task<(bool Success, string ErrorMessage, decimal? AccountBalance, decimal? TreasureAccountBalance)> ApplyPurchaseEffectsAsync(
        Purchase purchase,
        PurchaseSaveRequest request,
        string userId,
        Account account,
        List<PreparedPurchaseDetail> preparedDetails,
        bool applyFinancialEffects)
    {
        var effectiveDate = request.PurchaseDate;

        foreach (var preparedDetail in preparedDetails)
        {
            var stockBalance = await _db.StockBalances.FirstOrDefaultAsync(sb =>
                sb.ItemID == preparedDetail.Detail.ItemID &&
                sb.WarehouseID == preparedDetail.Detail.WarehouseID);

            if (stockBalance is null)
            {
                stockBalance = new StockBalance
                {
                    ItemID = preparedDetail.Detail.ItemID,
                    WarehouseID = preparedDetail.Detail.WarehouseID,
                    Remarks = preparedDetail.Remarks,
                    Quantity = preparedDetail.MainStockQuantity,
                    CreatedByUserId = userId,
                    CreationDate = effectiveDate
                };
                _db.StockBalances.Add(stockBalance);
                await _db.SaveChangesAsync();
            }
            else
            {
                stockBalance.Quantity += preparedDetail.MainStockQuantity;
                if (!string.IsNullOrWhiteSpace(preparedDetail.Remarks))
                    stockBalance.Remarks = preparedDetail.Remarks;
            }

            _db.PurchaseDetails.Add(new PurchaseDetails
            {
                PurchaseID = purchase.ID,
                ItemID = preparedDetail.Detail.ItemID,
                UnitConversion = preparedDetail.UnitConversion,
                Quantity = preparedDetail.Detail.Quantity,
                PerPrice = preparedDetail.Detail.UnitPrice,
                TotalPrice = preparedDetail.Detail.TotalPrice,
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
                TransactionID = PurchaseStockTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = effectiveDate
            });
        }

        if (!applyFinancialEffects)
            return (true, string.Empty, null, null);

        var accountBalance = await GetOrCreateAccountBalanceAsync(request.AccountID, request.CurrencyID, userId, effectiveDate);
        var journalRemarks = BuildPurchaseJournalRemarks(request.PurchaseNo, request.Remarks);

        // An active purchase should always write the full debit/credit trail so
        // JournalEntry balances and AccountBalances stay in sync even when the
        // selected account type requires full payment.
        accountBalance.Balance -= request.TotalAmount;

        _db.JournalEntries.Add(new JournalEntry
        {
            AccountBalanceID = accountBalance.ID,
            Debit = request.TotalAmount,
            Credit = 0,
            Balance = accountBalance.Balance,
            Remarks = journalRemarks,
            ChequePhoto = DefaultChequePhotoPath,
            TransactionTypeID = PurchaseTransactionTypeId,
            CreatedByUserId = userId,
            CreationDate = effectiveDate
        });

        AccountBalance treasureAccountBalance = null;
        if (request.ReceivedAmount > 0)
        {
            treasureAccountBalance = await _db.AccountBalances.FirstOrDefaultAsync(ab =>
                ab.AccountID == request.TreasureAccountID!.Value &&
                ab.CurrencyID == request.CurrencyID);

            if (treasureAccountBalance is null || request.ReceivedAmount > treasureAccountBalance.Balance)
                return (false, "رسید مبلغ په دکان/بانک کې نه دی.", null, null);

            accountBalance.Balance += request.ReceivedAmount;

            _db.JournalEntries.Add(new JournalEntry
            {
                AccountBalanceID = accountBalance.ID,
                Debit = 0,
                Credit = request.ReceivedAmount,
                Balance = accountBalance.Balance,
                Remarks = journalRemarks,
                ChequePhoto = DefaultChequePhotoPath,
                TransactionTypeID = PurchaseTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = effectiveDate
            });

            treasureAccountBalance.Balance -= request.ReceivedAmount;
            _db.JournalEntries.Add(new JournalEntry
            {
                AccountBalanceID = treasureAccountBalance.ID,
                Debit = request.ReceivedAmount,
                Credit = 0,
                Balance = treasureAccountBalance.Balance,
                Remarks = journalRemarks,
                ChequePhoto = DefaultChequePhotoPath,
                TransactionTypeID = PurchaseTransactionTypeId,
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
            return (false, "د زوړ خرید د واحد تبادله ونه موندل شو.", 0);

        var exchangedAmount = unitConversion.ExchangedAmount;
        if (exchangedAmount <= 0 && unitConversion.MainAmount > 0 && unitConversion.SubAmount > 0)
            exchangedAmount = unitConversion.SubAmount / unitConversion.MainAmount;

        if (exchangedAmount <= 0)
            return (false, "د زوړ خرید د واحد تبادله ناسمه ده.", 0);

        return (true, string.Empty, quantity / exchangedAmount);
    }

    private static string ValidateRequest(PurchaseSaveRequest request, bool requireTreasureRules)
    {
        if (request is null) return "معلومات ندي رسېدلي.";
        if (request.PurchaseNo <= 0) return "د خرید شمېره ضروري ده.";
        if (request.AccountID <= 0) return "حساب انتخاب کړئ.";
        if (request.CurrencyID <= 0) return "اسعار انتخاب کړئ.";
        if (request.PurchaseDate == default) return "نېټه انتخاب کړئ.";
        if (request.Details is null || request.Details.Count == 0) return "لږ تر لږه یو قطار اضافه کړئ.";
        if (request.TotalAmount <= 0) return "مجموعه باید له صفر څخه زیاته وي.";
        if (request.ReceivedAmount < 0) return "رسید باید منفي نه وي.";
        if (request.ReceivedAmount > request.TotalAmount) return "رسید باید له مجموعې څخه زیات نه وي.";
        if (requireTreasureRules && request.TreasureAccountID is > 0 && request.ReceivedAmount <= 0) return "کله چې تجرۍ/بانک انتخاب وي، رسید باید له صفر څخه زیات وي.";
        if (requireTreasureRules && (request.TreasureAccountID is null or <= 0) && request.ReceivedAmount > 0) return "کله چې رسید داخل وي، تجرۍ/بانک انتخاب کړئ.";

        var calculatedTotal = 0m;
        foreach (var row in request.Details)
        {
            if (row.ItemID <= 0) return "په ټولو قطارونو کې جنس ضروري دی.";
            if (row.UnitConversionID is null || row.UnitConversionID < 0) return "په ټولو قطارونو کې واحد ضروري دی.";
            if (row.Quantity <= 0) return "په ټولو قطارونو کې مقدار باید له صفر څخه زیات وي.";
            if (row.UnitPrice < 0) return "په ټولو قطارونو کې قیمت ضروري دی.";
            if (row.WarehouseID <= 0) return "په ټولو قطارونو کې ګدام ضروري دی.";

            var expectedRowTotal = row.Quantity * row.UnitPrice;
            if (row.TotalPrice != expectedRowTotal) row.TotalPrice = expectedRowTotal;
            calculatedTotal += row.TotalPrice;
        }

        if (calculatedTotal != request.TotalAmount) request.TotalAmount = calculatedTotal;
        request.RemainingAmount = request.TotalAmount - request.ReceivedAmount;

        return string.Empty;
    }

    private async Task<(decimal mainStockQuantity, int unitId, UnitConversion unitConversion, string error)> ResolvePurchaseUnitAsync(
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
                return (0, 0, null, "په ټولو قطارونو کې واحد ضروري دی.");

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

    private sealed class PreparedPurchaseDetail
    {
        public required PurchaseSaveDetailRequest Detail { get; init; }
        public required decimal MainStockQuantity { get; init; }
        public required int UnitId { get; init; }
        public required UnitConversion UnitConversion { get; init; }
        public required string Remarks { get; init; }
    }

    private sealed class ExistingPurchaseStockEffect
    {
        public required StockBalance StockBalance { get; init; }
        public required StockTransactions StockTransaction { get; init; }
        public required decimal MainStockQuantity { get; init; }
    }
}


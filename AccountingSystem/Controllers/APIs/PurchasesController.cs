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
        return ("Ø®Ø±ÛŒØ¯ Ù†Ù…Ø¨Ø± " + purchaseNo + (remarks ?? string.Empty)).Trim();
    }

    private static string BuildPurchaseRefundJournalRemarks(int purchaseNo, string remarks)
    {
        return ("Ø¯ Ø®Ø±ÛŒØ¯ ÙˆØ§Ù¾Ø³ÙŠ Ù†Ù…Ø¨Ø± " + purchaseNo + (remarks ?? string.Empty)).Trim();
    }

    private static string BuildPurchaseReceiveJournalRemarks(int purchaseNo, string remarks)
    {
        return ("Ø¯ Ø®Ø±ÛŒØ¯ Ø±Ø³ÛŒØ¯ Ù†Ù…Ø¨Ø± " + purchaseNo + " " + (remarks ?? string.Empty)).Trim();
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
            return NotFound(new { Message = "Ø®Ø±ÛŒØ¯ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ." });

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
            return Unauthorized(new { Message = "Ø³ÛŒØ³Ù¼Ù… ØªÙ‡ Ù†Ù†ÙˆØ²Ø¦" });

        var validationMessage = ValidateRequest(request, requireTreasureRules: !request.IsHolded);
        if (!string.IsNullOrWhiteSpace(validationMessage))
            return BadRequest(new { Message = validationMessage });

        var configurationMessage = await ValidatePurchaseConfigurationAsync();
        if (!string.IsNullOrWhiteSpace(configurationMessage))
            return BadRequest(new { Message = configurationMessage });

        if (await _db.Purchases.AnyAsync(p => p.PurchaseNo == request.PurchaseNo))
            return BadRequest(new { Message = "Ø¯ Ø®Ø±ÛŒØ¯ Ø´Ù…ÛØ±Ù‡ Ù…Ø®Ú©Û Ø«Ø¨Øª Ø³ÙˆÛ Ø¯Ù‡." });

        var referenceValidation = await ValidatePurchaseReferencesAsync(request);
        if (!referenceValidation.Success)
            return BadRequest(new { Message = referenceValidation.ErrorMessage });

        PurchaseOrder selectedOrder = null;
        if (request.OrderID is > 0)
        {
            selectedOrder = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.ID == request.OrderID.Value);
            if (selectedOrder is null)
                return BadRequest(new { Message = "Ø®Ø±ÛŒØ¯ Ø¢Ø±Ú‰Ø± ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ." });
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
            Message = "Ø®Ø±ÛŒØ¯ Ù¾Ù‡ Ø¨Ø±ÛŒØ§Ù„ÛŒØªÙˆØ¨ Ø«Ø¨Øª Ø´Ùˆ.",
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
            return Unauthorized(new { Message = "Ø³ÛŒØ³Ù¼Ù… ØªÙ‡ Ù†Ù†ÙˆØ²Ø¦." });

        var validationMessage = ValidateRequest(request, requireTreasureRules: !request.IsHolded);
        if (!string.IsNullOrWhiteSpace(validationMessage))
            return BadRequest(new { Message = validationMessage });

        var configurationMessage = await ValidatePurchaseConfigurationAsync();
        if (!string.IsNullOrWhiteSpace(configurationMessage))
            return BadRequest(new { Message = configurationMessage });

        var purchase = await _db.Purchases.FirstOrDefaultAsync(x => x.ID == id);
        if (purchase is null)
            return NotFound(new { Message = "Ø®Ø±ÛŒØ¯ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ." });

        if (!purchase.IsHolded)
            return BadRequest(new { Message = "Ø¯Ø§ Ø®Ø±ÛŒØ¯ Ù‡ÙˆÙ„Ú‰ Ø³ÙˆÛŒ Ù†Ù‡ Ø¯ÛŒ." });

        if (purchase.IsRefunded)
            return BadRequest(new { Message = "ÙˆØ§Ù¾Ø³ÙŠ Ø³ÙˆÛŒ Ø®Ø±ÛŒØ¯ Ø³Ù…ÛØ¯Ø§ÛŒ Ù†Ù‡ Ø³ÙŠ." });

        if (await _db.Purchases.AnyAsync(p => p.ID != id && p.PurchaseNo == request.PurchaseNo))
            return BadRequest(new { Message = "Ø¯ Ø®Ø±ÛŒØ¯ Ø´Ù…ÛØ±Ù‡ Ù…Ø®Ú©Û Ø«Ø¨Øª Ø³ÙˆÛ Ø¯Ù‡." });

        var referenceValidation = await ValidatePurchaseReferencesAsync(request);
        if (!referenceValidation.Success)
            return BadRequest(new { Message = referenceValidation.ErrorMessage });

        if (!request.IsHolded && request.ReceivedAmount > 0 && request.TreasureAccountID is not > 0)
            return BadRequest(new { Message = "Ú©Ù‡ Ø±Ø³ÛŒØ¯ Ù„Ù‡ ØµÙØ± Ú…Ø®Ù‡ Ø²ÛŒØ§Øª ÙˆÙŠØŒ ØªØ¬Ø±ÙŠ/Ø¨Ø§Ù†Ú© Ø­Ø³Ø§Ø¨ Ù„Ø§ Ù‡Ù… Ø¶Ø±ÙˆØ±ÙŠ Ø¯ÛŒ." });

        var purchaseDetails = await _db.PurchaseDetails
            .Where(x => x.PurchaseID == purchase.ID)
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (purchaseDetails.Count == 0)
            return BadRequest(new { Message = "Ø¯ Ø®Ø±ÛŒØ¯ ØªÙØµÛŒÙ„Ø§Øª ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´ÙˆÙ„." });

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
            reverseFinancialEffects: !purchase.IsHolded);

        if (!reverseResult.Success)
        {
            await tx.RollbackAsync();
            return BadRequest(new { Message = reverseResult.ErrorMessage });
        }

        _db.PurchaseDetails.RemoveRange(purchaseDetails);
        _db.StockTransactions.RemoveRange(existingEffectsResult.StockTransactions!);
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
            Message = "Ø®Ø±ÛŒØ¯ Ù¾Ù‡ Ø¨Ø±ÛŒØ§Ù„ÛŒØªÙˆØ¨ Ø³Ù… Ø´Ùˆ.",
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
            return Unauthorized(new { Message = "Ù…Ù‡Ø±Ø¨Ø§Ù†ÙŠ ÙˆÚ©Ú“Ø¦ Ø¨ÛŒØ§ Ø³ÛŒØ³Ù¼Ù… ØªÙ‡ Ù†Ù†ÙˆØ²Ø¦." });

        if (request is null)
            return BadRequest(new { Message = "Ø¯ ÙˆØ§Ù¾Ø³ÙŠ ØºÙˆÚšØªÙ†Ù‡ Ø¶Ø±ÙˆØ±ÙŠ Ø¯Ù‡." });

        if (request.RefundAmount < 0)
            return BadRequest(new { Message = "Ø¯ ÙˆØ§Ù¾Ø³ÙŠ Ù…Ø¨Ù„Øº Ù…Ù†ÙÙŠ Ú©ÛØ¯Ø§ÛŒ Ù†Ù‡ Ø´ÙŠ." });

        if (request.RefundAmount > 0 && request.TreasureAccountID is not > 0)
            return BadRequest(new { Message = "Ú©Ù„Ù‡ Ú†Û Ø¯ ÙˆØ§Ù¾Ø³ÙŠ Ù…Ø¨Ù„Øº Ù„Ù‡ ØµÙØ± Ú…Ø®Ù‡ Ø²ÛŒØ§Øª ÙˆÙŠØŒ ØªØ¬Ø±ÙŠ/Ø¨Ø§Ù†Ú© Ø­Ø³Ø§Ø¨ Ø¶Ø±ÙˆØ±ÙŠ Ø¯ÛŒ." });

        if (request.RefundAmount <= 0 && request.TreasureAccountID is > 0)
            return BadRequest(new { Message = "Ú©Ù„Ù‡ Ú†Û ØªØ¬Ø±ÙŠ/Ø¨Ø§Ù†Ú© Ø­Ø³Ø§Ø¨ Ø§Ù†ØªØ®Ø§Ø¨ ÙˆÙŠØŒ Ø¯ ÙˆØ§Ù¾Ø³ÙŠ Ù…Ø¨Ù„Øº Ø¨Ø§ÛŒØ¯ Ù„Ù‡ ØµÙØ± Ú…Ø®Ù‡ Ø²ÛŒØ§Øª ÙˆÙŠ." });

        var configurationMessage = await ValidatePurchaseRefundConfigurationAsync();
        if (!string.IsNullOrWhiteSpace(configurationMessage))
            return BadRequest(new { Message = configurationMessage });

        var purchase = await _db.Purchases.FirstOrDefaultAsync(x => x.ID == id);
        if (purchase is null)
            return NotFound(new { Message = "Ø®Ø±ÛŒØ¯ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ." });

        if (purchase.IsRefunded)
            return BadRequest(new { Message = "Ø¯ Ø¯Û Ø®Ø±ÛŒØ¯ ÙˆØ§Ù¾Ø³ÙŠ Ù…Ø®Ú©Û Ù„Ø§ Ø´ÙˆÛ Ø¯Ù‡." });

        if (purchase.IsHolded && request.RefundAmount > 0)
            return BadRequest(new { Message = "Ø¯ Ù‡ÙˆÙ„Ú‰ Ø´ÙˆÙŠ Ø®Ø±ÛŒØ¯ Ù„Ù¾Ø§Ø±Ù‡ Ø¯ ÙˆØ§Ù¾Ø³ÙŠ Ù†ØºØ¯ÙŠ Ù‚ÛŒØ¯ÙˆÙ†Ù‡ Ù†Ù‡ Ø´ÙŠ Ø«Ø¨ØªÛØ¯Ø§ÛŒ." });

        if (!purchase.IsHolded && request.RefundAmount > purchase.ReceivedAmount)
            return BadRequest(new { Message = "Ø¯ ÙˆØ§Ù¾Ø³ÙŠ Ù…Ø¨Ù„Øº Ø¯ Ø®Ø±ÛŒØ¯ Ù„Ù‡ Ø±Ø³ÛŒØ¯ Ø´ÙˆÛ Ø§Ù†Ø¯Ø§Ø²Û Ú…Ø®Ù‡ Ø²ÛŒØ§Øª Ú©ÛØ¯Ø§ÛŒ Ù†Ù‡ Ø´ÙŠ." });

        if (request.TreasureAccountID is > 0 &&
            !await _db.Accounts.AnyAsync(a => a.ID == request.TreasureAccountID.Value))
            return BadRequest(new { Message = "ØªØ¬Ø±ÙŠ/Ø¨Ø§Ù†Ú© Ø­Ø³Ø§Ø¨ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ." });

        var purchaseDetails = await _db.PurchaseDetails
            .Where(x => x.PurchaseID == purchase.ID)
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (purchaseDetails.Count == 0)
            return BadRequest(new { Message = "Ø¯ Ø®Ø±ÛŒØ¯ ØªÙØµÛŒÙ„Ø§Øª ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´ÙˆÙ„." });

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
                return BadRequest(new { Message = "ÙˆØ§Ù¾Ø³ÙŠ Ù†Ù‡ Ø´ÙŠ Ø¨Ø´Ù¾Ú“ÛØ¯Ø§ÛŒØŒ ÚÚ©Ù‡ Ø¯ ÛŒÙˆ ÛŒØ§ Ú…Ùˆ Ø¬Ù†Ø³ÙˆÙ†Ùˆ Ù„Ù¾Ø§Ø±Ù‡ Ú©Ø§ÙÙŠ Ù…ÙˆØ¬ÙˆØ¯ÙŠ Ù†Ø´ØªÙ‡." });
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
            Message = "Ø®Ø±ÛŒØ¯ ÙˆØ§Ù¾Ø³ Ú©Ú“Ù„ Ø³Ùˆ.",
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
            return Unauthorized(new { Message = "Ã™â€¦Ã™â€¡Ã˜Â±Ã˜Â¨Ã˜Â§Ã™â€ Ã™Å  Ã™Ë†ÃšÂ©Ãšâ€œÃ˜Â¦ Ã˜Â¨Ã›Å’Ã˜Â§ Ã˜Â³Ã›Å’Ã˜Â³Ã™Â¼Ã™â€¦ Ã˜ÂªÃ™â€¡ Ã™â€ Ã™â€ Ã™Ë†Ã˜Â²Ã˜Â¦." });

        if (request is null)
            return BadRequest(new { Message = "Ã˜Â¯ Ã˜Â±Ã˜Â³Ã›Å’Ã˜Â¯ Ã˜ÂºÃ™Ë†ÃšÅ¡Ã˜ÂªÃ™â€ Ã™â€¡ Ã˜Â¶Ã˜Â±Ã™Ë†Ã˜Â±Ã™Å  Ã˜Â¯Ã™â€¡." });

        if (request.ReceiveAmount <= 0)
            return BadRequest(new { Message = "Ã˜Â¯ Ã˜Â±Ã˜Â³Ã›Å’Ã˜Â¯ Ã™â€¦Ã˜Â¨Ã™â€žÃ˜Âº Ã˜Â¨Ã˜Â§Ã›Å’Ã˜Â¯ Ã™â€žÃ™â€¡ Ã˜ÂµÃ™ÂÃ˜Â± Ãšâ€¦Ã˜Â®Ã™â€¡ Ã˜Â²Ã›Å’Ã˜Â§Ã˜Âª Ã™Ë†Ã™Å ." });

        if (request.TreasureAccountID is not > 0)
            return BadRequest(new { Message = "Ã˜ÂªÃ˜Â¬Ã˜Â±Ã™Å /Ã˜Â¨Ã˜Â§Ã™â€ ÃšÂ© Ã˜Â­Ã˜Â³Ã˜Â§Ã˜Â¨ Ã˜Â¶Ã˜Â±Ã™Ë†Ã˜Â±Ã™Å  Ã˜Â¯Ã›Å’." });

        var purchase = await _db.Purchases.FirstOrDefaultAsync(x => x.ID == id);
        if (purchase is null)
            return NotFound(new { Message = "Ã˜Â®Ã˜Â±Ã›Å’Ã˜Â¯ Ã™Ë†Ã™â€ Ã™â€¡ Ã™â€¦Ã™Ë†Ã™â€ Ã˜Â¯Ã™â€ž Ã˜Â´Ã™Ë†." });

        if (purchase.IsRefunded)
            return BadRequest(new { Message = "Ã˜Â¯ Ã™Ë†Ã˜Â§Ã™Â¾Ã˜Â³ Ã˜Â´Ã™Ë†Ã™Å  Ã˜Â®Ã˜Â±Ã›Å’Ã˜Â¯ Ã™â€žÃ™Â¾Ã˜Â§Ã˜Â±Ã™â€¡ Ã˜Â±Ã˜Â³Ã›Å’Ã˜Â¯ Ã™â€ Ã™â€¡ Ã˜Â´Ã™Å  Ã˜Â«Ã˜Â¨Ã˜ÂªÃ›ÂÃ˜Â¯Ã˜Â§Ã›Å’." });

        if (purchase.IsHolded)
            return BadRequest(new { Message = "Ã˜Â¯ Ã™â€¡Ã™Ë†Ã™â€žÃšâ€° Ã˜Â´Ã™Ë†Ã™Å  Ã˜Â®Ã˜Â±Ã›Å’Ã˜Â¯ Ã™â€žÃ™Â¾Ã˜Â§Ã˜Â±Ã™â€¡ Ã˜Â±Ã˜Â³Ã›Å’Ã˜Â¯ Ã™â€ Ã™â€¡ Ã˜Â´Ã™Å  Ã˜Â«Ã˜Â¨Ã˜ÂªÃ›ÂÃ˜Â¯Ã˜Â§Ã›Å’." });

        if (purchase.ReceivedAmount >= purchase.TotalAmount && purchase.TotalAmount > 0)
            return BadRequest(new { Message = "Ø¯ Ø¯Û Ø®Ø±ÛŒØ¯ Ù¼ÙˆÙ„ Ù…Ø¨Ù„Øº Ù„Ø§ Ù…Ø®Ú©Û Ø¨Ø´Ù¾Ú“ Ø±Ø³ÛŒØ¯ Ø´ÙˆÛŒ Ø¯ÛŒ." });

        if (purchase.RemainingAmount <= 0)
            return BadRequest(new { Message = "Ã˜Â¯ Ã˜Â¯Ã›Â Ã˜Â®Ã˜Â±Ã›Å’Ã˜Â¯ Ã˜Â¨Ã˜Â§Ã™â€šÃ™Å  Ã™â€¦Ã˜Â¨Ã™â€žÃ˜Âº Ã˜ÂµÃ™ÂÃ˜Â± Ã˜Â¯Ã›Å’." });

        if (request.ReceiveAmount > purchase.RemainingAmount)
            return BadRequest(new { Message = "Ã˜Â¯ Ã˜Â±Ã˜Â³Ã›Å’Ã˜Â¯ Ã™â€¦Ã˜Â¨Ã™â€žÃ˜Âº Ã˜Â¯ Ã˜Â®Ã˜Â±Ã›Å’Ã˜Â¯ Ã™â€žÃ™â€¡ Ã˜Â¨Ã˜Â§Ã™â€šÃ™Å  Ã˜Â§Ã™â€ Ã˜Â¯Ã˜Â§Ã˜Â²Ã›Â Ãšâ€¦Ã˜Â®Ã™â€¡ Ã˜Â²Ã›Å’Ã˜Â§Ã˜Âª ÃšÂ©Ã›ÂÃ˜Â¯Ã˜Â§Ã›Å’ Ã™â€ Ã™â€¡ Ã˜Â´Ã™Å ." });

        if (!await _db.Accounts.AnyAsync(a => a.ID == request.TreasureAccountID.Value))
            return BadRequest(new { Message = "Ã˜ÂªÃ˜Â¬Ã˜Â±Ã™Å /Ã˜Â¨Ã˜Â§Ã™â€ ÃšÂ© Ã˜Â­Ã˜Â³Ã˜Â§Ã˜Â¨ Ã™Ë†Ã™â€ Ã™â€¡ Ã™â€¦Ã™Ë†Ã™â€ Ã˜Â¯Ã™â€ž Ã˜Â´Ã™Ë†." });

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
            return BadRequest(new { Message = "Ã˜Â±Ã˜Â³Ã›Å’Ã˜Â¯ Ã™â€¦Ã˜Â¨Ã™â€žÃ˜Âº Ã™Â¾Ã™â€¡ Ã˜Â¯Ã˜Â®Ã™â€ž/Ã˜Â¨Ã˜Â§Ã™â€ ÃšÂ© Ã™â€ Ã™â€¡ Ã˜Â¯Ã›Å’." });

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
            Message = "Ã˜Â¯ Ã˜Â®Ã˜Â±Ã›Å’Ã˜Â¯ Ã˜Â±Ã˜Â³Ã›Å’Ã˜Â¯ Ã˜Â«Ã˜Â¨Ã˜Âª Ã˜Â´Ã™Ë†.",
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
            return $"Required journal transaction type {PurchaseTransactionTypeId} was not found.";

        if (!await _db.JournalEntryTransactionTypes.AnyAsync(x => x.ID == PurchaseChangeTransactionTypeId))
            return $"Required journal transaction type {PurchaseChangeTransactionTypeId} was not found.";

        if (!await _db.StockTransactionTypes.AnyAsync(x => x.ID == PurchaseStockTransactionTypeId))
            return $"Required stock transaction type {PurchaseStockTransactionTypeId} was not found.";

        return string.Empty;
    }

    private async Task<string> ValidatePurchaseRefundConfigurationAsync()
    {
        if (!await _db.JournalEntryTransactionTypes.AnyAsync(x => x.ID == PurchaseRefundTransactionTypeId))
            return $"Required journal transaction type {PurchaseRefundTransactionTypeId} was not found.";

        if (!await _db.StockTransactionTypes.AnyAsync(x => x.ID == PurchaseRefundStockTransactionTypeId))
            return $"Required stock transaction type {PurchaseRefundStockTransactionTypeId} was not found.";

        return string.Empty;
    }

    private async Task<(bool Success, string ErrorMessage, Account Account)> ValidatePurchaseReferencesAsync(PurchaseSaveRequest request)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .Include(a => a.AccountType)
            .FirstOrDefaultAsync(a => a.ID == request.AccountID);
        if (account is null)
            return (false, "Ø­Ø³Ø§Ø¨ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø³Ùˆ.", null);

        if (!await _db.Currencies.AnyAsync(c => c.ID == request.CurrencyID))
            return (false, "Ø§Ø³Ø¹Ø§Ø± ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø³Ùˆ.", null);

        if (request.TreasureAccountID is > 0 &&
            !await _db.Accounts.AnyAsync(a => a.ID == request.TreasureAccountID.Value))
            return (false, "ØªØ¬Ø±Ø¦/Ø¨Ø§Ù†Ú© ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø³Ùˆ.", null);

        if (!request.IsHolded &&
            account.AccountTypeID == StrictFullPaymentAccountTypeId &&
            request.ReceivedAmount != request.TotalAmount)
            return (false, "Ø¯ Ø­Ø³Ø§Ø¨ Ú‰ÙˆÙ„ 10 Ù„Ù¾Ø§Ø±Ù‡ Ø±Ø³ÛŒØ¯ Ù…Ø¨Ù„Øº Ø¨Ø§ÛŒØ¯ Ù„Ù‡ Ù…Ø¬Ù…ÙˆØ¹Û Ø³Ø±Ù‡ Ù…Ø³Ø§ÙˆÙŠ ÙˆÙŠ.", null);

        return (true, string.Empty, account);
    }

    private async Task<(bool Success, string ErrorMessage, List<PreparedPurchaseDetail> PreparedDetails)> PreparePurchaseDetailsAsync(PurchaseSaveRequest request, string userId)
    {
        var itemIds = request.Details.Select(d => d.ItemID).Distinct().ToArray();
        var warehouseIds = request.Details.Select(d => d.WarehouseID).Distinct().ToArray();

        if (await _db.Items.CountAsync(i => itemIds.Contains(i.ID)) != itemIds.Length)
            return (false, "Ù¾Ù‡ Ø¬Ù†Ø³ÙˆÙ†Ùˆ Ú©Û Ù†Ø§Ø³Ù… Ø§Ù†ØªØ®Ø§Ø¨ Ø´ØªÙ‡.", null);

        if (await _db.WareHouses.CountAsync(w => warehouseIds.Contains(w.ID)) != warehouseIds.Length)
            return (false, "Ù¾Ù‡ Ú«Ø¯Ø§Ù…ÙˆÙ†Ùˆ Ú©Û Ù†Ø§Ø³Ù… Ø§Ù†ØªØ®Ø§Ø¨ Ø´ØªÙ‡.", null);

        var itemsById = await _db.Items
            .Where(i => itemIds.Contains(i.ID))
            .ToDictionaryAsync(i => i.ID);

        var preparedDetails = new List<PreparedPurchaseDetail>(request.Details.Count);
        foreach (var detail in request.Details)
        {
            if (!itemsById.TryGetValue(detail.ItemID, out var item))
                return (false, "Ù¾Ù‡ Ø¬Ù†Ø³ÙˆÙ†Ùˆ Ú©Û Ù†Ø§Ø³Ù… Ø§Ù†ØªØ®Ø§Ø¨ Ø´ØªÙ‡.", null);

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
                return (false, "Ù¾Ù‡ Ø¬Ù†Ø³ÙˆÙ†Ùˆ Ú©Û Ù†Ø§Ø³Ù… Ø§Ù†ØªØ®Ø§Ø¨ Ø´ØªÙ‡.", null, null, null);

            var expectedUnitId = item.UnitId;
            if (detail.UnitConversionID > 0)
            {
                if (!unitSubUnitByConversionId.TryGetValue(detail.UnitConversionID, out expectedUnitId))
                    return (false, "Ø¯ Ø²ÙˆÚ“ Ø®Ø±ÛŒØ¯ Ø¯ ÙˆØ§Ø­Ø¯ ØªØ¨Ø§Ø¯Ù„Ù‡ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ.", null, null, null);
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
                return (false, "Ø¯ Ø²ÙˆÚ“ Ø®Ø±ÛŒØ¯ Ø¯ Ø³Ù¼Ø§Ú© Ø¨Ø¯Ù„ÙˆÙ†ÙˆÙ†Ù‡ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ.", null, null, null);

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
            return (false, "Ø¯ Ø²ÙˆÚ“ Ø®Ø±ÛŒØ¯ Ø¬Ø±Ù†Ù„ Ù‚ÛŒØ¯ÙˆÙ†Ù‡ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ.", null, null, null);

        }

        return (true, string.Empty, stockEffects, stockEffects.Select(x => x.StockTransaction).ToList(), journalEntries);
    }

    private async Task<(bool Success, string ErrorMessage)> ReversePurchaseEffectsAsync(
        List<ExistingPurchaseStockEffect> stockEffects,
        List<JournalEntry> journalEntries,
        bool reverseFinancialEffects)
    {
        foreach (var stockEffect in stockEffects)
        {
            if (stockEffect.StockBalance.Quantity < stockEffect.MainStockQuantity)
                return (false, "Ø¯ Ø²ÙˆÚ“ Ø®Ø±ÛŒØ¯ Ø³Ù…ÙˆÙ† Ù†Ù‡ Ø´ÙŠØŒ ÚÚ©Ù‡ Ø§ÙˆØ³Ù†ÛŒ Ø³Ù¼Ø§Ú© Ú©Ù… Ø¯ÛŒ.");

            stockEffect.StockBalance.Quantity -= stockEffect.MainStockQuantity;
        }

        if (!reverseFinancialEffects)
            return (true, string.Empty);

        foreach (var journalEntry in journalEntries)
        {
            if (journalEntry.AccountBalance is null)
                return (false, "Ø¯ Ø²ÙˆÚ“ Ø®Ø±ÛŒØ¯ Ø¯ Ø­Ø³Ø§Ø¨ Ø¨Ù„Ø§Ù†Ø³ Ù‚ÛŒØ¯ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ.");

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
                return (false, "Ø±Ø³ÛŒØ¯ Ù…Ø¨Ù„Øº Ù¾Ù‡ Ø¯Ø®Ù„/Ø¨Ø§Ù†Ú© Ù†Ù‡ Ø¯ÛŒ.", null, null);

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
            return (false, "Ø¯ Ø²ÙˆÚ“ Ø®Ø±ÛŒØ¯ Ø¯ ÙˆØ§Ø­Ø¯ ØªØ¨Ø§Ø¯Ù„Ù‡ ÙˆÙ†Ù‡ Ù…ÙˆÙ†Ø¯Ù„ Ø´Ùˆ.", 0);

        var exchangedAmount = unitConversion.ExchangedAmount;
        if (exchangedAmount <= 0 && unitConversion.MainAmount > 0 && unitConversion.SubAmount > 0)
            exchangedAmount = unitConversion.SubAmount / unitConversion.MainAmount;

        if (exchangedAmount <= 0)
            return (false, "Ø¯ Ø²ÙˆÚ“ Ø®Ø±ÛŒØ¯ Ø¯ ÙˆØ§Ø­Ø¯ ØªØ¨Ø§Ø¯Ù„Ù‡ Ù†Ø§Ø³Ù…Ù‡ Ø¯Ù‡.", 0);

        return (true, string.Empty, quantity / exchangedAmount);
    }

    private static string ValidateRequest(PurchaseSaveRequest request, bool requireTreasureRules)
    {
        if (request is null) return "Ù…Ø¹Ù„ÙˆÙ…Ø§Øª Ù†Ø¯ÙŠ Ø±Ø³ÛØ¯Ù„ÙŠ.";
        if (request.PurchaseNo <= 0) return "Ø¯ Ø®Ø±ÛŒØ¯ Ø´Ù…ÛØ±Ù‡ Ø¶Ø±ÙˆØ±ÛŒ Ø¯Ù‡.";
        if (request.AccountID <= 0) return "Ø­Ø³Ø§Ø¨ Ø§Ù†ØªØ®Ø§Ø¨ Ú©Ú“Ø¦.";
        if (request.CurrencyID <= 0) return "Ø§Ø³Ø¹Ø§Ø± Ø§Ù†ØªØ®Ø§Ø¨ Ú©Ú“Ø¦.";
        if (request.PurchaseDate == default) return "Ù†ÛÙ¼Ù‡ Ø§Ù†ØªØ®Ø§Ø¨ Ú©Ú“Ø¦.";
        if (request.Details is null || request.Details.Count == 0) return "Ù„Ú– ØªØ± Ù„Ú–Ù‡ ÛŒÙˆ Ù‚Ø·Ø§Ø± Ø§Ø¶Ø§ÙÙ‡ Ú©Ú“Ø¦.";
        if (request.TotalAmount <= 0) return "Ù…Ø¬Ù…ÙˆØ¹Ù‡ Ø¨Ø§ÛŒØ¯ Ù„Ù‡ ØµÙØ± Ú…Ø®Ù‡ Ø²ÛŒØ§ØªÙ‡ ÙˆÙŠ.";
        if (request.ReceivedAmount < 0) return "Ø±Ø³ÛŒØ¯ Ø¨Ø§ÛŒØ¯ Ù…Ù†ÙÙŠ Ù†Ù‡ ÙˆÙŠ.";
        if (request.ReceivedAmount > request.TotalAmount) return "Ø±Ø³ÛŒØ¯ Ø¨Ø§ÛŒØ¯ Ù„Ù‡ Ù…Ø¬Ù…ÙˆØ¹Û Ú…Ø®Ù‡ Ø²ÛŒØ§Øª Ù†Ù‡ ÙˆÙŠ.";
        if (requireTreasureRules && request.TreasureAccountID is > 0 && request.ReceivedAmount <= 0) return "Ú©Ù„Ù‡ Ú†Û ØªØ¬Ø±Ø¦/Ø¨Ø§Ù†Ú© Ø§Ù†ØªØ®Ø§Ø¨ ÙˆÙŠØŒ Ø±Ø³ÛŒØ¯ Ø¨Ø§ÛŒØ¯ Ù„Ù‡ ØµÙØ± Ú…Ø®Ù‡ Ø²ÛŒØ§Øª ÙˆÙŠ.";
        if (requireTreasureRules && (request.TreasureAccountID is null or <= 0) && request.ReceivedAmount > 0) return "Ú©Ù„Ù‡ Ú†Û Ø±Ø³ÛŒØ¯ Ø¯Ø§Ø®Ù„ ÙˆÙŠØŒ ØªØ¬Ø±Ø¦/Ø¨Ø§Ù†Ú© Ø§Ù†ØªØ®Ø§Ø¨ Ú©Ú“Ø¦.";

        var calculatedTotal = 0m;
        foreach (var row in request.Details)
        {
            if (row.ItemID <= 0) return "Ù¾Ù‡ Ù¼ÙˆÙ„Ùˆ Ù‚Ø·Ø§Ø±ÙˆÙ†Ùˆ Ú©Û Ø¬Ù†Ø³ Ø¶Ø±ÙˆØ±ÛŒ Ø¯ÛŒ.";
            if (row.UnitConversionID is null || row.UnitConversionID < 0) return "Ù¾Ù‡ Ù¼ÙˆÙ„Ùˆ Ù‚Ø·Ø§Ø±ÙˆÙ†Ùˆ Ú©Û ÙˆØ§Ø­Ø¯ Ø¶Ø±ÙˆØ±ÛŒ Ø¯ÛŒ.";
            if (row.Quantity <= 0) return "Ù¾Ù‡ Ù¼ÙˆÙ„Ùˆ Ù‚Ø·Ø§Ø±ÙˆÙ†Ùˆ Ú©Û Ù…Ù‚Ø¯Ø§Ø± Ø¨Ø§ÛŒØ¯ Ù„Ù‡ ØµÙØ± Ú…Ø®Ù‡ Ø²ÛŒØ§Øª ÙˆÙŠ.";
            if (row.UnitPrice < 0) return "Ù¾Ù‡ Ù¼ÙˆÙ„Ùˆ Ù‚Ø·Ø§Ø±ÙˆÙ†Ùˆ Ú©Û Ù‚ÛŒÙ…Øª Ø¶Ø±ÙˆØ±ÛŒ Ø¯ÛŒ.";
            if (row.WarehouseID <= 0) return "Ù¾Ù‡ Ù¼ÙˆÙ„Ùˆ Ù‚Ø·Ø§Ø±ÙˆÙ†Ùˆ Ú©Û Ú«Ø¯Ø§Ù… Ø¶Ø±ÙˆØ±ÛŒ Ø¯ÛŒ.";

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
                return (0, 0, null, "Ù¾Ù‡ Ù¼ÙˆÙ„Ùˆ Ù‚Ø·Ø§Ø±ÙˆÙ†Ùˆ Ú©Û ÙˆØ§Ø­Ø¯ Ø¶Ø±ÙˆØ±ÛŒ Ø¯ÛŒ.");

            unitConversion = await _db.UnitConversion
                .FirstOrDefaultAsync(u => u.ID == unitConversionId && u.ItemID == item.ID);
        }

        if (unitConversion is null)
            return (0, 0, null, "Ø¯ Ø¯Û Ø¬Ù†Ø³ Ù„Ù¾Ø§Ø±Ù‡ ÙˆØ§Ø­Ø¯ Ù†Ø§Ø³Ù… Ø¯ÛŒ.");

        var exchangedAmount = unitConversion.ExchangedAmount;
        if (exchangedAmount <= 0 && unitConversion.MainAmount > 0 && unitConversion.SubAmount > 0)
        {
            exchangedAmount = unitConversion.SubAmount / unitConversion.MainAmount;
            if (exchangedAmount > 0)
                unitConversion.ExchangedAmount = exchangedAmount;
        }

        if (exchangedAmount <= 0)
            return (0, 0, null, "Ø¯ ÙˆØ§Ø­Ø¯ ØªØ¨Ø§Ø¯Ù„Ù‡ Ù†Ø§Ø³Ù…Ù‡ Ø¯Ù‡.");

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


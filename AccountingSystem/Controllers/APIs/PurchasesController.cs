using AccountingSystem.Data;
using AccountingSystem.Models.Accounting;
using AccountingSystem.Models.Accounts;
using AccountingSystem.Models.Inventory;
using AccountingSystem.Models.Purchase;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PurchasesController(ApplicationDbContext db) : ApiControllerBase
{
    private readonly ApplicationDbContext _db = db;
    private const int PurchaseTransactionTypeId = 6;
    private const int PurchaseStockTransactionTypeId = 5;
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

    private async Task<int?> ResolveTreasureAccountIdAsync(Purchase purchase)
    {
        if (purchase.ReceivedAmount <= 0)
            return null;

        var journalRemarks = BuildPurchaseJournalRemarks(purchase.PurchaseNo, purchase.Remarks);

        return await _db.JournalEntries
            .AsNoTracking()
            .Where(x =>
                x.TransactionTypeID == PurchaseTransactionTypeId &&
                x.Remarks == journalRemarks &&
                x.Debit == purchase.ReceivedAmount &&
                x.Credit == 0 &&
                x.AccountBalance.AccountID != purchase.AccountID &&
                x.AccountBalance.CurrencyID == purchase.CurrencyID)
            .OrderBy(x => x.ID)
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

        var itemIds = details.Select(x => x.ItemID).Distinct().ToArray();

        var unitConversions = await _db.UnitConversion
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemID))
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

            var unitConversionId = matchedStockTransaction is null
                ? null
                : unitConversions
                    .Where(x => x.ItemID == detail.ItemID && x.SubUnitID == matchedStockTransaction.UnitID)
                    .Select(x => (int?)x.ID)
                    .FirstOrDefault();

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

        var details = await BuildPurchaseDetailResponsesAsync(purchase);
        var treasureAccountId = await ResolveTreasureAccountIdAsync(purchase);

        return Ok(new PurchaseResponse
        {
            PurchaseID = purchase.ID,
            PurchaseNo = purchase.PurchaseNo,
            AccountID = purchase.AccountID,
            TreasureAccountID = treasureAccountId,
            CurrencyID = purchase.CurrencyID,
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
            return Unauthorized(new { Message = "سیسټم ته ننوزئ" });

        var validationMessage = ValidateRequest(request);
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
                return BadRequest(new { Message = "Purchase order was not found." });
        }

        var preparedDetailsResult = await PreparePurchaseDetailsAsync(request);
        if (!preparedDetailsResult.Success)
            return BadRequest(new { Message = preparedDetailsResult.ErrorMessage });

        await using var tx = await _db.Database.BeginTransactionAsync();

        var purchase = new Purchase
        {
            PurchaseNo = request.PurchaseNo,
            AccountID = request.AccountID,
            CurrencyID = request.CurrencyID,
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
            preparedDetailsResult.PreparedDetails!);

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

    private async Task<string> ValidatePurchaseConfigurationAsync()
    {
        if (!await _db.JournalEntryTransactionTypes.AnyAsync(x => x.ID == PurchaseTransactionTypeId))
            return $"Required journal transaction type {PurchaseTransactionTypeId} was not found.";

        if (!await _db.StockTransactionTypes.AnyAsync(x => x.ID == PurchaseStockTransactionTypeId))
            return $"Required stock transaction type {PurchaseStockTransactionTypeId} was not found.";

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
            return (false, "اسعار ونه موندل سو.", null);

        if (request.TreasureAccountID is > 0 &&
            !await _db.Accounts.AnyAsync(a => a.ID == request.TreasureAccountID.Value))
            return (false, "تجرئ/بانک ونه موندل سو.", null);

        if (account.AccountTypeID == StrictFullPaymentAccountTypeId && request.ReceivedAmount != request.TotalAmount)
            return (false, "For account type 10, received amount must be equal to total amount.", null);

        return (true, string.Empty, account);
    }

    private async Task<(bool Success, string ErrorMessage, List<PreparedPurchaseDetail> PreparedDetails)> PreparePurchaseDetailsAsync(PurchaseSaveRequest request)
    {
        var itemIds = request.Details.Select(d => d.ItemID).Distinct().ToArray();
        var warehouseIds = request.Details.Select(d => d.WarehouseID).Distinct().ToArray();

        if (await _db.Items.CountAsync(i => itemIds.Contains(i.ID)) != itemIds.Length)
            return (false, "په جنسونو کې ناسم انتخاب شته.", null);

        if (await _db.WareHouses.CountAsync(w => warehouseIds.Contains(w.ID)) != warehouseIds.Length)
            return (false, "په ګدامونو کې ناسم انتخاب شته.", null);

        var itemsById = await _db.Items
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.ID))
            .ToDictionaryAsync(i => i.ID);

        var preparedDetails = new List<PreparedPurchaseDetail>(request.Details.Count);
        foreach (var detail in request.Details)
        {
            if (!itemsById.TryGetValue(detail.ItemID, out var item))
                return (false, "په جنسونو کې ناسم انتخاب شته.", null);

            var (mainStockQuantity, unitId, unitError) = await ResolvePurchaseUnitAsync(detail.UnitConversionID!.Value, detail.Quantity, item);
            if (!string.IsNullOrWhiteSpace(unitError))
                return (false, unitError, null);

            preparedDetails.Add(new PreparedPurchaseDetail
            {
                Detail = detail,
                MainStockQuantity = mainStockQuantity,
                UnitId = unitId,
                Remarks = (detail.Remarks ?? string.Empty).Trim()
            });
        }

        return (true, string.Empty, preparedDetails);
    }

    private async Task<(bool Success, string ErrorMessage, List<ExistingPurchaseStockEffect> StockEffects, List<StockTransactions> StockTransactions, List<JournalEntry> JournalEntries)> LoadExistingPurchaseEffectsAsync(
        Purchase purchase,
        List<PurchaseDetails> purchaseDetails)
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

        var usedStockTransactionIds = new HashSet<int>();
        var stockEffects = new List<ExistingPurchaseStockEffect>(purchaseDetails.Count);

        foreach (var detail in purchaseDetails)
        {
            if (!itemsById.TryGetValue(detail.ItemID, out var item))
                return (false, "په جنسونو کې ناسم انتخاب شته.", null, null, null);

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

        var oldJournalRemarks = BuildPurchaseJournalRemarks(purchase.PurchaseNo, purchase.Remarks);
        var journalEntries = await _db.JournalEntries
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

        return (true, string.Empty, stockEffects, stockEffects.Select(x => x.StockTransaction).ToList(), journalEntries);
    }

    private async Task<(bool Success, string ErrorMessage)> ReversePurchaseEffectsAsync(
        List<ExistingPurchaseStockEffect> stockEffects,
        List<JournalEntry> journalEntries)
    {
        foreach (var stockEffect in stockEffects)
        {
            if (stockEffect.StockBalance.Quantity < stockEffect.MainStockQuantity)
                return (false, "د زوړ خرید سمون نه شي، ځکه اوسنی سټاک کم دی.");

            stockEffect.StockBalance.Quantity -= stockEffect.MainStockQuantity;
        }

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
        List<PreparedPurchaseDetail> preparedDetails)
    {
        var skipsAccountBalanceMovement = account.AccountTypeID == StrictFullPaymentAccountTypeId;
        var effectiveDate = request.PurchaseDate;
        var accountBalance = await GetOrCreateAccountBalanceAsync(request.AccountID, request.CurrencyID, userId, effectiveDate);

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

        var journalRemarks = BuildPurchaseJournalRemarks(request.PurchaseNo, request.Remarks);

        if (!skipsAccountBalanceMovement)
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
                return (false, "رسید مبلغ په دخل/بانک نه دی.", null, null);

            if (!skipsAccountBalanceMovement)
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

    private static string ValidateRequest(PurchaseSaveRequest request)
    {
        if (request is null) return "معلومات ندي رسېدلي.";
        if (request.PurchaseNo <= 0) return "د خرید شمېره ضروری ده.";
        if (request.AccountID <= 0) return "حساب انتخاب کړئ.";
        if (request.CurrencyID <= 0) return "اسعار انتخاب کړئ.";
        if (request.PurchaseDate == default) return "نېټه انتخاب کړئ.";
        if (request.Details is null || request.Details.Count == 0) return "لږ تر لږه یو قطار اضافه کړئ.";
        if (request.TotalAmount <= 0) return "مجموعه باید له صفر څخه زیاته وي.";
        if (request.ReceivedAmount < 0) return "رسید باید منفي نه وي.";
        if (request.ReceivedAmount > request.TotalAmount) return "رسید باید له مجموعې څخه زیات نه وي.";
        if (request.TreasureAccountID is > 0 && request.ReceivedAmount <= 0) return "کله چې تجرئ/بانک انتخاب وي، رسید باید له صفر څخه زیات وي.";
        if ((request.TreasureAccountID is null or <= 0) && request.ReceivedAmount > 0) return "کله چې رسید داخل وي، تجرئ/بانک انتخاب کړئ.";

        var calculatedTotal = 0m;
        foreach (var row in request.Details)
        {
            if (row.ItemID <= 0) return "په ټولو قطارونو کې جنس ضروری دی.";
            if (row.UnitConversionID is null or <= 0) return "په ټولو قطارونو کې واحد ضروری دی.";
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

    private async Task<(decimal mainStockQuantity, int unitId, string error)> ResolvePurchaseUnitAsync(
        int unitConversionId,
        decimal quantity,
        Item item)
    {
        if (unitConversionId <= 0)
            return (0, 0, "په ټولو قطارونو کې واحد ضروری دی.");

        var unitConversion = await _db.UnitConversion
            .FirstOrDefaultAsync(u => u.ID == unitConversionId && u.ItemID == item.ID);

        if (unitConversion is null)
            return (0, 0, "د دې جنس لپاره واحد ناسم دی.");

        var exchangedAmount = unitConversion.ExchangedAmount;
        if (exchangedAmount <= 0 && unitConversion.MainAmount > 0 && unitConversion.SubAmount > 0)
        {
            exchangedAmount = unitConversion.SubAmount / unitConversion.MainAmount;
            if (exchangedAmount > 0)
                unitConversion.ExchangedAmount = exchangedAmount;
        }

        if (exchangedAmount <= 0)
            return (0, 0, "د واحد تبادله ناسمه ده.");

        return (quantity / exchangedAmount, unitConversion.SubUnitID, string.Empty);
    }

    private sealed class PreparedPurchaseDetail
    {
        public required PurchaseSaveDetailRequest Detail { get; init; }
        public required decimal MainStockQuantity { get; init; }
        public required int UnitId { get; init; }
        public required string Remarks { get; init; }
    }

    private sealed class ExistingPurchaseStockEffect
    {
        public required StockBalance StockBalance { get; init; }
        public required StockTransactions StockTransaction { get; init; }
        public required decimal MainStockQuantity { get; init; }
    }
}

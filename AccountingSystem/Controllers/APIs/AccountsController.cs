using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models.Accounts;
using AccountingSystem.Models.Accounting;
using AccountingSystem.ViewModels;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AccountsController(ApplicationDbContext db, IWebHostEnvironment env) : ApiControllerBase
{
    private readonly ApplicationDbContext _db = db;
    private readonly IWebHostEnvironment _env = env;

    private static readonly int[] AccountsAllowedTypes = [1, 2, 6, 7];
    private static readonly int[] ContributorAllowedTypes = [8];
    private static readonly int[] AllAllowedTypes = AccountsAllowedTypes
        .Concat([3, 4, 5, 9, 10])
        .Concat(ContributorAllowedTypes)
        .ToArray();

    private static bool TryParseDecimalValue(string rawValue, out decimal parsedValue)
    {
        rawValue = rawValue?.Trim();

        return decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedValue) ||
               decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedValue);
    }

    [HttpGet]
    public async Task<object> Get(DataSourceLoadOptions loadOptions, string types = null)
    {
        var requestedTypes = ParseRequestedTypes(types) ?? AccountsAllowedTypes;

        List<AccountListItem> data;

        if (requestedTypes.All(typeId => AccountsAllowedTypes.Contains(typeId)))
        {
            data = await _db.Accounts
                .Include(a => a.AccountType)
                .Where(a => requestedTypes.Contains(a.AccountTypeID))
                .OrderByDescending(a => a.CreationDate)
                .AsNoTracking()
                .Select(account => new AccountListItem
                {
                    AccountID = account.ID,
                    Name = account.Name,
                    Code = account.Code,
                    AccountTypeID = account.AccountTypeID,
                    AccountTypeName = account.AccountType != null ? account.AccountType.Name : string.Empty,
                    IsActive = account.IsActive
                })
                .ToListAsync();
        }
        else
        {
            data = await _db.AccountContacts
                .Include(x => x.Account)
                .Include(x => x.Account.AccountType)
                .Where(a => requestedTypes.Contains(a.Account.AccountTypeID))
                .OrderByDescending(a => a.Account.CreationDate)
                .AsNoTracking()
                .Select(account => new AccountListItem
                {
                    AccountID = account.AccountID,
                    Name = account.Account.Name,
                    Code = account.Account.Code,
                    AccountTypeID = account.Account.AccountTypeID,
                    AccountTypeName = account.Account.AccountType != null ? account.Account.AccountType.Name : string.Empty,
                    IsActive = account.Account.IsActive,
                    AccountContactID = account.ID,
                    NIC = account.NIC ?? string.Empty,
                    FirstPhone = account.FirstPhone ?? string.Empty,
                    SecondPhone = account.SecondPhone ?? string.Empty,
                    Email = account.Email ?? string.Empty,
                    Address = account.Address ?? string.Empty
                })
                .ToListAsync();
        }

        return DataSourceLoader.Load(data, loadOptions);
    }

    [HttpGet("next-code")]
    public async Task<IActionResult> NextCode()
    {
        var nextCode = await GenerateNextCode();
        return Ok(new { Code = nextCode });
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance([FromQuery] int accountId, [FromQuery] int currencyId)
    {
        if (accountId <= 0 || currencyId <= 0)
            return BadRequest(new { Message = "Account and currency are required." });

        var balance = await _db.AccountBalances
            .Where(ab => ab.AccountID == accountId && ab.CurrencyID == currencyId)
            .Select(ab => ab.Balance)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            AccountID = accountId,
            CurrencyID = currencyId,
            Balance = balance
        });
    }

    [HttpGet("{accountId:int}/balances")]
    public async Task<IActionResult> GetBalances(int accountId)
    {
        if (accountId <= 0)
            return BadRequest(new { Message = "Account is required." });

        var account = await _db.Accounts
            .AsNoTracking()
            .Where(a => a.ID == accountId)
            .Select(a => new
            {
                AccountID = a.ID,
                a.Name,
                a.Code,
                AccountTypeName = a.AccountType != null ? a.AccountType.Name : string.Empty
            })
            .FirstOrDefaultAsync();

        if (account is null)
            return NotFound(new { Message = "Account was not found." });

        var balances = await _db.Currencies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.ID)
            .Select(c => new
            {
                CurrencyID = c.ID,
                c.CurrencyName,
                Balance = _db.AccountBalances
                    .Where(ab => ab.AccountID == accountId && ab.CurrencyID == c.ID)
                    .Select(ab => (decimal?)ab.Balance)
                    .FirstOrDefault() ?? 0
            })
            .ToListAsync();

        return Ok(new
        {
            Account = account,
            Balances = balances
        });
    }

    [HttpPost]
    public Task<IActionResult> Post([FromBody] AccountCreateRequest request) => SaveAccountAsync(request);

    [HttpPost("create")]
    public Task<IActionResult> Create([FromBody] AccountCreateRequest request) => SaveAccountAsync(request);

    [HttpPost("journal-entry")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> CreateJournalEntry([FromForm] JournalEntryCreateRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        request.Remarks = request.Remarks?.Trim() ?? string.Empty;

        if (!TryParseDecimalValue(request.Amount, out var amount))
            return BadRequest(new { Message = "Amount is not valid." });

        if (!TryParseDecimalValue(request.ExchangeRate, out var requestExchangeRate))
            requestExchangeRate = 0;

        if (request.DebitCurrencyId <= 0 || request.CreditCurrencyId <= 0)
            return BadRequest(new { Message = "Debit and credit currencies are required." });

        if (request.DebitAccountId <= 0 || request.CreditAccountId <= 0)
            return BadRequest(new { Message = "Debit and credit accounts are required." });

        if (amount <= 0)
            return BadRequest(new { Message = "Amount must be greater than zero." });

        if (string.IsNullOrWhiteSpace(request.Remarks))
            return BadRequest(new { Message = "Remarks are required." });

        if (request.ChequePhoto is not null &&
            !string.Equals(Path.GetExtension(request.ChequePhoto.FileName), ".jpg", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { Message = "Only .jpg files are allowed." });
        }

        var exchangeRate = request.DebitCurrencyId == request.CreditCurrencyId ? 1m : requestExchangeRate;
        if (exchangeRate <= 0)
            return BadRequest(new { Message = "A valid exchange rate is required." });

        var creditAmount = request.DebitCurrencyId == request.CreditCurrencyId
            ? amount
            : amount / exchangeRate;

        var selectedAccounts = await _db.Accounts
            .Where(a => a.ID == request.DebitAccountId || a.ID == request.CreditAccountId)
            .ToListAsync();

        var debitAccount = selectedAccounts.FirstOrDefault(a => a.ID == request.DebitAccountId);
        var creditAccount = selectedAccounts.FirstOrDefault(a => a.ID == request.CreditAccountId);

        if (debitAccount is null || creditAccount is null)
            return BadRequest(new { Message = "Selected accounts were not found." });

        var selectedBalances = await _db.AccountBalances
            .Include(ab => ab.Account)
            .Include(ab => ab.Currency)
            .Where(ab =>
                (ab.AccountID == request.DebitAccountId && ab.CurrencyID == request.DebitCurrencyId) ||
                (ab.AccountID == request.CreditAccountId && ab.CurrencyID == request.CreditCurrencyId))
            .ToListAsync();

        var debitAccountBalance = selectedBalances.FirstOrDefault(ab =>
            ab.AccountID == request.DebitAccountId &&
            ab.CurrencyID == request.DebitCurrencyId);
        var creditAccountBalance = selectedBalances.FirstOrDefault(ab =>
            ab.AccountID == request.CreditAccountId &&
            ab.CurrencyID == request.CreditCurrencyId);




        if (debitAccountBalance is null)
        {
            debitAccountBalance = new AccountBalance
            {
                AccountID = request.DebitAccountId,
                CurrencyID = request.DebitCurrencyId,
                Balance = 0,
                CreatedByUserId = userId,
                CreationDate = DateTime.UtcNow,
                Account = debitAccount
            };
            _db.AccountBalances.Add(debitAccountBalance);
        }

        if (creditAccountBalance is null)
        {
            creditAccountBalance = new AccountBalance
            {
                AccountID = request.CreditAccountId,
                CurrencyID = request.CreditCurrencyId,
                Balance = 0,
                CreatedByUserId = userId,
                CreationDate = DateTime.UtcNow,
                Account = creditAccount
            };
            _db.AccountBalances.Add(creditAccountBalance);
        }

        var debitAccountTypeId = debitAccount.AccountTypeID;
        var creditAccountTypeId = creditAccount.AccountTypeID;
        var bothAccountsAreRangeAccounts =
            new[] { 3, 4, 5 }.Contains(debitAccountTypeId) &&
            new[] { 3, 4, 5 }.Contains(creditAccountTypeId);

        var debitTransactionTypeId = bothAccountsAreRangeAccounts ? 11 : 4;
        var creditTransactionTypeId = bothAccountsAreRangeAccounts ? 11 : 3;

        var requiredTransactionTypeIds = new[] { debitTransactionTypeId, creditTransactionTypeId }
            .Distinct()
            .ToArray();

        var availableTransactionTypeIds = await _db.JournalEntryTransactionTypes
            .Where(x => requiredTransactionTypeIds.Contains(x.ID))
            .Select(x => x.ID)
            .ToListAsync();

        var missingTransactionTypeId = requiredTransactionTypeIds.FirstOrDefault(id => !availableTransactionTypeIds.Contains(id));
        if (missingTransactionTypeId > 0)
            return BadRequest(new { Message = $"Required journal transaction type {missingTransactionTypeId} was not found." });

        var previousDebitBalance = debitAccountBalance.Balance;
        var previousCreditBalance = creditAccountBalance.Balance;
        debitAccountBalance.Balance -= amount;
        creditAccountBalance.Balance += creditAmount;

        string chequePhotoPath = "/images/journalentry/default.png";
        string uploadedChequePhotoPhysicalPath = null;

        try
        {
            if (request.ChequePhoto is not null && request.ChequePhoto.Length > 0)
            {
                var webRootPath = _env.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRootPath))
                {
                    webRootPath = Path.Combine(_env.ContentRootPath, "wwwroot");
                }

                var folderPath = Path.Combine(webRootPath, "images", "journalentry");
                Directory.CreateDirectory(folderPath);

                var fileName = $"{Guid.NewGuid():N}.jpg";
                uploadedChequePhotoPhysicalPath = Path.Combine(folderPath, fileName);
                await using var stream = System.IO.File.Create(uploadedChequePhotoPhysicalPath);
                await request.ChequePhoto.CopyToAsync(stream);
                chequePhotoPath = $"/images/journalentry/{fileName}";
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            if (debitAccountBalance.ID == 0 || creditAccountBalance.ID == 0)
            {
                await _db.SaveChangesAsync();
            }

            debitAccountBalance.Balance = previousDebitBalance - amount;
            creditAccountBalance.Balance = previousCreditBalance + creditAmount;

            var debitJournalEntry = new JournalEntry
            {
                AccountBalanceID = debitAccountBalance.ID,
                Debit = amount,
                Credit = 0,
                Balance = debitAccountBalance.Balance,
                Remarks = request.Remarks,
                ChequePhoto = chequePhotoPath,
                TransactionTypeID = debitTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = DateTime.UtcNow
            };
            _db.JournalEntries.Add(debitJournalEntry);

            var creditJournalEntry = new JournalEntry
            {
                AccountBalanceID = creditAccountBalance.ID,
                Debit = 0,
                Credit = creditAmount,
                Balance = creditAccountBalance.Balance,
                Remarks = request.Remarks,
                ChequePhoto = chequePhotoPath,
                TransactionTypeID = creditTransactionTypeId,
                CreatedByUserId = userId,
                CreationDate = DateTime.UtcNow
            };
            _db.JournalEntries.Add(creditJournalEntry);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                DebitAccountBalance = debitAccountBalance.Balance,
                CreditAccountBalance = creditAccountBalance.Balance,
                CreditAmount = creditAmount,
                ExchangeRate = exchangeRate,
                ChequePhoto = chequePhotoPath,
                JournalEntries = new[]
                {
                    new
                    {
                        Id = debitJournalEntry.ID,
                        Date = debitJournalEntry.CreationDate,
                        AccountName = debitAccount.Name,
                        Currency = debitAccountBalance.Currency?.CurrencyName ?? string.Empty,
                        TransactionType = debitTransactionTypeId == 11 ? "د حسابونو تبادله" : "نقد منفي",
                        Credit = debitJournalEntry.Credit,
                        Debit = debitJournalEntry.Debit,
                        Balance = debitJournalEntry.Balance,
                        Remarks = debitJournalEntry.Remarks
                    },
                    new
                    {
                        Id = creditJournalEntry.ID,
                        Date = creditJournalEntry.CreationDate,
                        AccountName = creditAccount.Name,
                        Currency = creditAccountBalance.Currency?.CurrencyName ?? string.Empty,
                        TransactionType = creditTransactionTypeId == 11 ? "د حسابونو تبادله" : "نقد جمع",
                        Credit = creditJournalEntry.Credit,
                        Debit = creditJournalEntry.Debit,
                        Balance = creditJournalEntry.Balance,
                        Remarks = creditJournalEntry.Remarks
                    }
                }
            });
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(uploadedChequePhotoPhysicalPath) && System.IO.File.Exists(uploadedChequePhotoPhysicalPath))
                System.IO.File.Delete(uploadedChequePhotoPhysicalPath);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Message = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    private async Task<IActionResult> SaveAccountAsync(AccountCreateRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        request.Name = request.Name?.Trim() ?? string.Empty;
        request.Code = request.Code?.Trim() ?? string.Empty;
        request.NIC = request.NIC?.Trim() ?? string.Empty;
        request.FirstPhone = request.FirstPhone?.Trim() ?? string.Empty;
        request.SecondPhone = request.SecondPhone?.Trim() ?? string.Empty;
        request.Email = request.Email?.Trim() ?? string.Empty;
        request.Address = request.Address?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { Message = "نوم اړین دی." });

        var requiresContact = !AccountsAllowedTypes.Contains(request.AccountTypeID);

        if (requiresContact && string.IsNullOrWhiteSpace(request.FirstPhone))
            return BadRequest(new { Message = "First phone is required." });

        if (string.IsNullOrWhiteSpace(request.Code))
            request.Code = await GenerateNextCode();

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { Message = "کوډ نسي جوړیدای." });

        if (!AllAllowedTypes.Contains(request.AccountTypeID))
            return BadRequest(new { Message = "نامناسب حساب ډول." });

        var existsName = await _db.Accounts.AnyAsync(a => a.Name == request.Name);
        if (existsName)
            return BadRequest(new { Message = "دغه نوم مخکې استفاده سوی." });

        var existsCode = await _db.Accounts.AnyAsync(a => a.Code == request.Code);
        if (existsCode)
            return BadRequest(new { Message = "دغه کوډ مخکې سته." });

        var account = new Account
        {
            Name = request.Name,
            Code = request.Code,
            AccountTypeID = request.AccountTypeID,
            IsActive = true,
            CreatedByUserId = userId,
            CreationDate = DateTime.UtcNow
        };

        _db.Accounts.Add(account);

        if (requiresContact)
        {
            _db.AccountContacts.Add(new AccountContacts
            {
                Account = account,
                NIC = request.NIC,
                FirstPhone = request.FirstPhone,
                SecondPhone = request.SecondPhone,
                Email = request.Email,
                Address = request.Address,
                CreatedByUserId = userId,
                CreationDate = DateTime.UtcNow
            });
        }

        var allowedCurrencies = await _db.Currencies
            .Where(c => c.IsActive)
            .Select(c => c.ID)
            .ToListAsync();

        foreach (var balance in request.Balances ?? new List<AccountBalancePayload>())
        {
            if (!allowedCurrencies.Contains(balance.CurrencyID))
                continue;

            var accountBalance = new AccountBalance
            {
                Account = account,
                CurrencyID = balance.CurrencyID,
                Balance = balance.Amount,
                CreatedByUserId = userId,
                CreationDate = DateTime.UtcNow
            };

            _db.AccountBalances.Add(accountBalance);

            if (balance.Amount != 0)
            {
                _db.JournalEntries.Add(new JournalEntry
                {
                    AccountBalance = accountBalance,
                    Debit = balance.Amount < 0 ? balance.Amount : 0,
                    Credit = balance.Amount > 0 ? balance.Amount : 0,
                    Balance = balance.Amount,
                    ChequePhoto = "default.png",
                    TransactionTypeID = 1,
                    CreatedByUserId = userId,
                    CreationDate = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { account.ID });
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var account = await _db.Accounts.FindAsync(key);
        if (account is null)
            return NotFound();

        var dict = Deserialize(values);
        if (dict.Count == 0)
            return BadRequest(new { Message = "بیلابیل معلومات نه سول واخیستل." });

        if (TryGetString(dict, nameof(AccountListItem.Name), out var name))
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { Message = "نوم اړین دی." });

            if (!string.Equals(account.Name, name, StringComparison.OrdinalIgnoreCase) &&
                await _db.Accounts.AnyAsync(a => a.ID != account.ID && a.Name == name))
            {
                return BadRequest(new { Message = "دغه نوم مخکې استفاده سوی دی." });
            }

            account.Name = name.Trim();
        }

        if (TryGetString(dict, nameof(AccountListItem.Code), out var code))
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { Message = "کوډ اړین دی." });

            if (!string.Equals(account.Code, code, StringComparison.OrdinalIgnoreCase) &&
                await _db.Accounts.AnyAsync(a => a.ID != account.ID && a.Code == code))
            {
                return BadRequest(new { Message = "دغه کوډ مخکې موجود دی." });
            }

            account.Code = code.Trim();
        }

        if (TryGetInt(dict, nameof(AccountListItem.AccountTypeID), out var accountTypeId))
        {
            if (!AllAllowedTypes.Contains(accountTypeId))
                return BadRequest(new { Message = "نامناسب حساب ډول دی." });

            account.AccountTypeID = accountTypeId;
        }

        if (!AccountsAllowedTypes.Contains(account.AccountTypeID))
        {
            var contact = await _db.AccountContacts.FirstOrDefaultAsync(c => c.AccountID == key);
            if (contact is null)
            {
                contact = new AccountContacts
                {
                    AccountID = key,
                    CreatedByUserId = userId,
                    CreationDate = DateTime.UtcNow
                };
                _db.AccountContacts.Add(contact);
            }

            if (TryGetString(dict, nameof(AccountListItem.NIC), out var nic))
            {
                contact.NIC = nic;
            }

            if (TryGetString(dict, nameof(AccountListItem.FirstPhone), out var firstPhone))
            {
                if (string.IsNullOrWhiteSpace(firstPhone))
                    return BadRequest(new { Message = "First phone is required." });

                contact.FirstPhone = firstPhone;
            }

            if (TryGetString(dict, nameof(AccountListItem.SecondPhone), out var secondPhone))
            {
                contact.SecondPhone = secondPhone;
            }

            if (TryGetString(dict, nameof(AccountListItem.Email), out var email))
            {
                contact.Email = email;
            }

            if (TryGetString(dict, nameof(AccountListItem.Address), out var address))
            {
                contact.Address = address;
            }
        }

        if (TryGetBool(dict, nameof(AccountListItem.IsActive), out var isActive))
        {
            account.IsActive = isActive;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    private static Dictionary<string, JsonElement> Deserialize(string values)
    {
        if (string.IsNullOrWhiteSpace(values))
            return new Dictionary<string, JsonElement>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(values) ?? new Dictionary<string, JsonElement>();
        }
        catch
        {
            return new Dictionary<string, JsonElement>();
        }
    }

    private static bool TryGetString(Dictionary<string, JsonElement> dict, string key, out string value)
    {
        value = string.Empty;
        if (dict.TryGetValue(key, out var element) && element.ValueKind != JsonValueKind.Null && element.ValueKind != JsonValueKind.Undefined)
        {
            value = (element.GetString() ?? string.Empty).Trim();
            return true;
        }

        return false;
    }

    private static bool TryGetInt(Dictionary<string, JsonElement> dict, string key, out int value)
    {
        if (dict.TryGetValue(key, out var element) && element.TryGetInt32(out value))
            return true;

        value = default;
        return false;
    }

    private static bool TryGetBool(Dictionary<string, JsonElement> dict, string key, out bool value)
    {
        if (dict.TryGetValue(key, out var element) && element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = element.GetBoolean();
            return true;
        }

        value = default;
        return false;
    }

    private int[] ParseRequestedTypes(string rawTypes)
    {
        if (string.IsNullOrWhiteSpace(rawTypes))
            return null;

        var parsed = rawTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment =>
            {
                var trimmed = segment.Trim();
                return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
            })
            .Where(value => value > 0 && AllAllowedTypes.Contains(value))
            .Distinct()
            .ToArray();

        return parsed.Length == 0 ? null : parsed;
    }

    private async Task<string> GenerateNextCode()
    {
        var prefix = AccountDefinitions.DefaultCodePrefix;
        var existingCodes = await _db.Accounts
            .Where(a => a.Code.StartsWith(prefix))
            .Select(a => a.Code)
            .ToListAsync();

        var maxNumber = existingCodes
            .Select(code => code.Length > prefix.Length ? code.Substring(prefix.Length) : "0")
            .Select(fragment => int.TryParse(fragment, out var parsed) ? parsed : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{(maxNumber + 1):D3}";
    }

}

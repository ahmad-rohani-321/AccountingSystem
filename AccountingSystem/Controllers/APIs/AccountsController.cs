using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models.Accounts;
using AccountingSystem.Models.Accounting;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs;

[Route("api/[controller]")]
[ApiController]
    [Authorize]
    public class AccountsController(IHttpContextAccessor accessor, ApplicationDbContext db) : ControllerBase
    {

    private readonly IHttpContextAccessor _accessor = accessor;
    private readonly ApplicationDbContext _db = db;

    private static readonly int[] AccountsAllowedTypes = { 1, 2, 6, 7 };
    private static readonly int[] IndexAllowedTypes = { 3, 4, 5, 9 };
    private static readonly int[] ContributorAllowedTypes = { 8 };
    private static readonly int[] AllAllowedTypes = AccountsAllowedTypes
        .Concat(IndexAllowedTypes)
        .Concat(ContributorAllowedTypes)
        .ToArray();

    [HttpGet]
    public async Task<object> Get(DataSourceLoadOptions loadOptions, string? types = null)
    {
        var requestedTypes = ParseRequestedTypes(types) ?? AccountsAllowedTypes;

        var accounts = await _db.Accounts
            .Include(a => a.AccountType)
            .Where(a => requestedTypes.Contains(a.AccountTypeID))
            .OrderByDescending(a => a.CreationDate)
            .AsNoTracking()
            .ToListAsync();

        var accountIds = accounts.Select(a => a.ID).ToList();
        var contacts = await _db.AccountContacts
            .Where(c => accountIds.Contains(c.AccountID))
            .AsNoTracking()
            .ToListAsync();

        var data = accounts.Select(account =>
        {
            var contact = contacts.FirstOrDefault(c => c.AccountID == account.ID);
            return new AccountListItem
            {
                AccountID = account.ID,
                Name = account.Name,
                Code = account.Code,
                AccountTypeID = account.AccountTypeID,
                AccountTypeName = account.AccountType?.Name ?? string.Empty,
                IsActive = account.IsActive,
                AccountContactID = contact?.ID,
                NIC = contact?.NIC ?? string.Empty,
                FirstPhone = contact?.FirstPhone ?? string.Empty,
                SecondPhone = contact?.SecondPhone ?? string.Empty,
                Email = contact?.Email ?? string.Empty,
                Address = contact?.Address ?? string.Empty
            };
        }).ToList();

        return DataSourceLoader.Load(data, loadOptions);
    }

    [HttpGet("next-code")]
    public async Task<IActionResult> NextCode()
    {
        var nextCode = await GenerateNextCode();
        return Ok(new { Code = nextCode });
    }

    [HttpPost]
    public Task<IActionResult> Post([FromBody] AccountCreateRequest request) => SaveAccountAsync(request);

    [HttpPost("create")]
    public Task<IActionResult> Create([FromBody] AccountCreateRequest request) => SaveAccountAsync(request);

    private async Task<IActionResult> SaveAccountAsync(AccountCreateRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        request.Name = request.Name?.Trim() ?? string.Empty;
        request.Code = request.Code?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { Message = "نوم اړین دی." });

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
                    Debit = balance.Amount > 0 ? balance.Amount : 0,
                    Credit = balance.Amount < 0 ? balance.Amount : 0,
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
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var account = await _db.Accounts.FindAsync(key);
        if (account is null)
            return NotFound();

        var dict = Deserialize(values);
        if (dict.Count == 0)
            return BadRequest(new { Message = "بیلابیل معلومات نه شول واخیستل." });

        if (TryGetString(dict, nameof(AccountListItem.Name), out var name))
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { Message = "نوم اړین دی." });

            if (!string.Equals(account.Name, name, StringComparison.OrdinalIgnoreCase) &&
                await _db.Accounts.AnyAsync(a => a.ID != account.ID && a.Name == name))
            {
                return BadRequest(new { Message = "دغه نوم مخکې استفاده شوی دی." });
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

    private int[]? ParseRequestedTypes(string? rawTypes)
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

    private string GetUserId()
    {
        var principal = _accessor.HttpContext?.User ?? User;
        return principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }
}


using System;
using System.Collections.Generic;
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

    [HttpGet]
    public async Task<object> Get(DataSourceLoadOptions loadOptions)
    {
        var accounts = await _db.Accounts
            .Include(a => a.AccountType)
            .AsNoTracking()
            .ToListAsync();

        var contacts = await _db.AccountContacts
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
    public async Task<IActionResult> Post([FromBody] AccountCreateRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        request.Name = request.Name?.Trim() ?? string.Empty;
        request.Code = request.Code?.Trim() ?? string.Empty;
        request.FirstPhone = request.FirstPhone?.Trim() ?? string.Empty;
        request.SecondPhone = request.SecondPhone?.Trim() ?? string.Empty;
        request.Email = request.Email?.Trim() ?? string.Empty;
        request.Address = request.Address?.Trim() ?? string.Empty;
        request.NIC = request.NIC?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { Message = "نوم اړین دی." });

        if (string.IsNullOrWhiteSpace(request.Code))
            request.Code = await GenerateNextCode();

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { Message = "کوډ نشي جوړیدای." });

        if (string.IsNullOrWhiteSpace(request.FirstPhone))
            return BadRequest(new { Message = "لومړی شمېره اړینه ده." });

        if (!AccountDefinitions.AllowedAccountTypeIds.Contains(request.AccountTypeID))
            return BadRequest(new { Message = "نامناسب حساب ډول." });

        var existsName = await _db.Accounts.AnyAsync(a => a.Name == request.Name);
        if (existsName)
            return BadRequest(new { Message = "دغه نوم مخکې استفاده شوی." });

        var existsCode = await _db.Accounts.AnyAsync(a => a.Code == request.Code);
        if (existsCode)
            return BadRequest(new { Message = "دغه کوډ مخکې شته." });

        if (!string.IsNullOrWhiteSpace(request.NIC))
        {
            var existsNic = await _db.AccountContacts.AnyAsync(c => c.NIC == request.NIC);
            if (existsNic)
                return BadRequest(new { Message = "دغه تذکره کارول شوې." });
        }

        var account = new Account
        {
            Name = request.Name,
            Code = request.Code,
            AccountTypeID = request.AccountTypeID,
            IsActive = true,
            CreatedByUserId = userId,
            CreationDate = DateTime.UtcNow
        };

        var contact = new AccountContacts
        {
            Account = account,
            FirstPhone = request.FirstPhone,
            SecondPhone = request.SecondPhone,
            Email = request.Email,
            Address = request.Address,
            NIC = request.NIC,
            CreatedByUserId = userId,
            CreationDate = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        _db.AccountContacts.Add(contact);

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

        var contact = await _db.AccountContacts.FirstOrDefaultAsync(c => c.AccountID == account.ID);
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
            if (!AccountDefinitions.AllowedAccountTypeIds.Contains(accountTypeId))
                return BadRequest(new { Message = "نامناسب حساب ډول دی." });

            account.AccountTypeID = accountTypeId;
        }

        if (TryGetBool(dict, nameof(AccountListItem.IsActive), out var isActive))
        {
            account.IsActive = isActive;
        }

        if (TryGetString(dict, nameof(AccountListItem.FirstPhone), out var firstPhone))
        {
            if (string.IsNullOrWhiteSpace(firstPhone))
                return BadRequest(new { Message = "لومړی شمېره اړینه ده." });
        }

        if (contact is null)
        {
            contact = new AccountContacts
            {
                Account = account,
                CreatedByUserId = userId,
                CreationDate = DateTime.UtcNow
            };
            _db.AccountContacts.Add(contact);
        }

        if (TryGetString(dict, nameof(AccountListItem.FirstPhone), out var firstPhoneValue))
        {
            contact.FirstPhone = firstPhoneValue.Trim();
        }

        if (TryGetString(dict, nameof(AccountListItem.SecondPhone), out var secondPhone))
        {
            contact.SecondPhone = secondPhone.Trim();
        }

        if (TryGetString(dict, nameof(AccountListItem.Email), out var email))
        {
            contact.Email = email.Trim();
        }

        if (TryGetString(dict, nameof(AccountListItem.Address), out var address))
        {
            contact.Address = address.Trim();
        }

        if (TryGetString(dict, nameof(AccountListItem.NIC), out var nic))
        {
            if (!string.IsNullOrWhiteSpace(nic) &&
                !string.Equals(contact.NIC, nic, StringComparison.OrdinalIgnoreCase) &&
                await _db.AccountContacts.AnyAsync(c => c.ID != contact.ID && c.NIC == nic))
            {
                return BadRequest(new { Message = "دغه تذکره مخکې ثبت شوې." });
            }

            contact.NIC = nic.Trim();
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

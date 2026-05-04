using AccountingSystem.Data;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;

namespace AccountingSystem.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _db;

    public AccountController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var accountTypeOptions = await _db.AccountTypes
            .Where(a => AccountDefinitions.AllowedAccountTypeIds.Contains(a.ID))
            .Select(a => new AccountTypeOption { ID = a.ID, Name = a.Name })
            .ToListAsync();

        ViewBag.AccountTypeOptions = accountTypeOptions;
        ViewBag.AccountTypeOptionsJson = JsonSerializer.Serialize(accountTypeOptions);

        var currencies = await _db.Currencies
            .Where(c => c.IsActive)
            .Select(c => new { c.ID, c.CurrencyName })
            .ToListAsync();

        ViewBag.ActiveCurrenciesJson = JsonSerializer.Serialize(currencies);
        return View();
    }

    public async Task<IActionResult> Accounts()
    {

        var allowedAccountTypeIds = new[] { 1, 2, 6, 7 };
        var accountTypeOptions = await _db.AccountTypes
            .Where(a => allowedAccountTypeIds.Contains(a.ID))
            .Select(a => new AccountTypeOption { ID = a.ID, Name = a.Name })
            .ToListAsync();

        ViewBag.AccountTypeOptions = accountTypeOptions;
        ViewBag.AccountTypeOptionsJson = JsonSerializer.Serialize(accountTypeOptions);

        var currencies = await _db.Currencies
            .Where(c => c.IsActive)
            .Select(c => new { c.ID, c.CurrencyName })
            .ToListAsync();

        ViewBag.ActiveCurrenciesJson = JsonSerializer.Serialize(currencies);
        return View();
    }

    public async Task<IActionResult> Contributors()
    {
        var accountTypeOptions = await _db.AccountTypes
            .Where(a => a.ID == 8)
            .Select(a => new AccountTypeOption { ID = a.ID, Name = a.Name })
            .ToListAsync();

        ViewBag.AccountTypeOptions = accountTypeOptions;
        ViewBag.AccountTypeOptionsJson = JsonSerializer.Serialize(accountTypeOptions);

        var currencies = await _db.Currencies
            .Where(c => c.IsActive)
            .Select(c => new { c.ID, c.CurrencyName })
            .ToListAsync();

        ViewBag.ActiveCurrenciesJson = JsonSerializer.Serialize(currencies);
        return View();
    }
}

using AccountingSystem.Data;
using AccountingSystem.Models.Accounts;
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
        ViewData["Title"] = "حساب";
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
}

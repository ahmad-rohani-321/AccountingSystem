using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult Accounts() => View();
        public IActionResult Contributors() => View();
        public IActionResult Shares(int accountId) => View(accountId);
    }
}

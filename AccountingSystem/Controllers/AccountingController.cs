using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    [Authorize]
    public class AccountingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Transactions()
        {
            return View();
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
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

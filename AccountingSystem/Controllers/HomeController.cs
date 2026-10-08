using AccountingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AccountingSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.IsInRole("Seller")) return RedirectToAction("Index", "Sale");
            if (User.IsInRole("Purchaser")) return RedirectToAction("Index", "Purchase");
            if (User.IsInRole("Warehouse Man")) return RedirectToAction("Index", "Inventory");
            if (User.IsInRole("Finance Manager")) return RedirectToAction("Transactions", "Accounting");
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

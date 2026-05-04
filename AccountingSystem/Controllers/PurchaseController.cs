using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    public class PurchaseController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult NewPurchase()
        {
            ViewData["title"] = "نوی خرید";

            return View();
        }

    }
}

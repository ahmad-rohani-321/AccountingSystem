using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    public class PurchaseController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult PurchaseEdit(int id)
        {
            ViewData["title"] = "د خرید سمون";
            ViewData["PurchaseId"] = id;

            return View();
        }

        public IActionResult NewPurchase()
        {
            ViewData["title"] = "نوی خرید";

            return View();
        }

    }
}

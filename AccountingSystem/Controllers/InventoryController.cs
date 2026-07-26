using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    public class InventoryController : Controller
    {
        public IActionResult Stocks()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}

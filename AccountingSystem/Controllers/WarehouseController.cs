using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    public class WarehouseController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RemainingStock()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View(); 
        }
    }
}

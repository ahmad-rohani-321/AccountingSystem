using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    public class SaleController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult NewSale() => View();
        public IActionResult EditSale(int id) => View(id);
    }
}

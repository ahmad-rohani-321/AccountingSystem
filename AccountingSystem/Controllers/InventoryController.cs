using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        public IActionResult Stocks() => View();

        public IActionResult Index() => View();

        public IActionResult StockItems() => View();
        
        public IActionResult StockHistory() => View();

        public IActionResult MinAlerts() => View();
    }
}

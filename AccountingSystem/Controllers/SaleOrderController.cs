using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    [Authorize]
    public class SaleOrderController : Controller
    {
        // GET: SaleOrderController
        public ActionResult Index()
        {
            return View();
        }

        public IActionResult NewSaleOrder(int? orderId)
        {
            ViewBag.OrderID = orderId;
            return View();
        }
    }
}

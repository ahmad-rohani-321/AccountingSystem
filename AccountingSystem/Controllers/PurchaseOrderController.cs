using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    [Authorize]
    public class PurchaseOrderController : Controller
    {
        // GET: PurchaseOrderController
        public ActionResult Index()
        {
            return View();
        }

        public IActionResult NewPurchaseOrder(int? orderId)
        {
            ViewBag.OrderID = orderId;
            return View();
        }
    }
}

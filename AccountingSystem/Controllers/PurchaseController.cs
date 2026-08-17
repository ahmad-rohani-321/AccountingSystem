using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    [Authorize]
    public class PurchaseController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult NewPurchase() => View();

        public IActionResult EditPurchase(int purchaseId) => View(purchaseId);
    }
}

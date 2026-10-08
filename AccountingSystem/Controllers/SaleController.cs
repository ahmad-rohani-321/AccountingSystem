using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = AccountingSystem.Models.Identity.SystemRoles.Sales)]
    public class SaleController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult NewSale() => View();
        public IActionResult EditSale(int saleId) => View(saleId);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        [Authorize(Roles = AccountingSystem.Models.Identity.SystemRoles.Sales)]
        public IActionResult SalesDashboard() => View();
        [Authorize(Roles = AccountingSystem.Models.Identity.SystemRoles.Purchases)]
        public IActionResult PurchasesDashboard() => View();
        [Authorize(Roles = AccountingSystem.Models.Identity.SystemRoles.Finance)]
        public IActionResult AccountBalancesDashboard() => View();
    }
}

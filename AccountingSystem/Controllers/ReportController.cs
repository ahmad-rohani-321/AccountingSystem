using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        public IActionResult SalesDashboard() => View();
    }
}

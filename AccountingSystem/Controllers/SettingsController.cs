using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

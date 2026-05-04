using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers
{
    public class ActionsController : Controller
    {
        public ActionResult JournalEntry()
        {
            return View();
        }

        public ActionResult ItemPrices() => View();
    }
}

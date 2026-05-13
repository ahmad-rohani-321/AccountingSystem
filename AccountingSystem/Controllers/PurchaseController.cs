using AccountingSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers
{
    public class PurchaseController(ApplicationDbContext db) : Controller
    {
        private readonly ApplicationDbContext _db = db;

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PurchaseEdit(int id)
        {
            var purchase = await _db.Purchases
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id);

            if (purchase is null || !purchase.IsHolded)
                return RedirectToAction(nameof(Index));

            ViewData["title"] = "د خرید سمون";
            ViewData["PurchaseId"] = id;

            return View();
        }

        public IActionResult NewPurchase()
        {
            ViewData["title"] = "نوی خرید";

            return View();
        }
    }
}

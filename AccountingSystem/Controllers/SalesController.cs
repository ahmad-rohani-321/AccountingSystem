using AccountingSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers
{
    public class SalesController(ApplicationDbContext db) : Controller
    {
        private readonly ApplicationDbContext _db = db;

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SaleEdit(int id)
        {
            var sale = await _db.Sales
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id);

            if (sale is null || !sale.IsHolded)
                return RedirectToAction(nameof(Index));

            ViewData["title"] = "د فروش سمون";
            ViewData["SaleId"] = id;

            return View();
        }

        public IActionResult NewSale()
        {
            ViewData["title"] = "نوی فروش";

            return View();
        }
    }
}

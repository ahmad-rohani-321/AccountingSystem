using AccountingSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseController(ApplicationDbContext context, IHttpContextAccessor accessor) : ControllerBase
{      
    private readonly ApplicationDbContext _context = context;
    private readonly IHttpContextAccessor _contextAccessor = accessor;


    [HttpGet("Next-No")]
    public async Task<IActionResult> GetNextNo()
    {
        var lastPurchaseNo = await _context.Purchases
            .Select(p => (int?)p.PurchaseNo)
            .MaxAsync() ?? 0;

        return Ok(new { PurchaseNo = lastPurchaseNo + 1 });
    }
}

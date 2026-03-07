using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Data;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StockBalancesController(ApplicationDbContext db) : ControllerBase
    {
        private readonly ApplicationDbContext _db = db;

        [HttpGet]
        public async Task<object> Get(DataSourceLoadOptions loadOptions)
        {
            var data = await _db.StockBalances
                .AsNoTracking()
                .Include(sb => sb.Item)
                    .ThenInclude(i => i.Unit)
                .Include(sb => sb.Warehouse)
                .ToListAsync();

            return DataSourceLoader.Load(data, loadOptions);
        }
    }
}

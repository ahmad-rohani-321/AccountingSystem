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
            var query = _db.StockBalances
                .AsNoTracking()
                .Where(sb => sb.Quantity > 0 && sb.Item.IsActive && sb.Item.Unit.IsActive && sb.Warehouse.IsActive)
                .Include(sb => sb.Item)
                    .ThenInclude(i => i.Unit)
                .Include(sb => sb.Warehouse)
                .AsQueryable();

            return await DataSourceLoader.LoadAsync(query, loadOptions);
        }
    }
}

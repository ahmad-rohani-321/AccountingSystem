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

        [HttpGet("LeastItems")]
        public async Task<object> GetLeastItems(
            DataSourceLoadOptions loadOptions,
            int? itemId,
            int? warehouseId,
            int? categoryId,
            int? unitId)
        {
            var baseQuery = _db.StockBalances
                .AsNoTracking()
                .Where(sb =>
                    sb.Item != null &&
                    sb.Item.IsActive &&
                    sb.Item.Category != null &&
                    sb.Item.Category.IsActive &&
                    sb.Item.Unit != null &&
                    sb.Item.Unit.IsActive &&
                    sb.Warehouse != null &&
                    sb.Warehouse.IsActive);

            if (itemId.HasValue && itemId.Value > 0)
                baseQuery = baseQuery.Where(sb => sb.ItemID == itemId.Value);

            if (warehouseId.HasValue && warehouseId.Value > 0)
                baseQuery = baseQuery.Where(sb => sb.WarehouseID == warehouseId.Value);

            if (categoryId.HasValue && categoryId.Value > 0)
                baseQuery = baseQuery.Where(sb => sb.Item.CategoryId == categoryId.Value);

            if (unitId.HasValue && unitId.Value > 0)
                baseQuery = baseQuery.Where(sb => sb.Item.UnitId == unitId.Value);

            // Quantity in StockBalance is already stored in the main unit.
            // Group by item + warehouse to combine batches of the same item in the same warehouse.
            const decimal equalityTolerance = 0.000001m;

            var query = baseQuery
                .GroupBy(sb => new
                {
                    sb.ItemID,
                    sb.WarehouseID,
                    sb.Item.NativeName,
                    sb.Item.AliasName,
                    sb.Item.MinimumQuantity,
                    sb.Item.CategoryId,
                    CategoryName = sb.Item.Category.Name,
                    sb.Item.UnitId,
                    UnitName = sb.Item.Unit.Name,
                    WarehouseName = sb.Warehouse.Name
                })
                .Select(g => new
                {
                    g.Key.ItemID,
                    g.Key.WarehouseID,
                    g.Key.NativeName,
                    g.Key.AliasName,
                    g.Key.MinimumQuantity,
                    g.Key.CategoryId,
                    g.Key.CategoryName,
                    g.Key.UnitId,
                    g.Key.UnitName,
                    g.Key.WarehouseName,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .Where(x => x.Quantity <= x.MinimumQuantity + equalityTolerance)
                .OrderBy(x => x.NativeName)
                .ThenBy(x => x.WarehouseName);

            return await DataSourceLoader.LoadAsync(query, loadOptions);
        }
    }
}

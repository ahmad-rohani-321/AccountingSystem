using AccountingSystem.Models.Inventory;

namespace AccountingSystem.Repository.Inventory;
public interface IWarehouseRepository
{
    Task<List<WareHouse>> GetAllAsync();
    Task<WareHouse> GetByIdAsync(int id);
    Task AddAsync(WareHouse warehouse);
    Task UpdateAsync(WareHouse warehouse);
    Task UpdateRangeAsync(List<WareHouse> list);
    Task<bool> Exists(int id);
}

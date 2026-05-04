namespace AccountingSystem.Repository.Inventory;

using AccountingSystem.Models.Inventory;

public interface IUnitRepository
{
    Task<List<Unit>> GetAllAsync();
    Task<Unit> GetByIdAsync(int id);
    Task AddAsync(Unit unit);
    Task UpdateAsync(Unit unit);
    Task<bool> Exists(int id);
}

using AccountingSystem.Models.Inventory;

namespace AccountingSystem.Repository.Inventory;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category> GetByIdAsync(int id);
    Task AddAsync(Category category);
    Task UpdateAsync(Category entity);
    Task<bool> Exists(int id);
}

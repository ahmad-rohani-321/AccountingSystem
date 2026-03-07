using AccountingSystem.Data;
using AccountingSystem.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Repository.Inventory
{
    public class CategoryRepository(ApplicationDbContext context) : ICategoryRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Categories.Include(x => x.CreatedByUser).ToListAsync();
        }

        public async Task<Category> GetByIdAsync(int id)
        {
            return await _context.Categories.FirstOrDefaultAsync(x => x.ID == id);
        }

        public async Task AddAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category entity)
        {
            _context.Categories.Update(entity);
            await _context.SaveChangesAsync();
        }


        public async Task<bool> Exists(int id)
        {
            return await _context.Categories.AnyAsync(c => c.ID == id);
        }
    }
}

using AccountingSystem.Data;
using AccountingSystem.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Repository.Inventory
{
    public class UnitRepository(ApplicationDbContext context) : IUnitRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<List<Unit>> GetAllAsync()
        {
            return await _context.Units.Include(x => x.CreatedByUser).ToListAsync();
        }

        public async Task<Unit> GetByIdAsync(int id)
        {
            return await _context.Units.FirstOrDefaultAsync(x => x.ID == id);
        }

        public async Task AddAsync(Unit unit)
        {
            await _context.Units.AddAsync(unit);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Unit unit)
        {
            _context.Units.Update(unit);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> Exists(int id)
        {
            return await _context.Units.AnyAsync(u => u.ID == id);
        }
    }
}

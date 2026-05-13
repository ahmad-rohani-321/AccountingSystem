using AccountingSystem.Data;
using AccountingSystem.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Repository.Inventory
{
    public class WarehouseRepository(ApplicationDbContext context) : IWarehouseRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<List<WareHouse>> GetAllAsync()
        {
            return await _context.WareHouses
                .Include(x => x.CreatedByUser)
                .ToListAsync();
        }

        public async Task<WareHouse> GetByIdAsync(int id)
        {
            return await _context.WareHouses.FirstOrDefaultAsync(x => x.ID == id);
        }

        public async Task AddAsync(WareHouse warehouse)
        {
            await _context.WareHouses.AddAsync(warehouse);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(WareHouse warehouse)
        {
            _context.WareHouses.Update(warehouse);
            await _context.SaveChangesAsync();
        }
    }
}

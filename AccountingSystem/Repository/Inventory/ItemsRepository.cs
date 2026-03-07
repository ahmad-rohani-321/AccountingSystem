namespace AccountingSystem.Repository.Inventory;

using AccountingSystem.Data;
using AccountingSystem.Models.Inventory;
using Microsoft.EntityFrameworkCore;

public class ItemsRepository(ApplicationDbContext context) : IItemsRepository
{
    private readonly ApplicationDbContext _context = context;
    public async Task AddItemAsync(Item item, List<UnitConversion> unitConversions)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.Items.Add(item);

        if (unitConversions is { Count: > 0 })
        {
            foreach (var unitConversion in unitConversions)
            {
                unitConversion.Item = item;
            }

            _context.UnitConversion.AddRange(unitConversions);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public Task<Item> GetItemByIdAsync(int id)
    {
        return _context.Items
            .Include(i => i.Category)
            .Include(i => i.Unit)
            .Include(i => i.CreatedByUser)
            .FirstOrDefaultAsync(i => i.ID == id);
    }

    public Task UpdateItemAsync(Item item)
    {
        _context.Items.Update(item);
        return _context.SaveChangesAsync();
    }

    public async Task<List<Item>> GetAllItemsAsync()
    {
        return await _context.Items
            .Include(i => i.Category)
            .Include(i => i.Unit)
            .Include(i => i.CreatedByUser)
            .ToListAsync();
    }
}

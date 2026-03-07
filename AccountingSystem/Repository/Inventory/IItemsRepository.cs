using System;
using AccountingSystem.Models.Inventory;

namespace AccountingSystem.Repository.Inventory;

public interface IItemsRepository
{
    Task<List<Item>> GetAllItemsAsync();
    Task<Item> GetItemByIdAsync(int id);
    Task AddItemAsync(Item item, List<UnitConversion> unitConversions);
    Task UpdateItemAsync(Item item);
}

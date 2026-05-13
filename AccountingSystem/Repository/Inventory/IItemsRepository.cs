using AccountingSystem.Models.Inventory;

namespace AccountingSystem.Repository.Inventory;

public interface IItemsRepository
{
    Task AddItemAsync(Item item, List<UnitConversion> unitConversions);
    Task UpdateItemAsync(Item item);
}

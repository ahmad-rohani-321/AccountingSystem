using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.Models.Inventory
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public string Description { get; set; } = default!;
        public bool IsActive { get; set; } = true;
    }
}

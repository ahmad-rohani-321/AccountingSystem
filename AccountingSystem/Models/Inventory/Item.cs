using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Inventory
{
    public class Item : BaseEntity
    {
        public string ImageName { get; set; } = default!; // Image file name, GUID.ToString() for image name to ensure uniqueness of the file name, stored in wwwroot/images/itemsfolder,  e.g. "e3b0c442-98fc-1c14-9afbf4c8996fb924.jpg"
        public string NativeName { get; set; } = string.Empty;
        public string AliasName { get; set; } = string.Empty;
        public string SKU { get; set; } = default!; // SKU Stock Keeping Unit, Unique code for item
        public string SerialNumber { get; set; } = default!;
        public string Description { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public decimal MinimumQuantity { get; set; }
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; }

        public int UnitId { get; set; }
        [ForeignKey(nameof(UnitId))]
        public Unit Unit { get; set; }

    }
}

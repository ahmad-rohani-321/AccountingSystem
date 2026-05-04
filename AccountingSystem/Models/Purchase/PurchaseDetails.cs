using AccountingSystem.Models.Inventory;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Purchase
{
    public class PurchaseDetails : BaseEntity
    {
        public int ItemID { get; set; }
        public int PurchaseID { get; set; }
        public decimal Quantity { get; set; }
        public decimal PerPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int WarehouseID { get; set; }
        public string Remarks { get; set; }
        [ForeignKey(nameof(ItemID))]
        public Item Item { get; set; }
        [ForeignKey(nameof(PurchaseID))]
        public Purchase Purchase { get; set; }

        [ForeignKey(nameof(WarehouseID))]
        public WareHouse Warehouse { get; set; }
    }
}

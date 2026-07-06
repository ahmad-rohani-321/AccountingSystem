using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Inventory;
public class StockBalance : BaseEntity
{
    public decimal Quantity { get; set; }
    public string Remarks { get; set; }
    public int WarehouseID { get; set; }
    public int ItemID { get; set; }

    [ForeignKey(nameof(ItemID))]
    public Item Item { get; set; }

    [ForeignKey(nameof(WarehouseID))]
    public WareHouse Warehouse { get; set; }
}
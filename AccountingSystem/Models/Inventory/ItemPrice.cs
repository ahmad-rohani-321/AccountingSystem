using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Inventory;

public class ItemPrice : BaseEntity
{
    public int ItemID { get; set; }
    public decimal PurchaseLastPrice { get; set; }
    public decimal SalePrice { get; set; }
    public string Remarks { get; set; }

    [ForeignKey(nameof(ItemID))]
    public Item Item { get; set; }
}

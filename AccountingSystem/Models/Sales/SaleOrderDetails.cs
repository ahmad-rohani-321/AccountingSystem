using System;
using System.ComponentModel.DataAnnotations.Schema;
using AccountingSystem.Models.Inventory;

namespace AccountingSystem.Models.Sales;

public class SaleOrderDetails : BaseEntity
{
    public int ItemID { get; set; }
    public int UnitID { get; set; }
    public decimal Quantity { get; set; }
    public string Remarks { get; set; }

    [ForeignKey(nameof(ItemID))]
    public Item Item { get; set; }

    [ForeignKey(nameof(UnitID))]
    public UnitConversion Unit { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations.Schema;
using AccountingSystem.Models.Inventory;

namespace AccountingSystem.Models.Sales;

public class SaleDetails : BaseEntity
{
    public int ItemID { get; set; }
    public int SaleID { get; set; }
    public decimal Quantity { get; set; }
    public decimal PerPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal Profit { get; set; }
    public int WarehouseID { get; set; }
    public string Remarks { get; set; }
    public int UnitConversionID { get; set; }

    [ForeignKey(nameof(ItemID))]
    public Item Item { get; set; }

    [ForeignKey(nameof(SaleID))]
    public Sales Sale { get; set; }

    [ForeignKey(nameof(WarehouseID))]
    public WareHouse Warehouse { get; set; }

    [ForeignKey(nameof(UnitConversionID))]
    public UnitConversion UnitConversion { get; set; }
}

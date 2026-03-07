using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Inventory;

public class UnitConversion : BaseEntity
{
    public int ItemID { get; set; }
    public int MainUnitId { get; set; }
    public int SubUnitID { get; set; }
    public decimal MainAmount { get; set; }
    public decimal SubAmount { get; set; }
    public decimal ExchangedAmount { get; set; }


    public string Remarks { get; set; }

    [ForeignKey(nameof(ItemID))]
    public Item Item { get; set; }

    // Navigation properties
    [ForeignKey(nameof(MainUnitId))]
    public Unit MainUnit { get; set; }

    [ForeignKey(nameof(SubUnitID))]
    public Unit SubUnit { get; set; }
}

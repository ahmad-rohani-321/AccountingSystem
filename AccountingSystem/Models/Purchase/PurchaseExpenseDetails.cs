using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Purchase;

public class PurchaseExpenseDetails : BaseEntity
{
    public int PurchaseExpenseID { get; set; }
    public int PurchaseDetailItemID { get; set; }
    public decimal PerExpense { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal PerTamamShud { get; set; }
    public decimal TotalTamamShud { get; set; }
    public decimal ItemPrice { get; set; }

    [ForeignKey(nameof(PurchaseExpenseID))]
    public PurchaseExpences PurchaseExpense { get; set; }

    [ForeignKey(nameof(PurchaseDetailItemID))]
    public PurchaseDetails PurchaseDetails { get; set; }
}

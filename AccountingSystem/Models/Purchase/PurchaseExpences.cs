using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Purchase;

public class PurchaseExpences : BaseEntity
{
    public int PurchaseID { get; set; }
    public string Remarks { get; set; }
    public decimal TotalExpense { get; set; }

    [ForeignKey(nameof(PurchaseID))]
    public Purchase Purchase { get; set; }
}

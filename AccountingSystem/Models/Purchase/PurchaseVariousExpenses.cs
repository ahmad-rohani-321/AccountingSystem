using System;
using System.ComponentModel.DataAnnotations.Schema;
using AccountingSystem.Models.Settings;

namespace AccountingSystem.Models.Purchase;

public class PurchaseVariousExpenses : BaseEntity
{
    public int PurchaseExpenseID { get; set; }
    public int AccountID { get; set; }
    public int CurrencyID { get; set; }
    public decimal Amount { get; set; }
    public string Remarks { get; set; }

    [ForeignKey(nameof(PurchaseExpenseID))]
    public PurchaseExpences PurchaseExpense { get; set; }

    [ForeignKey(nameof(AccountID))]
    public Accounts.Account Account { get; set; }

    [ForeignKey(nameof(CurrencyID))]
    public Currency Currency { get; set; }
}

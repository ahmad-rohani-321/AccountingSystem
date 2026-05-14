using System;
using System.ComponentModel.DataAnnotations.Schema;
using AccountingSystem.Models.Settings;

namespace AccountingSystem.Models.Sales;

public class Sales : BaseEntity
{
    public int AccountID { get; set; }
    public int SaleNo { get; set; }
    public int CurrencyID { get; set; }
    public string Remarks { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsRefunded { get; set; } = false;
    public bool IsHolded { get; set; } = false;

    [ForeignKey(nameof(AccountID))]
    public Accounts.Account Account { get; set; }

    [ForeignKey(nameof(CurrencyID))]
    public Currency Currency { get; set; }
}

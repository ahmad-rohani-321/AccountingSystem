using System;
using System.ComponentModel.DataAnnotations.Schema;
using AccountingSystem.Models.Settings;

namespace AccountingSystem.Models.Shares;

public class SharesCalculator : BaseEntity
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public decimal Expences { get; set; }
    public decimal OurLoans { get; set; }
    public decimal OthersLoans { get; set; }
    public decimal Purchases { get; set; }
    public decimal Sales { get; set; }
    public decimal SaleProfit { get; set; }
    public decimal Saleries { get; set; }
    public decimal Payables { get; set; }
    public decimal Receivables { get; set; }
    public decimal DividableAmount { get; set; }

    public int MainCurrencyId { get; set; }
    public string Remarks { get; set; }

    [ForeignKey(nameof(MainCurrencyId))]
    public Currency MainCurrency { get; set; }
}

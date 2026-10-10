using System;
using System.ComponentModel.DataAnnotations.Schema;
using AccountingSystem.Models.Accounts;
using AccountingSystem.Models.Settings;

namespace AccountingSystem.Models.Shares;

public class SharesDivider : BaseEntity
{
    public decimal Amount { get; set; }
    public decimal ShareAmount { get; set; }
    public decimal Weight { get; set; }
    public decimal Percentage { get; set; }
    public string Remarks { get; set; }

    public int AccountId { get; set; }
    [ForeignKey(nameof(AccountId))]
    public Account Account { get; set; }

    public int CurrencyId { get; set; }
    [ForeignKey(nameof(CurrencyId))]
    public Currency Currency { get; set; }
    public int SharesCalculatorID { get; set; }
    [ForeignKey(nameof(SharesCalculatorID))]
    public SharesCalculator SharesCalculator { get; set; }
}

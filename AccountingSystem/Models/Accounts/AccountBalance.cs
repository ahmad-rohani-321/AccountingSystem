using System;
using System.ComponentModel.DataAnnotations.Schema;
using AccountingSystem.Models.Settings;

namespace AccountingSystem.Models.Accounts;

public class AccountBalance : BaseEntity
{
    public decimal Balance { get; set; }

    public int CurrencyID { get; set; }
    [ForeignKey(nameof(CurrencyID))]
    public Currency Currency { get; set; }
    public int AccountID { get; set; }
    [ForeignKey(nameof(AccountID))]
    public Account Account { get; set; }
}

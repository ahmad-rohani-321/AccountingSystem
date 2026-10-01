using System.ComponentModel.DataAnnotations.Schema;
using AccountingSystem.Models.Accounts;
using AccountingSystem.Models.Settings;

namespace AccountingSystem.Models.Shares;

public class Share : BaseEntity
{
    public int AccountId { get; set; }
    public int CurrencyId { get; set; }
    public decimal Amount { get; set; }
    public string Remarks { get; set; }

    [ForeignKey(nameof(AccountId))]
    public Account Account { get; set; }


    [ForeignKey(nameof(CurrencyId))]
    public Currency Currency { get; set; }
}

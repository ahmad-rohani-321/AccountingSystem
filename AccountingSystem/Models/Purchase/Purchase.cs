using AccountingSystem.Models.Settings;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Purchase
{
    public class Purchase : BaseEntity
    {
        public int AccountID { get; set; }
        public int PurchaseNo { get; set; }
        public int CurrencyID { get; set; }
        public string Remarks { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        [ForeignKey(nameof(AccountID))]
        public Accounts.Account Account { get; set; }

        [ForeignKey(nameof(CurrencyID))]
        public Currency Currency { get; set; }
    }
}

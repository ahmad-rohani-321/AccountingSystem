using System.ComponentModel.DataAnnotations.Schema;
namespace AccountingSystem.Models.Purchase;

public class PurchaseOrder : BaseEntity
{
    public int AccountID { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public string Remarks { get; set; }
    [ForeignKey(nameof(AccountID))]
    public Accounts.Account Account { get; set; }
}

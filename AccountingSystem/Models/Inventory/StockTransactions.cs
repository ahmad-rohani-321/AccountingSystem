using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Inventory;

public class StockTransactions : BaseEntity
{
    public decimal Quantity { get; set; }
    public string Remarks { get; set; }

    public int UnitID { get; set; }
    [ForeignKey(nameof(UnitID))]
    public Unit Unit { get; set; }

    public int TransactionID { get; set; }
    [ForeignKey(nameof(TransactionID))]
    public StockTransactionType Transaction { get; set; }
    public int StockBalanceID { get; set; }
    [ForeignKey(nameof(StockBalanceID))]
    public StockBalance StockBalance { get; set; }
}
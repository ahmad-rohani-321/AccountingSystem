namespace AccountingSystem.ViewModels;

public class StockTransactionsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string TransactionTypeName { get; set; }
    public string UnitName { get; set; }
    public decimal Quantity { get; set; }
    public string WarehouseName { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
}

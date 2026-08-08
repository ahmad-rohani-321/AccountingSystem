namespace AccountingSystem.ViewModels;

public class StockItemsViewModel
{
    public int Id { get; set; }
    public int ItemID { get; set; }
    public string ItemName { get; set; }
    public int UnitID { get; set; }
    public string UnitName { get; set; }
    public int StockID { get; set; }
    public string StockName { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinQuantity { get; set; }
    public string Remarks { get; set; }
    public int TransactionType { get; set; }
    
}

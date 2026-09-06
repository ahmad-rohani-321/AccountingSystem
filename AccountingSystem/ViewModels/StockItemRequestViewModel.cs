namespace AccountingSystem.ViewModels;

public class StockItemRequestViewModel
{
    public int ItemId { get; set; }
    public int UnitId { get; set; }
    public decimal Quantity { get; set; }
    public int StockId { get; set; }
    public decimal BaseCurrencyPurchasePrice { get; set; }
    public string Remarks { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.Now;
}

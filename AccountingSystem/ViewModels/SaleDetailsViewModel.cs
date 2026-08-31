namespace AccountingSystem.ViewModels;
public class SaleDetailsViewModel
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; }
    public decimal Quantity { get; set; }
    public decimal PerPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int StockId { get; set; }
    public string Remarks { get; set; }
    
}
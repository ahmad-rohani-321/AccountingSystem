namespace AccountingSystem.ViewModels;

public class RemainingStockOperationVm
{
    public int TransactionTypeID { get; set; }
    public int? ItemID { get; set; }
    public int? StockBalanceID { get; set; }
    public int? UnitConversionID { get; set; }
    public int? WarehouseID { get; set; }
    public decimal Quantity { get; set; }
    public string Notes { get; set; } = string.Empty;
}

namespace AccountingSystem.ViewModels;

public class PurchasesViewModel
{
    public int PurchaseId { get; set; }
    public int PurchaseNo { get; set; }
    public int PersonId { get; set; }
    public string PersonName { get; set; }
    public int CurrencyId { get; set; }
    public string CurrencyName { get; set; }
    public decimal PurchaseTotal { get; set; }
    public decimal PurchaseRecieved { get; set; }
    public decimal PurchaseRemaining { get; set; }
    public int PurchaseItemsCount { get; set; }
    public string Remarks { get; set; }
    public DateTime PurchaseDate { get; set; }
}
